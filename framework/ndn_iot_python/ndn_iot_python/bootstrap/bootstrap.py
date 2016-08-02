#!/usr/bin/python

from pyndn import Name, Interest, Data, Exclude
from pyndn.util.memory_content_cache import MemoryContentCache
from pyndn.security import KeyChain, SecurityException
from pyndn.security.identity import IdentityManager, FilePrivateKeyStorage, BasicIdentityStorage
from pyndn.security.policy import NoVerifyPolicyManager
from pyndn.threadsafe_face import ThreadsafeFace
from pyndn.encoding import ProtobufTlv
from pyndn.util.boost_info_parser import BoostInfoParser
from pyndn.util.memory_content_cache import MemoryContentCache

from ndn_iot_python.commands.app_request_pb2 import AppRequestMessage

import time
import sys
import json

import logging

try:
    import asyncio
except ImportError:
    import trollius as asyncio

class Bootstrap(object):
    def __init__(self, face):
        self._defaultIdentity = None
        self._dataPrefix = None
        self._defaultCertificateName = None
        self._controllerName = None
        self._applicationName = ""

        self._identityManager = IdentityManager(BasicIdentityStorage())
        self._keyChain = KeyChain(self._identityManager)

        self._face = face
        self._certificateContentCache = MemoryContentCache(face)
        self._followingTrustSchema = dict()

    def getKeyChain(self):
        return self._keyChain

    def start(self):
        print "start not implemented"
        return 

    def setupKeyChain(self, confObjOrFileName, requestPermission = True, onSetupComplete = None, onSetupFailed = None):
        if isinstance(confObjOrFileName, basestring):
            confObj = self.processConfiguration(confObjOrFileName)
        else:
            confObj = confObjOrFileName

        if "identity" in confObj:
            if confObj["identity"] == "default":
                self._defaultIdentity = self._keyChain.getDefaultIdentity()
            else:
                defaultIdName = Name(confObj["identity"])
                try:
                    defaultKey = self._keyChain.getIdentityManager().getDefaultKeyNameForIdentity(defaultIdName)
                    self._defaultIdentity = defaultIdName
                except SecurityException:
                    msg = "Identity " + defaultIdName.toUri() + " in configuration does not exist. Please configure the device with this identity first"
                    print msg
                    if onSetupFailed:
                        onSetupFailed(msg)
                    return False
        else:
            self._defaultIdentity = self._keyChain.getDefaultIdentity()

        # TODO: handling the case where no default identity is present
        self._defaultCertificateName = self._keyChain.getIdentityManager().getDefaultCertificateNameForIdentity(self._defaultIdentity)
        self._defaultKeyName = self._keyChain.getIdentityManager().getDefaultKeyNameForIdentity(self._defaultIdentity)
        # Note we'll not be able to issue face commands before this point
        # Decouple this from command interest signing?
        self._face.setCommandSigningInfo(self._keyChain, self._defaultCertificateName)
        print "default cert name " + self._defaultCertificateName.toUri()
        self._certificateContentCache.registerPrefix(Name(self._defaultCertificateName).getPrefix(-1), self.onRegisterFailed)
        self._certificateContentCache.add(self._keyChain.getCertificate(self._defaultCertificateName))

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

        if requestPermission:
            self.sendAppRequest(confObj["application"], onSetupComplete, onSetupFailed)
        else:
            if onSetupComplete:
                onSetupComplete(self._defaultIdentity, self._keyChain)
        return True

    def onRegisterFailed(self, prefix):
        print("register failed for prefix " + prefix.getName().toUri())
        return

    def onTrustSchemaData(self, interest, data):
        print("Trust schema received: " + data.getName().toUri())
        # Process newly received trust schema

        newInterest = Interest(interest)
        newInterest.refreshNonce()
        excludeComponent = data.getName().get(-1)
        exclude = Exclude()
        exclude.appendAny()
        exclude.appendComponent(excludeComponent)
        newInterest.setExclude(exclude)
        self._face.expressInterest(newInterest, self.onTrustSchemaData, self.onTrustSchemaTimeout)
        return

    def onTrustSchemaTimeout(self, interest):
        print("Trust schema interest times out: " + interest.getName().toUri())
        newInterest = Interest(interest)
        newInterest.refreshNonce()
        self._face.expressInterest(newInterest, self.onTrustSchemaData, self.onTrustSchemaTimeout)        
        return  

    def startTrustSchemaUpdate(self, namespace, onUpdateSuccess = None, onUpdateFailed = None):
        self._followingTrustSchema[namespace] = True
        initialInterest = Interest(Name(namespace))
        initialInterest.setChildSelector(1)
        self._face.expressInterest(initialInterest, self.onTrustSchemaData, self.onTrustSchemaTimeout)
        return

    def stopTrustSchemaUpdate(self):
        print "stopTrustSchemaUpdate not implemented"
        return

    def processConfiguration(self, confFile):
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
            return None
        return confObj

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

    def sendAppRequest(self, applicationName, onSetupComplete, onSetupFailed):
        message = AppRequestMessage()

        for component in range(self._defaultCertificateName.size()):
            message.command.idName.components.append(self._defaultCertificateName.get(component).toEscapedString())
        for component in range(self._dataPrefix.size()):
            message.command.dataPrefix.components.append(self._dataPrefix.get(component).toEscapedString())
        message.command.appName = applicationName
        paramComponent = ProtobufTlv.encode(message)

        requestInterest = Interest(Name(self._controllerName).append("requests").appendVersion(int(time.time())).append(paramComponent))

        requestInterest.setInterestLifetimeMilliseconds(4000)
        self._face.makeCommandInterest(requestInterest)
        self._face.expressInterest(requestInterest, 
          lambda interest, data : self.onAppRequestData(interest, data, onSetupComplete, onSetupFailed), 
          lambda interest : self.onAppRequestTimeout(interest, onSetupComplete, onSetupFailed))
        print "Application publish request sent: " + requestInterest.getName().toUri()
        return

    def onAppRequestData(self, interest, data, onSetupComplete, onSetupFailed):
        print "Got application publishing request data"
        def onVerified(data):
            if data.getContent().toRawStr() == "200":
                if onSetupComplete:
                    onSetupComplete(self._defaultIdentity, self._keyChain)
                else:
                    print "onSetupComplete"
        def onVerifyFailed(data):
            print "Application request response verification failed!"
        self._keyChain.verifyData(data, onVerified, onVerifyFailed)
        return

    def onAppRequestTimeout(self, interest, onSetupComplete, onSetupFailed):
        print "Application publishing request times out"
        newInterest = Interest(interest)
        newInterest.refreshNonce()
        self._face.expressInterest(newInterest,
          lambda interest, data : self.onAppRequestData(interest, data, onSetupComplete, onSetupFailed), 
          lambda interest : self.onAppRequestTimeout(interest, onSetupComplete, onSetupFailed))
        return
