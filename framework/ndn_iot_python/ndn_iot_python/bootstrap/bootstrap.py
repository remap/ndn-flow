#!/usr/bin/python

from pyndn import Name, Interest, Data, Exclude

from pyndn.threadsafe_face import ThreadsafeFace
from pyndn.security import KeyChain, SecurityException
from pyndn.security.identity import IdentityManager, FilePrivateKeyStorage, BasicIdentityStorage
from pyndn.security.policy import ConfigPolicyManager
from pyndn.security.certificate import IdentityCertificate

from pyndn.encoding import ProtobufTlv
from pyndn.util import MemoryContentCache
from pyndn.util.boost_info_parser import BoostInfoParser

from ndn_iot_python.commands.app_request_pb2 import AppRequestMessage

import time
import os, sys
import json

import logging

try:
    import asyncio
except ImportError:
    import trollius as asyncio

class Bootstrap(object):
    """
    Create a Bootstrap object. Bootstrap object provides interface for setting up KeyChain, default certificate name;
    (as a producer) requesting publishing authorization from controller; and (as a consumer) keeping track of changes

    :param face: the face for communicating with a local / remote forwarder
    :type face: ThreadsafeFace

    TODO: support Face as well as ThreadsafeFace
    """
    def __init__(self, face):
        self._defaultIdentity = None
        self._defaultCertificateName = None
        
        self._controllerName = None
        self._controllerCertificate = None

        self._applicationName = ""

        self._identityManager = IdentityManager(BasicIdentityStorage(), FilePrivateKeyStorage())
        __location__ = os.path.realpath(os.path.join(os.getcwd(), os.path.dirname(__file__)))

        self._policyManager = ConfigPolicyManager(os.path.join(__location__, ".default.conf"))
        # keyChain is what we return to the application after successful setup
        # TODO: should we separate keyChain from internal KeyChain used to verify trust schemas?
        self._keyChain = KeyChain(self._identityManager, self._policyManager)

        self._face = face
        # setFace for keyChain or else it won't be able to express interests for certs
        self._keyChain.setFace(self._face)
        self._certificateContentCache = MemoryContentCache(face)
        
        self._trustSchemas = dict()

###############################################
# Initial keyChain and defaultCertificate setup
###############################################
    def setupDefaultIdentityAndRoot(self, defaultIdentityOrFileName, signerName = None, onSetupComplete = None, onSetupFailed = None):
        """
        Sets up the keyChain, default key name and certificate name according to given 
        configuration. If successful, this KeyChain and default certificate name will be 
        returned to the application, which can be passed to instances like Consumer, Discovery, etc

        :param defaultIdentityOrFileName: if str, the name of the configuration file; if Name, 
          the default identity name of this IoT node. The node will use the default keys and 
          certificate of that identity name.
        :type defaultIdentityOrFileName: Name or str
        :param signerName: (optional) the expected signing identity of the certificate
        :type signerName: Name
        :param onSetupComplete: (optional) onSetupComplete(Name, KeyChain) will be called if 
          set up's successful
        :type onSetupComplete: function object
        :param onSetupFailed: (optional) onSetupFailed(msg) will be called if setup fails
        :type onSetupFailed: function object
        """
        def helper(identityName, signerName):
            try:
                self._defaultIdentity = identityName
                self._defaultCertificateName = self._identityManager.getDefaultCertificateNameForIdentity(self._defaultIdentity)
                self._defaultKeyName = self._identityManager.getDefaultKeyNameForIdentity(identityName)
            except SecurityException:
                msg = "Identity " + identityName.toUri() + " in configuration does not exist. Please configure the device with this identity first"
                print msg
                if onSetupFailed:
                    onSetupFailed(msg)
            
            # Note we'll not be able to issue face commands before this point
            self._face.setCommandSigningInfo(self._keyChain, self._defaultCertificateName)
            print "default cert name " + self._defaultCertificateName.toUri()
            # Serve our own certificate
            self._certificateContentCache.registerPrefix(Name(self._defaultCertificateName).getPrefix(-1), self.onRegisterFailed)
            self._certificateContentCache.add(self._keyChain.getCertificate(self._defaultCertificateName))

            actualSignerName = self._keyChain.getCertificate(self._defaultCertificateName).getSignature().getKeyLocator().getKeyName()    
            if not signerName:
                print "Deriving from " + actualSignerName.toUri() + " for controller name"
            else:
                if signerName and actualSignerName.toUri() != signerName.toUri():
                    msg = "Configuration signer names mismatch: expected " + signerName.toUri() + "; got " + actualSignerName.toUri()
                    print msg
                    if onSetupFailed:
                        onSetupFailed(msg)

            self._controllerName = self.getIdentityNameFromCertName(actualSignerName)
            print "Controller name: " + self._controllerName.toUri()

            try:
                self._controllerCertificate = self._keyChain.getCertificate(self._identityManager.getDefaultCertificateNameForIdentity(self._controllerName))
                
                # TODO: this does not seem a good approach, implementation-wise and security implication
                self._policyManager._certificateCache.insertCertificate(self._controllerCertificate)
                if onSetupComplete:
                    onSetupComplete(Name(self._defaultCertificateName), self._keyChain)
            except SecurityException as e:
                print "don't have controller certificate " + actualSignerName.toUri() + " yet"
                controllerCertInterest = Interest(Name(actualSignerName))
                controllerCertInterest.setInterestLifetimeMilliseconds(4000)
                
                self._face.expressInterest(controllerCertInterest, 
                  lambda interest, data: self.onControllerCertData(interest, data, onSetupComplete, onSetupFailed), 
                  lambda interest: self.onControllerCertTimeout(interest, onSetupComplete, onSetupFailed))
            return

        if isinstance(defaultIdentityOrFileName, basestring):
            confObj = self.processConfiguration(defaultIdentityOrFileName)
            if "identity" in confObj:
                if confObj["identity"] == "default":
                    # TODO: handling the case where no default identity is present
                    defaultIdentity = self._keyChain.getDefaultIdentity()
                else:
                    defaultIdentity = Name(confObj["identity"])
            else:
                defaultIdentity = self._keyChain.getDefaultIdentity()

            # TODO: handling signature with direct bits instead of keylocator keyname
            if "signer" in confObj:    
                if confObj["signer"] == "default":
                    signerName = None
                else:
                    signerName = Name(confObj["signer"])
            else:
                signerName = None
                print "Deriving from " + signerName.toUri() + " for controller name"

            helper(defaultIdentity, signerName)
        else:
            if isinstance(defaultIdentityOrFileName, Name):
                helper(defaultIdentityOrFileName, signerName)
            else:
                raise RuntimeError("Please call setupDefaultIdentityAndRoot with identity name and root key name")
        return

    def onControllerCertData(self, interest, data, onSetupComplete, onSetupFailed):
        # TODO: verification rule for received self-signed cert. 
        # So, if a controller comes masquerading in at this point with the right name, it is problematic. Similar with ndn-pi's implementation
        self._controllerCertificate = IdentityCertificate(data)
        # insert root certificate so that we could verify initial trust schemas
        # TODO: this does not seem a good approach, implementation-wise and security implication
        self._keyChain.getPolicyManager()._certificateCache.insertCertificate(self._controllerCertificate)
        try:
            self._identityManager.addCertificate(self._controllerCertificate)
        except SecurityException as e:
            print str(e)
        for schema in self._trustSchemas:
            # TODO: remove the concept of pending-schema
            if "pending-schema" in self._trustSchemas[schema]:
                self._keyChain.verifyData(self._trustSchemas[schema]["pending-schema"], self.onSchemaVerified, self.onSchemaVerificationFailed)
        if onSetupComplete:
            onSetupComplete(Name(self._defaultCertificateName), self._keyChain)
        return

    def onControllerCertTimeout(self, interest, onSetupComplete, onSetupFailed):
        print "Controller certificate interest times out"
        newInterest = Interest(interest)
        newInterest.refreshNonce()
        self._face.expressInterest(newInterest, 
          lambda interest, data: self.onControllerCertData(interest, data, onSetupComplete, onSetupFailed), 
          lambda interest: self.onControllerCertTimeout(interest, onSetupComplete, onSetupFailed))
        return

#########################################################
# Handling application consumption (trust schema updates)
#########################################################
    # TODO: if trust schema gets over packet size limit, segmentation
    def startTrustSchemaUpdate(self, appPrefix, onUpdateSuccess = None, onUpdateFailed = None):
        """
        Starts trust schema update for under an application prefix: initial 
        interest asks for the rightMostChild, and later interests are sent 
        with previous version excluded. Each verified trust schema will trigger
        onUpdateSuccess and update the ConfigPolicyManager for the keyChain
        in this instance, and unverified ones will trigger onUpdateFailed.

        The keyChain and trust anchor should be set up using setupDefaultIdentityAndRoot
        before calling this method. 

        :param appPrefix: the prefix to ask trust schema for. (interest name: /<prefix>/_schema)
        :type appPrefix: Name
        :param onUpdateSuccess: (optional) onUpdateSuccess(trustSchemaStr, isInitial) is 
          called when update succeeds
        :type onUpdateSuccess: function object
        :param onUpdateFailed: (optional) onUpdateFailed(msg) is called when update fails
        :type onUpdateFailed: function object
        """
        namespace = appPrefix.toUri()
        if namespace in self._trustSchemas:
            if self._trustSchemas[namespace]["following"] == True:
                print "Already following trust schema under this namespace!"
                return
            self._trustSchemas[namespace]["following"] = True
        else:
            self._trustSchemas[namespace] = {"following": True, "version": 0, "is-initial": True}

        initialInterest = Interest(Name(namespace).append("_schema"))
        initialInterest.setChildSelector(1)
        self._face.expressInterest(initialInterest, 
          lambda interest, data: self.onTrustSchemaData(interest, data, onUpdateSuccess, onUpdateFailed), 
          lambda interest: self.onTrustSchemaTimeout(interest, onUpdateSuccess, onUpdateFailed))
        return

    def stopTrustSchemaUpdate(self):
        print "stopTrustSchemaUpdate not implemented"
        return

    def onSchemaVerified(self, data, onUpdateSuccess, onUpdateFailed):
        print "trust schema verified: " + data.getName().toUri()
        version = data.getName().get(-1)
        namespace = data.getName().getPrefix(-2).toUri()
        if not (namespace in self._trustSchemas):
            print "unexpected: received trust schema for application namespace that's not being followed; malformed data name?"
            return

        if version.toVersion() <= self._trustSchemas[namespace]["version"]:
            msg = "Got out-of-date trust schema"
            print msg
            if onUpdateFailed:
                onUpdateFailed(msg)
            return

        self._trustSchemas[namespace]["version"] = version.toVersion()
        
        if "pending-schema" in self._trustSchemas[namespace] and self._trustSchemas[namespace]["pending-schema"].getName().toUri() == data.getName().toUri():
            # we verified a pending trust schema, don't need to keep that any more
            del self._trustSchemas[namespace]["pending-schema"]

        self._trustSchemas[namespace]["trust-schema"] = data.getContent().toRawStr()
        print self._trustSchemas[namespace]["trust-schema"]

        # TODO: what about trust schema for discovery, is discovery its own application?
        newInterest = Interest(Name(data.getName()).getPrefix(-1))
        newInterest.setChildSelector(1)
        exclude = Exclude()
        exclude.appendAny()
        exclude.appendComponent(version)
        newInterest.setExclude(exclude)
        self._face.expressInterest(newInterest, 
          lambda interest, data: self.onTrustSchemaData(interest, data, onUpdateSuccess, onUpdateFailed), 
          lambda interest: self.onTrustSchemaTimeout(interest, onUpdateSuccess, onUpdateFailed))

        # Note: this changes the verification rules for root cert, future trust schemas as well; ideally from the outside this doesn't have an impact, but do we want to avoid this?
        # Per reset function in ConfigPolicyManager; For now we don't call reset as we still want root cert in our certCache, instead of asking for it again (when we want to verify) each time we update the trust schema
        self._policyManager.config = BoostInfoParser()
        self._policyManager.config.read(self._trustSchemas[namespace]["trust-schema"], "updated-schema")
        
        if onUpdateSuccess:
            onUpdateSuccess(data.getContent().toRawStr(), self._trustSchemas[namespace]["is-initial"])
        self._trustSchemas[namespace]["is-initial"] = False
        return

    def onSchemaVerificationFailed(self, data, reason, onUpdateSuccess, onUpdateFailed):
        print "trust schema verification failed: " + reason
        namespace = data.getName().getPrefix(-2).toUri()
        if not (namespace in self._trustSchemas):
            print "unexpected: received trust schema for application namespace that's not being followed; malformed data name?"
            return
        
        newInterest = Interest(Name(data.getName()).getPrefix(-1))
        newInterest.setChildSelector(1)
        exclude = Exclude()
        exclude.appendAny()
        exclude.appendComponent(Name.Component.fromVersion(self._trustSchemas[namespace]["version"]))
        newInterest.setExclude(exclude)
        # Don't immediately ask for potentially the same content again if verification fails
        self._face.callLater(4000, lambda : 
          self._face.expressInterest(newInterest, 
            lambda interest, data: self.onTrustSchemaData(interest, data, onUpdateSuccess, onUpdateFailed), 
            lambda interest: self.onTrustSchemaTimeout(interest, onUpdateSuccess, onUpdateFailed)))
        return

    def onTrustSchemaData(self, interest, data, onUpdateSuccess, onUpdateFailed):
        print("Trust schema received: " + data.getName().toUri())
        namespace = data.getName().getPrefix(-2).toUri()
        # Process newly received trust schema
        if not self._controllerCertificate:
            # we don't yet have the root certificate fetched, so we store this cert for now
            print "Controller certificate not yet present, verify once it's in place"
            self._trustSchemas[namespace]["pending-schema"] = data
        else:
            # we veriy the received trust schema, should we use an internal KeyChain instead?
            self._keyChain.verifyData(data, 
              lambda data: self.onSchemaVerified(data, onUpdateSuccess, onUpdateFailed), 
              lambda data, reason: self.onSchemaVerificationFailed(data, reason, onUpdateSuccess, onUpdateFailed))

        return

    def onTrustSchemaTimeout(self, interest, onUpdateSuccess, onUpdateFailed):
        print("Trust schema interest times out: " + interest.getName().toUri())
        newInterest = Interest(interest)
        newInterest.refreshNonce()
        self._face.expressInterest(newInterest, 
          lambda interest, data: self.onTrustSchemaData(interest, data, onUpdateSuccess, onUpdateFailed), 
          lambda interest: self.onTrustSchemaTimeout(interest, onUpdateSuccess, onUpdateFailed))        
        return

###############################################
# Handling application producing authorizations
###############################################
    # Wrapper for sendAppRequest, fills in already configured defaultCertificateName
    def requestProducerAuthorization(self, dataPrefix, appName, onRequestSuccess = None, onRequestFailed = None):
        """
        Requests producing authorization for a data prefix: commandInterest is sent out 
        to the controller, using /<controller identity>/requests/<encoded-application-parameters>/<signed-interest-suffix>
        where encoded-application-parameters is a ProtobufTlv encoding of 
        {appPrefix, certificateName, appName}

        The keyChain, trust anchor and controller name should be set up using 
        setupDefaultIdentityAndRoot before calling this method.

        :param dataPrefix: the prefix to request publishing for
        :type dataPrefix: Name
        :param appName: the application name to request publishing for
        :type appName: str
        :param onRequestSuccess: (optional) onRequestSuccess() is called when a valid response
          if received for the request
        :type onRequestSuccess: function object
        :param onRequestFailed: (optional) onRequestFailed(msg) is called when request fails
        :type onRequestFailed: function object
        """
        # TODO: update logic on this part, should the presence of default certificate name be mandatory? 
        # And allow application developer to send app request to a configured root/controller?
        if not self._defaultCertificateName:
            raise RuntimeError("Default certificate is missing! Try setupDefaultIdentityAndRoot first?")
            return
        self.sendAppRequest(self._defaultCertificateName, dataPrefix, appName, onRequestSuccess, onRequestFailed)

    def sendAppRequest(self, certificateName, dataPrefix, applicationName, onRequestSuccess, onRequestFailed):
        message = AppRequestMessage()

        for component in range(certificateName.size()):
            message.command.idName.components.append(certificateName.get(component).toEscapedString())
        for component in range(dataPrefix.size()):
            message.command.dataPrefix.components.append(dataPrefix.get(component).toEscapedString())
        message.command.appName = applicationName

        paramComponent = ProtobufTlv.encode(message)

        requestInterest = Interest(Name(self._controllerName).append("requests").append(paramComponent))

        requestInterest.setInterestLifetimeMilliseconds(4000)
        self._face.makeCommandInterest(requestInterest)
        
        self._face.expressInterest(requestInterest, 
          lambda interest, data : self.onAppRequestData(interest, data, onRequestSuccess, onRequestFailed), 
          lambda interest : self.onAppRequestTimeout(interest, onRequestSuccess, onRequestFailed))
        print "Application publish request sent: " + requestInterest.getName().toUri()
        return

    def onAppRequestData(self, interest, data, onRequestSuccess, onRequestFailed):
        print "Got application publishing request data"
        def onVerified(data):
            responseObj = json.loads(data.getContent().toRawStr())
            if responseObj["status"] == "200":
                if onRequestSuccess:
                    onRequestSuccess()
                else:
                    print "onSetupComplete"
            else:
                print "Verified content: " + data.getContent().toRawStr()
                if onRequestFailed:
                    onRequestFailed(data.getContent().toRawStr())
        def onVerifyFailed(data, reason):
            msg = "Application request response verification failed: " + reason
            print msg
            if onRequestFailed:
                onRequestFailed(msg)

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

###############################################
# Helper functions
###############################################
    def onRegisterFailed(self, prefix):
        print("register failed for prefix " + prefix.getName().toUri())
        return

    def processConfiguration(self, confFile):
        config = BoostInfoParser()
        config.read(confFile)

        # TODO: handle missing configuration, refactor dict representation
        confObj = dict()
        try:
            confObj["identity"] = config["application/identity"][0].value
            confObj["signer"] = config["application/signer"][0].value
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

        return Name(certName.getPrefix(i))

#################################
# Getters and setters
#################################
    def getKeyChain(self):
        return self._keyChain
