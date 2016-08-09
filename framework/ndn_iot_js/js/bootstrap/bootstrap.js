var Bootstrap = function Bootstrap(face)
{
    this.identityManager = new IdentityManager(new IndexedDbIdentityStorage());
    
    var defaultPolicy = 
      "validator"                + "\n" +
      "{"                        + "\n" +
      "  rule"                   + "\n" +
      "  {"                      + "\n" +
      "    id \"initial rule\""  + "\n" +
      "    for data"             + "\n" +
      "    checker"              + "\n" +
      "    {"                    + "\n" +
      "      type hierarchical"  + "\n" +
      "    }"                    + "\n" +
      "  }"                      + "\n" +
      "}";

    this.policyManager = var ConfigPolicyManager();
    this.policyManager.load(defaultPolicy, "default-policy");

    // keyChain is what we return to the application after successful setup
    // TODO: should we separate keyChain from internal KeyChain used to verify trust schemas?
    this.keyChain = new KeyChain(this.identityManager, this.policyManager)

    this.face = face;
    // setFace for keyChain or else it won't be able to express interests for certs
    this.keyChain.setFace(this.face);
    this.certificateContentCache = new MemoryContentCache(this.face);
    
    this.trustSchemas = {};
}
        
/**
 * Initial keyChain and defaultCertificate setup
 */
Bootstrap.prototype.setupDefaultIdentityAndRoot = function(defaultIdentity, signerName, onSetupComplete, onSetupFailed)
{
    var self = this;
    this.identityManager.getDefaultCertificateNameForIdentity(defaultIdentity, function (defaultCertificateName) {
        self.defaultIdentity = defaultIdentity;
        self.defaultCertificateName = defaultCertificateName;
        self.identityManager.getDefaultKeyNameForIdentity(defaultIdentity, function (defaultKeyName) {
            self.defaultKeyName = defaultKeyName;
            // Note we'll not be able to issue face commands before this point
            self.face.setCommandSigningInfo(self.keyChain, self.defaultCertificateName);
            // Serve our own certificate
            self.certificateContentCache.registerPrefix(new Name(this.defaultCertificateName.getPrefix(-1)), function (prefix) {
                console.log("Prefix registration failed: " + prefix.toUri());
            });
            self.keyChain.getCertificate(self.defaultCertificateName, function (certificate) {
                self.certificateContentCache.add(certificate);
                var actualSignerName = certificate.getSignature().getKeyLocator().getKeyName();
                if (actualSignerName.toUri() !== signerName.toUri()) {
                    var msg = "Configuration signer names mismatch: expected " + signerName.toUri() + "; got " + actualSignerName.toUri();
                    if (onSetupFailed !== undefined)
                        onSetupFailed(msg);
                    return;
                }
                self.controllerName = self.getIdentityNameFromCertName(actualSignerName);
                console.log("Controller name: " + self.controllerName.toUri());
                // TODO: check if we have controller certificate
            }, function (error) {
                var msg = "Certificate does not exist " + self.defaultCertificateName.toUri();
                if (onSetupFailed !== undefined)
                    onSetupFailed(msg);
                return;
            });
        }, function (error) {
            console.log(error);
            var msg = "Identity " + defaultIdentity.toUri() + " in configuration does not have a default key. Please configure the device with this identity first";
            if (onSetupFailed !== undefined)
                onSetupFailed(msg);
            return;
        });
    }, function (error) {
        console.log(error);
        var msg = "Identity " + defaultIdentity.toUri() + " in configuration does not have a default certificate. Please configure the device with this identity first";
        if (onSetupFailed !== undefined)
            onSetupFailed(msg);
        return;
    });

}
            

            try:
                identityName = self.getIdentityNameFromCertName(actualSignerName)
                self._controllerCertificate = self._keyChain.getCertificate(self._identityManager.getDefaultCertificateNameForIdentity(identityName))
                self._controllerCertificate.getName().toUri()
                # TODO: this does not seem a good approach, implementation-wise and security implication
                self._keyChain.getPolicyManager()._certificateCache.insertCertificate(self._controllerCertificate)
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
            if signerName and isinstance(defaultIdentityOrFileName, Name):
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
        excludeComponent = data.getName().get(-1)
        exclude = Exclude()
        exclude.appendAny()
        exclude.appendComponent(version)
        newInterest.setExclude(exclude)
        self._face.expressInterest(newInterest, 
          lambda interest, data: self.onTrustSchemaData(interest, data, onUpdateSuccess, onUpdateFailed), 
          lambda interest: self.onTrustSchemaTimeout(interest, onUpdateSuccess, onUpdateFailed))

        # Note: this changes the verification rules for root cert, future trust schemas as well; ideally from the outside this doesn't have an impact, but do we want to avoid this?
        # Per reset function in ConfigPolicyManager; For now we don't call reset as we still want root cert in our certCache, instead of asking for it again (when we want to verify) each time we update the trust schema
        self._keyChain.getPolicyManager().config = BoostInfoParser()
        self._keyChain.getPolicyManager().config.read(self._trustSchemas[namespace]["trust-schema"], "use-string!")
        
        if onUpdateSuccess:
            onUpdateSuccess(data.getContent().toRawStr(), self._trustSchemas[namespace]["is-initial"])
        self._trustSchemas[namespace]["is-initial"] = False
        return

    def onSchemaVerificationFailed(self, data, onUpdateSuccess, onUpdateFailed):
        print "trust schema verification failed"
        namespace = data.getName().getPrefix(-2).toUri()
        if not (namespace in self._trustSchemas):
            print "unexpected: received trust schema for application namespace that's not being followed; malformed data name?"
            return
        
        newInterest = Interest(Name(data.getName()).getPrefix(-1))
        newInterest.setChildSelector(1)
        excludeComponent = data.getName().get(-1)
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
              lambda data: self.onSchemaVerificationFailed(data, onUpdateSuccess, onUpdateFailed))

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

        requestInterest = Interest(Name(self._controllerName).append("requests").appendVersion(int(time.time())).append(paramComponent))

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
            if data.getContent().toRawStr() == "200":
                if onRequestSuccess:
                    onRequestSuccess()
                else:
                    print "onSetupComplete"
            else:
                print "Verified content: " + data.getContent().toRawStr()
                if onRequestFailed:
                    onRequestFailed(data.getContent().toRawStr())
        def onVerifyFailed(data):
            msg = "Application request response verification failed!"
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

        return certName.getPrefix(i)

#################################
# Getters and setters
#################################
    def getKeyChain(self):
        return self._keyChain
