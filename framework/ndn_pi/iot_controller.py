from __future__ import print_function

import logging
import time
from sys import stdin, stdout
import struct

from pyndn import Name, Face, Interest, Data
from pyndn.security import KeyChain
from pyndn.security.certificate import IdentityCertificate, PublicKey, CertificateSubjectDescription
from pyndn.encoding import ProtobufTlv
from pyndn.security.security_exception import SecurityException
from pyndn.util import Blob, MemoryContentCache
from pyndn.util.boost_info_parser import BoostInfoParser, BoostInfoTree

from base_node import BaseNode, Command

from commands import CertificateRequestMessage, UpdateCapabilitiesCommandMessage, DeviceConfigurationMessage, AppRequestMessage
from security.hmac_helper import HmacHelper

from collections import defaultdict
import json
from base64 import b64encode

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
    def __init__(self, nodeName, networkName, applicationDirectory = ""):
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

        # Set up application directory
        if applicationDirectory == "":
            applicationDirectory = os.path.expanduser('~/.ndn/iot/applications')
        self._applicationDirectory = applicationDirectory
        self._applications = dict()
        
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
        # Trusting root's own certificate upon each run
        # TODO: debug where application starts first and controller starts second, application's interest cannot be verified
        self._rootCertificate = self._keyChain.getCertificate(self.getDefaultCertificateName())
        self._policyManager._certificateCache.insertCertificate(self._rootCertificate)
        
        self._memoryContentCache = MemoryContentCache(self.face)
        self.face.setCommandSigningInfo(self._keyChain, self.getDefaultCertificateName())
        self._memoryContentCache.registerPrefix(self.prefix, onRegisterFailed = self.onRegisterFailed, 
          onRegisterSuccess = None, onDataNotFound = self._onCommandReceived)
        # Serve root certificate in our memoryContentCache
        self._memoryContentCache.add(self._rootCertificate)
        self.loadApplications()
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

        interestName = Name('/home/configure').append(Name(deviceSerial))
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

    def _handleCertificateRequest(self, interest):
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
        except SecurityException as e:
            self.log.warn('Could not create device certificate: ' + str(e))
        else:
            self.log.info('Creating certificate for device {}'.format(deviceSerial))

        if certData is not None:
            response.setContent(certData.wireEncode())
            response.getMetaInfo().setFreshnessPeriod(10000) # should be good even longer
        else:
            response.setContent("Denied")
        if hmac is not None:
            hmac.signData(response)
        self.sendData(response, False)

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
        except SecurityException as e:
            print(e)
            # assume this is due to already existing?
            pass

        certificate = self._identityManager._generateCertificateForKey(keyName)

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
        self._directory = tempDirectory

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

    def _onCommandReceived(self, prefix, interest, face, interestFilterId, filter):
        """
        """
        interestName = interest.getName()

        #if it is a certificate name, serve the certificate
        # TODO: since we've memoryContentCache serving root cert now, this should no longer be required
        try:
            if interestName.isPrefixOf(self.getDefaultCertificateName()):
                foundCert = self._identityManager.getCertificate(self.getDefaultCertificateName())
                self.log.debug("Serving certificate request")
                self.face.putData(foundCert)
                return
        except SecurityException as e:
            # We don't have this certificate, this is probably not a certificate request
            # TODO: this does not differentiate from certificate request but certificate not exist; should update
            print(str(e))
            pass

        afterPrefix = interestName.get(prefix.size()).toEscapedString()
        if afterPrefix == "listDevices":
            #compose device list
            self.log.debug("Received device list request")
            response = self._prepareCapabilitiesList(interestName)
            self.sendData(response)
        elif afterPrefix == "certificateRequest":
            #build and sign certificate
            self.log.debug("Received certificate request")
            self._handleCertificateRequest(interest)

        elif afterPrefix == "updateCapabilities":
            # needs to be signed!
            self.log.debug("Received capabilities update")
            def onVerifiedCapabilities(interest):
                print("capabilities good")
                response = Data(interest.getName())
                response.setContent(str(time.time()))
                self.sendData(response)
                self._updateDeviceCapabilities(interest)
            self._keyChain.verifyInterest(interest, 
                    onVerifiedCapabilities, self.verificationFailed)
        elif afterPrefix == "requests":
            # application request to publish under some names received; need to be signed
            def onVerifiedAppRequest(interest):
                # TODO: for now, we automatically grant access to any valid signed interest
                print("verified! send response!")
                message = AppRequestMessage()
                ProtobufTlv.decode(message, interest.getName().get(prefix.size() + 1).getValue())
                certName = Name("/".join(message.command.idName.components))
                dataPrefix = Name("/".join(message.command.dataPrefix.components))
                appName = message.command.appName
                isUpdated = self.updateTrustSchema(appName, certName, dataPrefix, True)

                response = Data(interest.getName())
                if isUpdated:
                    response.setContent("{\"status\": 200, \"message\": \"granted, trust schema updated OK\" }")
                    self.log.info("Verified and granted application publish request")
                else:
                    response.setContent("{\"status\": 400, \"message\": \"not granted, requested publishing namespace already exists\" }")
                    self.log.info("Verified and but requested namespace already exists")
                self.sendData(response)
                return
            def onVerificationFailedAppRequest(interest):
                print("application request verify failed!")
                response = Data(interest.getName())
                response.setContent("{\"status\": 401, \"message\": \"command interest verification failed\" }")
                self.sendData(response)
            self.log.info("Received application request: " + interestName.toUri())
            #print("Verifying with trust schema: ")
            #print(self._policyManager.config)
            self._keyChain.verifyInterest(interest, 
                    onVerifiedAppRequest, onVerificationFailedAppRequest)
        else:
            print("Got interest unable to answer yet: " + interest.getName().toUri())
            if interest.getExclude():
                print("interest has exclude: " + interest.getExclude().toUri())
            # response = Data(interest.getName())
            # response.setContent("500")
            # response.getMetaInfo().setFreshnessPeriod(1000)
            # self.sendData(response)

    def onStartup(self):
        # begin taking add requests
        self.loop.call_soon(self.displayMenu)
        self.loop.add_reader(stdin, self.handleUserInput) 

    def displayMenu(self):
        menuStr = "\n"
        menuStr += "P)air a new device with serial and PIN\n"
        menuStr += "D)irectory listing\n"
        menuStr += "E)xpress an interest\n"
        menuStr += "L)oad hosted applications (" + (self._applicationDirectory) + ")\n"
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

    def loadApplicationsMenuSelect(self):
        try:
            confirm = input('This will override existing trust schemas, continue? (Y/N): ').upper().startswith('Y')
            if confirm:
                self.loadApplications(override = True)
            else:
                print("Aborted")
        except KeyboardInterrupt:
            print("Aborted")
        finally:
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
        elif inputStr.startswith('L'):
            self.loadApplicationsMenuSelect()
        else:
            self.loop.call_soon(self.displayMenu)
            
########################
# application trust schema distribution
########################
    def updateTrustSchema(self, appName, certName, dataPrefix, publishNew = False):
        if appName in self._applications:
            if dataPrefix.toUri() in self._applications[appName]["dataPrefix"]:
                print("some key is configured for namespace " + dataPrefix.toUri() + " for application " + appName + ". Ignoring this request.")
                return False
            else:
                # TODO: Handle malformed conf where validator tree does not exist
                validatorNode = self._applications[appName]["tree"]["validator"][0]
        else:
            # This application does not previously exist, we create its trust schema 
            # (and for now, add in static rules for sync data)

            self._applications[appName] = {"tree": BoostInfoParser(), "dataPrefix": [], "version": 0}
            validatorNode = self._applications[appName]["tree"].getRoot().createSubtree("validator")
            
            trustAnchorNode = validatorNode.createSubtree("trust-anchor")
            #trustAnchorNode.createSubtree("type", "file")
            #trustAnchorNode.createSubtree("file-name", os.path.expanduser("~/.ndn/iot/root.cert"))
            trustAnchorNode.createSubtree("type", "base64")
            trustAnchorNode.createSubtree("base64-string", Blob(b64encode(self._rootCertificate.wireEncode().toBytes()), False).toRawStr())

            #create cert verification rule
            # TODO: the idea for this would be, if the cert has /home-prefix/<one-component>/KEY/ksk-*/ID-CERT, then it should be signed by fixed controller(s)
            # if the cert has /home-prefix/<multiple-components>/KEY/ksk-*/ID-CERT, then it should be checked hierarchically (this is for subdomain support)
            certRuleNode = validatorNode.createSubtree("rule")
            certRuleNode.createSubtree("id", "Certs")
            certRuleNode.createSubtree("for", "data")

            filterNode = certRuleNode.createSubtree("filter")
            filterNode.createSubtree("type", "regex")
            filterNode.createSubtree("regex", "^[^<KEY>]*<KEY><>*<ID-CERT>")

            checkerNode = certRuleNode.createSubtree("checker")
            # TODO: wait how did my first hierarchical verifier work?
            #checkerNode.createSubtree("type", "hierarchical")

            checkerNode.createSubtree("type", "customized")
            checkerNode.createSubtree("sig-type", "rsa-sha256")

            keyLocatorNode = checkerNode.createSubtree("key-locator")
            keyLocatorNode.createSubtree("type", "name")
            # We don't put cert version in there
            keyLocatorNode.createSubtree("name", Name(self.getDefaultCertificateName()).getPrefix(-1).toUri())
            keyLocatorNode.createSubtree("relation", "equal")

            # Discovery rule: anything that multicasts under my home prefix should be signed, and the signer should have been authorized by root
            # TODO: This rule as of right now is over-general
            discoveryRuleNode = validatorNode.createSubtree("rule")
            discoveryRuleNode.createSubtree("id", "sync-data")
            discoveryRuleNode.createSubtree("for", "data")

            filterNode = discoveryRuleNode.createSubtree("filter")
            filterNode.createSubtree("type", "regex")
            filterNode.createSubtree("regex", "^[^<MULTICAST>]*<MULTICAST><>*")

            checkerNode = discoveryRuleNode.createSubtree("checker")
            # TODO: wait how did my first hierarchical verifier work?
            #checkerNode.createSubtree("type", "hierarchical")

            checkerNode.createSubtree("type", "customized")
            checkerNode.createSubtree("sig-type", "rsa-sha256")

            keyLocatorNode = checkerNode.createSubtree("key-locator")
            keyLocatorNode.createSubtree("type", "name")
            keyLocatorNode.createSubtree("regex", "^[^<KEY>]*<KEY><>*<ID-CERT>")


        ruleNode = validatorNode.createSubtree("rule")
        ruleNode.createSubtree("id", dataPrefix.toUri())
        ruleNode.createSubtree("for", "data")
        
        filterNode = ruleNode.createSubtree("filter")
        filterNode.createSubtree("type", "name")
        filterNode.createSubtree("name", dataPrefix.toUri())
        filterNode.createSubtree("relation", "is-prefix-of")

        checkerNode = ruleNode.createSubtree("checker")
        checkerNode.createSubtree("type", "customized")
        checkerNode.createSubtree("sig-type", "rsa-sha256")

        keyLocatorNode = checkerNode.createSubtree("key-locator")
        keyLocatorNode.createSubtree("type", "name")
        # We don't put cert version in there
        keyLocatorNode.createSubtree("name", certName.getPrefix(-1).toUri())
        keyLocatorNode.createSubtree("relation", "equal")

        if not os.path.exists(self._applicationDirectory):
            os.makedirs(self._applicationDirectory)
        self._applications[appName]["tree"].write(os.path.join(self._applicationDirectory, appName + ".conf"))
        self._applications[appName]["dataPrefix"].append(dataPrefix.toUri())
        self._applications[appName]["version"] = int(time.time())
        if publishNew:
            # TODO: ideally, this is the trust schema of the application, and does not necessarily carry controller prefix. 
            # We make it carry controller prefix here so that prefix registration / route setup is easier (implementation workaround)
            data = Data(Name(self.prefix).append(appName).append("_schema").appendVersion(self._applications[appName]["version"]))
            data.setContent(str(self._applications[appName]["tree"].getRoot()))
            self.signData(data)
            self._memoryContentCache.add(data)
        return True
    
    # TODO: putting existing confs into memoryContentCache        
    def loadApplications(self, directory = None, override = False):
        if not directory:
            directory = self._applicationDirectory
        if override:
            self._applications.clear()
        if os.path.exists(directory):
            for f in os.listdir(directory):
                fullFileName = os.path.join(directory, f)
                if os.path.isfile(fullFileName) and f.endswith('.conf'):
                    appName = f.rstrip('.conf')
                    if appName in self._applications and not override:
                        print("loadApplications: " + appName + " already exists, do nothing for configuration file: " + fullFileName)
                    else:
                        self._applications[appName] = {"tree": BoostInfoParser(), "dataPrefix": [], "version": int(time.time())}
                        self._applications[appName]["tree"].read(fullFileName)
                        data = Data(Name(self.prefix).append(appName).append("_schema").appendVersion(self._applications[appName]["version"]))
                        data.setContent(str(self._applications[appName]["tree"].getRoot()))
                        self.signData(data)
                        self._memoryContentCache.add(data)
                        try:
                            validatorTree = self._applications[appName]["tree"]["validator"][0]
                            for rule in validatorTree["rule"]:
                                self._applications[appName]["dataPrefix"].append(rule["id"][0].value)
                        # TODO: don't swallow any general exceptions, we want to catch only KeyError (make sure) here
                        except Exception as e:
                            print("loadApplications parse configuration file " + fullFileName + " : " + str(e))

        return

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
