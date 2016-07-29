#!/usr/bin/python

#
# This is the data publisher using MemoryContentCache
#

from pyndn import Name, Interest, Data
from pyndn.util.memory_content_cache import MemoryContentCache
from pyndn.security import KeyChain, SecurityException
from pyndn.security.identity import IdentityManager, FilePrivateKeyStorage, BasicIdentityStorage
from pyndn.security.policy import NoVerifyPolicyManager
from pyndn.threadsafe_face import ThreadsafeFace
from pyndn.encoding import ProtobufTlv
from pyndn.util.boost_info_parser import BoostInfoParser

from ndn_pi.commands import AppRequestMessage

import time
import sys
import json

import logging

try:
    import asyncio
except ImportError:
    import trollius as asyncio

class FlowPublisher(object):
    def __init__(self, face, confFile = "app.conf"):
        self._defaultIdentity = None
        self._dataPrefix = None
        self._defaultCertificateName = None
        self._controllerName = None
        self._applicationName = ""

        self._identityManager = IdentityManager(BasicIdentityStorage(), FilePrivateKeyStorage())
        self._keyChain = KeyChain(self._identityManager)

        self._face = face

        if self.processConfiguration(confFile):
            self._face.setCommandSigningInfo(self._keyChain, self._defaultCertificateName)
            print "Using default certificate name: " + self._defaultCertificateName.toUri()
        else:
            print "Setup failed"

    def getKeyChain(self):
        return self._keyChain

    def processConfiguration(self, confFile, requestPermission = True, onSetupComplete = None, onSetupFailed = None):
        config = BoostInfoParser()
        config.read(confFile)

        # TODO: handle missing configuration, refactor dict representation
        confObj = dict()
        try:
            confObj["identity"] = config["application/identity"][0].value
            confObj["prefix"] = config["application/prefix"][0].value

            confObj["signer"] = config["application/signer"][0].value
            confObj["application"] = config["application/appName"][0].value
        except KeyError as e:
            msg = "Missing key in configuration: " + str(e)
            print msg
            return False

        if "identity" in confObj:
            if confObj["identity"] == "default":
                self._defaultIdentity = self._keyChain.getDefaultIdentity()
            else:
                defaultIdName = Name(confObj["identity"])
                try:
                    defaultKey = self._keyChain.getIdentityManager().getDefaultKeyNameForIdentity(defaultIdName)
                    self._defaultIdentity = defaultIdName
                except SecurityException:
                    msg = "Identity " + defaultIdName.toUri() + " in configuration file " + confFile + " does not exist. Please configure the device with this identity first"
                    print msg
                    if onSetupFailed:
                        onSetupFailed(msg)
                    return False
        else:
            self._defaultIdentity = self._keyChain.getDefaultIdentity()
        # TODO: handling the case where no default identity is present
        self._defaultCertificateName = self._keyChain.getIdentityManager().getDefaultCertificateNameForIdentity(self._defaultIdentity)

        if "prefix" in confObj:
            self._dataPrefix = Name(confObj["prefix"])
        else:
            print "Configuration file " + confFile + " is missing application data prefix"
        
        # TODO: handling signature with direct bits instead of keylocator keyname
        signerName = self._keyChain.getCertificate(self._defaultCertificateName).getSignature().getKeyLocator().getKeyName()    
        if "signer" in confObj:    
            if confObj["signer"] == "default":
                print "Deriving from " + signerName.toUri() + " for controller name"
            else:
                intendedSigner = confObj["signer"]
                if intendedSigner != signerName.toUri():
                    print "Configuration file " + confFile + " signer names mismatch: expected " + intendedSigner + "; got " + signerName.toUri()
                else:
                    signerName = Name(intendedSigner)
        else:
            print "Deriving from " + signerName.toUri() + " for controller name"
        self._controllerName = self.getIdentityNameFromCertName(signerName)
        print "Controller name: " + self._controllerName.toUri()

        if "appName" in confObj:
            self._applicationName = confObj["appName"]

        if requestPermission:
            self.sendAppRequest()
        return True

    def getIdentityNameFromCertName(self, certName):
        i = certName.size() - 1

        idString = "KEY"
        while i >= 0:
            if certName.get(i).toEscapedString() == idString:
                break
            i -= 1

        if i < 0:
            print "Error: unexpected certName " + certName.toUri()
            return None

        return certName.getPrefix(i)

    def sendAppRequest(self):
        message = AppRequestMessage()

        for component in range(self._defaultIdentity.size()):
            message.command.idName.components.append(self._defaultIdentity.get(component).toEscapedString())
        for component in range(self._dataPrefix.size()):
            message.command.dataPrefix.components.append(self._dataPrefix.get(component).toEscapedString())
        message.command.appName = self._applicationName
        paramComponent = ProtobufTlv.encode(message)

        requestInterest = Interest(Name(self._controllerName).append("requests").appendVersion(int(time.time())).append(paramComponent))
        # TODO: change this. (for now, make this request long lived (100s), if the controller operator took some time to respond)
        requestInterest.setInterestLifetimeMilliseconds(100000)
        self._keyChain.sign(requestInterest, self._defaultCertificateName)
        self._face.expressInterest(requestInterest, self.onAppRequestData, self.onAppRequestTimeout)
        print "Application publish request sent: " + requestInterest.getName().toUri()
        return

    def onAppRequestData(self, interest, data):
        print "Got application publishing request data"

        return

    def onAppRequestTimeout(self, interest):
        print "Application publishing request times out"
        return
    
    def start(self):
        self._dataCache = MemoryContentCache(self._face, 100000)
        self.registerCachePrefix()
        print "Serving data at {}".format(self._dataPrefix.toUri())
        self._face.callLater(5000, self.publishData)
        return

    def registerCachePrefix(self):
        self._dataCache.registerPrefix(self._dataPrefix, self.cacheRegisterFail, self.onDataMissing)

    def cacheRegisterFail(self, interest):
        # just try again
        self.log.warn('Could not register data cache')
        self.registerCachePrefix()

    def onDataMissing(self, prefix, interest, transport, prefixId):
        self._missedRequests += 1
        # let it timeout

    def publishData(self):
        timestamp = time.time() 
        cpu_use = ps.cpu_percent()
        users = [u.name for u in ps.users()]
        nProcesses = len(ps.pids())
        memUse = ps.virtual_memory().percent
        swapUse = ps.swap_memory().percent

        info = {'cpu_usage':cpu_use, 'users':users, 'processes':nProcesses,
                 'memory_usage':memUse, 'swap_usage':swapUse}
    
        dataOut = Data(Name(self._dataPrefix).appendVersion(int(timestamp)))
        dataOut.setContent(json.dumps(info))
        dataOut.getMetaInfo().setFreshnessPeriod(10000)
        self._keyChain.sign(dataOut, self._defaultCertificateName)

        self._dataCache.add(dataOut)

        # repeat every 5 seconds
        self._face.callLater(5000, self.publishData)

if __name__ == '__main__':
    try:
        import psutil as ps
    except Exception as e:
        print str(e)

    loop = asyncio.get_event_loop()
    face = ThreadsafeFace(loop)

    n = FlowPublisher(face)
    n.start()

    loop.run_forever()
