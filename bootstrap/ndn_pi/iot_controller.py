from __future__ import print_function

import logging
import time
from sys import stdin, stdout
import struct

from pyndn import Name, Face, Interest, Data
from pyndn.util import Blob
from pyndn.security import KeyChain
from pyndn.security.certificate import IdentityCertificate, PublicKey, CertificateSubjectDescription
from pyndn.encoding import ProtobufTlv
from pyndn.security.security_exception import SecurityException

from base_node import BaseNode, Command

from commands import CertificateRequestMessage, UpdateCapabilitiesCommandMessage, DeviceConfigurationMessage
from security import HmacHelper



from collections import defaultdict
import json

try:
    import asyncio
except ImportError:
    import trollius as asyncio

from pyndn.threadsafe_face import ThreadsafeFace

# more Python 2+3 compatibility
try:
    input = raw_input
except NameError:
    pass

class IotController(BaseNode):
    """
    The controller class has a few built-in commands:
        - listDevices: return the names and capabilities of all attached devices
        - certificateRequest: takes public key information and returns name of
            new certificate
        - updateCapabilities: should be sent periodically from IotNodes to update their
           command lists
        - addDevice: add a device based on HMAC
    It is unlikely that you will need to subclass this.
    """
    def __init__(self, nodeName, networkName):
        super(IotController, self).__init__()
        
        self.deviceSuffix = Name(nodeName)
        self.networkPrefix = Name(networkName)
        self.prefix = Name(self.networkPrefix).append(self.deviceSuffix)

        self._policyManager.setEnvironmentPrefix(self.networkPrefix)
        self._policyManager.setTrustRootIdentity(self.prefix)
        self._policyManager.setDeviceIdentity(self.prefix)
        self._policyManager.updateTrustRules()
        
        # the controller keeps a directory of capabilities->names
        self._directory = defaultdict(list)

        # keep track of who's still using HMACs
        # key is device serial, value is the HmacHelper
        self._hmacDevices = {}

        # our capabilities
        self._baseDirectory = {}

        # add the built-ins
        self._insertIntoCapabilities('listDevices', 'directory', False)
        self._insertIntoCapabilities('updateCapabilities', 'capabilities', True)

        self._directory.update(self._baseDirectory)

    def _insertIntoCapabilities(self, commandName, keyword, isSigned):
        newUri = Name(self.prefix).append(Name(commandName)).toUri()
        self._baseDirectory[keyword] = [{'signed':isSigned, 'name':newUri}]

    def beforeLoopStart(self):
        if not self._policyManager.hasRootSignedCertificate():
            # make one....
            self.log.warn('Generating controller certificate...')
            newKey = self._identityManager.generateRSAKeyPairAsDefault(
                self.prefix, isKsk=True)
            newCert = self._identityManager.selfSign(newKey)
            self._identityManager.addCertificateAsDefault(newCert)
        self.face.setCommandSigningInfo(self._keyChain, self.getDefaultCertificateName())
        self.face.registerPrefix(self.prefix, 
            self._onCommandReceived, self.onRegisterFailed)
        self.loop.call_soon(self.onStartup)


######
# Initial configuration
#######
    # TODO: deviceSuffix will be replaced by deviceSerial
    def _addDeviceToNetwork(self, deviceSerial, newDeviceSuffix, pin):
        h = HmacHelper(pin)
        self._hmacDevices[deviceSerial] = h

        d = DeviceConfigurationMessage()

        for source, dest in [(self.networkPrefix, d.configuration.networkPrefix),
                             (self.deviceSuffix, d.configuration.controllerName),
                             (newDeviceSuffix, d.configuration.deviceSuffix)]:
            for i in range(source.size()):
                component = source.get(i)
                dest.components.append(component.getValue().toRawStr())

        interestName = Name('/localhop/configure').append(Name(deviceSerial))
        encodedParams = ProtobufTlv.encode(d)
        interestName.append(encodedParams)
        interest = Interest(interestName)
        h.signInterest(interest)

        self.face.expressInterest(interest, self._deviceAdditionResponse,
            self._deviceAdditionTimedOut)

    def _deviceAdditionTimedOut(self, interest):
        deviceSerial = str(interest.getName().get(2).getValue())
        self.log.warn("Timed out trying to configure device " + deviceSerial)
        # don't try again
        self._hmacDevices.pop(deviceSerial)

    def _deviceAdditionResponse(self, interest, data):
        status = data.getContent().toRawStr()
        deviceSerial = str(interest.getName().get(2).getValue())
        hmacChecker = self._hmacDevices[deviceSerial]
        if (hmacChecker.verifyData(data)): 
            self.log.info("Received {} from {}".format(status, deviceSerial))
        else:
            self.log.warn("Received invalid HMAC from {}".format(deviceSerial))
        
######
# Certificate signing
######

    def _handleCertificateRequest(self, interest, transport):
        """
        Extracts a public key name and key bits from a command interest name 
        component. Generates a certificate if the request is verifiable.

        This expects an HMAC signed interest.
        """
        message = CertificateRequestMessage()
        commandParamsTlv = interest.getName().get(self.prefix.size()+1)
        ProtobufTlv.decode(message, commandParamsTlv.getValue())

        signature = HmacHelper.extractInterestSignature(interest)
        deviceSerial = str(signature.getKeyLocator().getKeyName().get(-1).getValue())

        response = Data(interest.getName())
        certData = None
        hmac = None
        try:
            hmac = self._hmacDevices[deviceSerial]
            if hmac.verifyInterest(interest):
                certData = self._createCertificateFromRequest(message)
                # remove this hmac; another request will require a new pin
                self._hmacDevices.pop(deviceSerial)
        except KeyError:
            self.log.warn('Received certificate request for device with no registered key')
        except SecurityException:
            self.log.warn('Could not create device certificate')
        else:
            self.log.info('Creating certificate for device {}'.format(deviceSerial))

        if certData is not None:
            response.setContent(certData.wireEncode())
            response.getMetaInfo().setFreshnessPeriod(10000) # should be good even longer
        else:
            response.setContent("Denied")
        if hmac is not None:
            hmac.signData(response)
        self.sendData(response, transport, False)

    def _createCertificateFromRequest(self, message):
        """
        Generate an IdentityCertificate from the public key information given.
        """
        # TODO: Verify the certificate was actually signed with the private key
        # matching the public key we are issuing a cert for!!

        keyComponents = message.command.keyName.components
        keyName = Name("/".join(keyComponents))

        self.log.debug("Key name: " + keyName.toUri())

        if not self._policyManager.getEnvironmentPrefix().match(keyName):
            # we do not issue certs for keys outside of our network
            return None

        keyDer = Blob(message.command.keyBits)
        keyType = message.command.keyType

        try:
            self._identityStorage.addKey(keyName, keyType, keyDer)
        except SecurityException:
            # assume this is due to already existing?
            pass

        certificate = self._identityManager.generateCertificateForKey(keyName)

        self._keyChain.sign(certificate, self.getDefaultCertificateName())
        # store it for later use + verification
        self._identityStorage.addCertificate(certificate)
        self._policyManager._certificateCache.insertCertificate(certificate)
        return certificate

######
# Device Capabilities
######

    def _updateDeviceCapabilities(self, interest):
        """
        Take the received capabilities update interest and update our directory listings.
        """
        # we assume the sender is the one who signed the interest...
        signature = self._policyManager._extractSignature(interest)
        certificateName = signature.getKeyLocator().getKeyName()
        senderIdentity = IdentityCertificate.certificateNameToPublicKeyName(certificateName).getPrefix(-1)

        self.log.info('Updating capabilities for {}'.format(senderIdentity.toUri()))

        # get the params from the interest name
        messageComponent = interest.getName().get(self.prefix.size()+1)
        message = UpdateCapabilitiesCommandMessage()
        ProtobufTlv.decode(message, messageComponent.getValue())
        # we remove all the old capabilities for the sender
        tempDirectory = defaultdict(list)
        for keyword in self._directory:
            tempDirectory[keyword] = [cap for cap in self._directory[keyword] 
                    if not senderIdentity.match(Name(cap['name']))]

        # then we add the ones from the message
        for capability in message.capabilities:
            capabilityPrefix = Name()
            for component in capability.commandPrefix.components:
                capabilityPrefix.append(component)
            commandUri = capabilityPrefix.toUri()
            if not senderIdentity.match(capabilityPrefix):
                self.log.error("Node {} tried to register another prefix: {} - ignoring update".format(
                    senderIdentity.toUri(),commandUri))
            else:    
                for keyword in capability.keywords:
                    allUris = [info['name'] for info in tempDirectory[keyword]]
                    if capabilityPrefix not in allUris:
                        listing = {'signed':capability.needsSignature,
                                'name':commandUri}
                        tempDirectory[keyword].append(listing)
        self._directory= tempDirectory

    def _prepareCapabilitiesList(self, interestName):
        """
        Responds to a directory listing request with JSON
        """
        
        dataName = Name(interestName).append(Name.Component.fromNumber(int(time.time())))
        response = Data(dataName)

        response.setContent(json.dumps(self._directory))

        return response

#####
# Interest handling
####

    def _onCommandReceived(self, prefix, interest, transport, prefixId):
        """
        """
        interestName = interest.getName()

        #if it is a certificate name, serve the certificate
        foundCert = self._identityStorage.getCertificate(interestName)
        if foundCert is not None:
            self.log.debug("Serving certificate request")
            transport.send(foundCert.wireEncode().buf())
            return

        afterPrefix = interestName.get(prefix.size()).toEscapedString()
        if afterPrefix == "listDevices":
            #compose device list
            self.log.debug("Received device list request")
            response = self._prepareCapabilitiesList(interestName)
            self.sendData(response, transport)
        elif afterPrefix == "certificateRequest":
            #build and sign certificate
            self.log.debug("Received certificate request")
            self._handleCertificateRequest(interest, transport)

        elif afterPrefix == "updateCapabilities":
            # needs to be signed!
            self.log.debug("Received capabilities update")
            def onVerifiedCapabilities(interest):
                response = Data(interest.getName())
                response.setContent(str(time.time()))
                self.sendData(response, transport)
                self._updateDeviceCapabilities(interest)
            self._keyChain.verifyInterest(interest, 
                    onVerifiedCapabilities, self.verificationFailed)
        else:
            response = Data(interest.getName())
            response.setContent("500")
            response.getMetaInfo().setFreshnessPeriod(1000)
            transport.send(response.wireEncode().buf())

    def onStartup(self):
        # begin taking add requests
        self.loop.call_soon(self.displayMenu)
        self.loop.add_reader(stdin, self.handleUserInput) 

    def displayMenu(self):
        menuStr = "\n"
        menuStr += "P)air a new device with serial and PIN\n"
        menuStr += "D)irectory listing\n"
        menuStr += "E)xpress an interest\n"
        menuStr += "Q)uit\n"

        print(menuStr)
        print ("> ", end="")
        stdout.flush()

    def listDevices(self):
        menuStr = ''
        for capability, commands in self._directory.items():
            menuStr += '{}:\n'.format(capability)
            for info in commands:
                signingStr = 'signed' if info['signed'] else 'unsigned'
                menuStr += '\t{} ({})\n'.format(info['name'], signingStr)
        print(menuStr)
        self.loop.call_soon(self.displayMenu)

    def onInterestTimeout(self, interest):
        print('Interest timed out: {}'.interest.getName().toUri())

    def onDataReceived(self, interest, data):
        print('Received data named: {}'.format(data.getName().toUri()))
        print('Contents:\n{}'.format(data.getContent().toRawStr()))
    
    def expressInterest(self):
        try:
            interestName = input('Interest name: ')
            if len(interestName):
                toSign = input('Signed? (y/N): ').upper().startswith('Y')
                interest = Interest(Name(interestName))
                interest.setInterestLifetimeMilliseconds(5000)
                interest.setChildSelector(1)
                if (toSign):
                    self.face.makeCommandInterest(interest) 
                self.face.expressInterest(interest, self.onDataReceived, self.onInterestTimeout)
            else:
                print("Aborted")
        except KeyboardInterrupt:
                print("Aborted")
        finally:
                self.loop.call_soon(self.displayMenu)

    def beginPairing(self):
        try:
            deviceSerial = input('Device serial: ') 
            devicePin = input('PIN: ')
            deviceSuffix = input('Node name: ')
        except KeyboardInterrupt:
               print('Pairing attempt aborted')
        else:
            if len(deviceSerial) and len(devicePin) and len(deviceSuffix):
                self._addDeviceToNetwork(deviceSerial, Name(deviceSuffix), 
                    devicePin.decode('hex'))
            else:
               print('Pairing attempt aborted')
        finally:
            self.loop.call_soon(self.displayMenu)

    def handleUserInput(self):
        inputStr = stdin.readline().upper()
        if inputStr.startswith('D'):
            self.listDevices()
        elif inputStr.startswith('P'):
            self.beginPairing()
        elif inputStr.startswith('E'):
            self.expressInterest()
        elif inputStr.startswith('Q'):
            self.stop()
        else:
            self.loop.call_soon(self.displayMenu)
            
        

if __name__ == '__main__':
    import os
    import sys

    nArgs = len(sys.argv) - 1
    if nArgs == 0:
        from pyndn.util.boost_info_parser import BoostInfoParser
        fileName = os.path.expanduser('~/.ndn/iot_controller.conf')
    
        config = BoostInfoParser()
        config.read(fileName)
        deviceName = config["device/controllerName"][0].value
        networkName = config["device/environmentPrefix"][0].value
    elif nArgs == 2:
        networkName = sys.argv[1]
        deviceName = sys.argv[2]
    else:
        print('Usage: {} [network-name controller-name]'.format(sys.argv[0]))
        sys.exit(1)

    deviceSuffix = Name(deviceName)
    networkPrefix = Name(networkName)
    n = IotController(deviceSuffix, networkPrefix)
    n.start()
