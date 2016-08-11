var Bootstrap = function Bootstrap(face)
{
    this.identityManager = new IdentityManager(new IndexedDbIdentityStorage(), new IndexedDbPrivateKeyStorage());
    
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

    this.policyManager = new ConfigPolicyManager();
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
Bootstrap.prototype.setupDefaultIdentityAndRoot = function(identityName, signerName, onSetupComplete, onSetupFailed)
{
    var self = this;

    function helper (defaultIdentity) {
        self.identityManager.getDefaultCertificateNameForIdentity(defaultIdentity, function (defaultCertificateName) {
            self.defaultIdentity = defaultIdentity;
            self.defaultCertificateName = defaultCertificateName;

            self.identityManager.getDefaultKeyNameForIdentity(defaultIdentity, function (defaultKeyName) {
                self.defaultKeyName = defaultKeyName;
                // Note we'll not be able to issue face commands before this point
                self.face.setCommandSigningInfo(self.keyChain, self.defaultCertificateName);
                // Serve our own certificate
                self.certificateContentCache.registerPrefix(new Name(self.defaultCertificateName.getPrefix(-1)), function (prefix) {
                    console.log("Prefix registration failed: " + prefix.toUri());
                });
                self.keyChain.getCertificate(self.defaultCertificateName, function (certificate) {
                    self.certificateContentCache.add(certificate);
                    var actualSignerName = certificate.getSignature().getKeyLocator().getKeyName();
                    if (signerName !== undefined && actualSignerName.toUri() !== signerName.toUri()) {
                        var msg = "Configuration signer names mismatch: expected " + signerName.toUri() + "; got " + actualSignerName.toUri();
                        if (onSetupFailed !== undefined)
                            onSetupFailed(msg);
                        return;
                    }
                    self.controllerName = self.getIdentityNameFromCertName(actualSignerName);
                    console.log("Controller name: " + self.controllerName.toUri());
                    
                    self.identityManager.getDefaultCertificateNameForIdentity(self.controllerName, function (certificateName) {
                        self.keyChain.getCertificate(certificateName, function (certificate) {
                            self.controllerCertificate = certificate;   
                            // TODO: this does not seem a good approach, implementation-wise and security implication
                            self.policyManager.certificateCache.insertCertificate(self.controllerCertificate);
                            if (onSetupComplete !== undefined) {
                                onSetupComplete(new Name(self.defaultCertificateName), self.keyChain);
                            }
                        }, function (error) {
                            var msg = "Cannot find controller certificate: " + certificateName.toUri();
                            console.log(error);
                            if (onSetupFailed !== undefined) {
                                onSetupFailed(msg);
                            }
                        })
                    }, function (error) {
                        var msg = "Cannot find default ceritificate for controller identity: " + self.controllerName.toUri();
                        console.log(error);
                        if (onSetupFailed !== undefined) {
                            onSetupFailed(msg);
                        }
                    });
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
    
    if (identityName === undefined) {
        this.identityManager.getDefaultIdentity(function (defaultIdentityName) {
            helper(defaultIdentityName);
        }, function (error) {
            var msg = "Default identity not configured";
            if (onSetupFailed !== undefined)
                onSetupFailed(msg);
            return;
        });
    } else {
        helper(identityName);
    }
}

/**
 * We don't ask for controller certificate in this implementation, but expects it to be existent per the add device step
 */
/*
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
*/

/**
 * Handling application consumption (trust schema updates)
 */
// TODO: if trust schema gets over packet size limit, segmentation
Bootstrap.prototype.startTrustSchemaUpdate = function (appPrefix, onUpdateSuccess, onUpdateFailed)
{
    var namespace = appPrefix.toUri()
    if (namespace in this.trustSchemas) {
        if (this.trustSchemas[namespace]["following"] == true) {
            console.log("already following trust schema under this namespace: " + namespace);
            return;
        }
        this.trustSchemas[namespace]["following"] = true;
    } else {
        this.trustSchemas[namespace] = {"following": true, "version": 0, "is-initial": true};
    }

    var initialInterest = new Interest(new Name(namespace).append("_schema"));
    initialInterest.setChildSelector(1);
    var self = this;
    this.face.expressInterest(initialInterest, function (interest, data) {
        self.onTrustSchemaData(interest, data, onUpdateSuccess, onUpdateFailed);
    }, function (interest) {
        self.onTrustSchemaTimeout(interest, onUpdateSuccess, onUpdateFailed);
    });
    return;
}
        
Bootstrap.prototype.stopTrustSchemaUpdate = function ()
{
    console.log("stopTrustSchemaUpdate not implemented");
    return;
}

Bootstrap.prototype.onSchemaVerified = function (data, onUpdateSuccess, onUpdateFailed)
{
    console.log("trust schema verified: " + data.getName().toUri());
    var version = data.getName().get(-1);
    var namespace = data.getName().getPrefix(-2).toUri();

    if (!(namespace in this.trustSchemas)) {
        console.log("unexpected: received trust schema for application namespace that's not being followed; malformed data name?");
        return;
    }

    if (version.toVersion() <= this.trustSchemas[namespace]["version"]) {
        var msg = "Got out-of-date trust schema";
        console.log(msg);
        if (onUpdateFailed !== undefined) {
            onUpdateFailed(msg);
        }
        return;
    }

    this.trustSchemas[namespace]["version"] = version.toVersion();
    // Remove pending trust schema (while fetching root certificate) logic
    
    var trustSchemaString = data.getContent().toString("binary");
    this.trustSchemas[namespace]["trust-schema"] = trustSchemaString;

    // TODO: what about trust schema for discovery, is discovery its own application?
    var newInterest = new Interest(new Name(data.getName()).getPrefix(-1));
    newInterest.setChildSelector(1);
    var excludeComponent = data.getName().get(-1);
    var exclude = new Exclude();
    exclude.appendAny();
    exclude.appendComponent(version);
    newInterest.setExclude(exclude);
    var self = this;
    this.face.expressInterest(newInterest, function (interest, data) {
        self.onTrustSchemaData(interest, data, onUpdateSuccess, onUpdateFailed);
    }, function (interest) {
        self.onTrustSchemaTimeout(interest, onUpdateSuccess, onUpdateFailed);
    });

    // Note: this changes the verification rules for root cert, future trust schemas as well; ideally from the outside this doesn't have an impact, but do we want to avoid this?
    // Per reset function in ConfigPolicyManager; For now we don't call reset as we still want root cert in our certCache, instead of asking for it again (when we want to verify) each time we update the trust schema
    // TODO: check if the above note holds for our JS implementation and whether it matters as we are using base64 root by default
    this.policyManager.load(trustSchemaString, "updated-schema");

    if (onUpdateSuccess !== undefined) {
        onUpdateSuccess(trustSchemaString, this.trustSchemas[namespace]["is-initial"]);
    }
    this.trustSchemas[namespace]["is-initial"] = false;
    return;
}

Bootstrap.prototype.onSchemaVerificationFailed = function (data, onUpdateSuccess, onUpdateFailed)
{
    console.log("trust schema verification failed");
    var namespace = data.getName().getPrefix(-2).toUri();
    if (!(namespace in self._trustSchemas)) {
        console.log("unexpected: received trust schema for application namespace that's not being followed; malformed data name?");
        return ;
    }
    
    var newInterest = new Interest(new Name(data.getName()).getPrefix(-1));
    newInterest.setChildSelector(1);
    var excludeComponent = data.getName().get(-1);
    var exclude = new Exclude();
    exclude.appendAny();
    exclude.appendComponent(Name.Component.fromVersion(this.trustSchemas[namespace]["version"]));
    newInterest.setExclude(exclude);

    // Don't immediately ask for potentially the same content again if verification fails
    var self = this;
    setTimeout(4000, function () {
        self.face.expressInterest(newInterest, function (interest, data) {
            self.onTrustSchemaData(interest, data, onUpdateSuccess, onUpdateFailed);
        }, function (interest) {
            self.onTrustSchemaTimeout(interest, onUpdateSuccess, onUpdateFailed);
        })
    });
    return;
}

Bootstrap.prototype.onTrustSchemaData = function (interest, data, onUpdateSuccess, onUpdateFailed)
{
    console.log("Trust schema received: " + data.getName().toUri());
    var namespace = data.getName().getPrefix(-2).toUri();

    // Process newly received trust schema
    if (this.controllerCertificate === undefined) {
        // We should have controller certificate, don't do pending schema in JS implementation
        console.log("Controller certificate not yet present, verify once it's in place");
    } else {
        // we veriy the received trust schema, should we use an internal KeyChain instead?
        var self = this;
        this.keyChain.verifyData(data, function (data) {
            self.onSchemaVerified(data, onUpdateSuccess, onUpdateFailed);
        }, function (data) {
            self.onSchemaVerificationFailed(data, onUpdateSuccess, onUpdateFailed); 
        });
    }
        
    return;
}

Bootstrap.prototype.onTrustSchemaTimeout = function (interest, onUpdateSuccess, onUpdateFailed)
{
    console.log("Trust schema interest times out: " + interest.getName().toUri());
    var newInterest = new Interest(interest);
    newInterest.refreshNonce();
    var self = this;
    this.face.expressInterest(newInterest, function (interest, data) {
        self.onTrustSchemaData(interest, data, onUpdateSuccess, onUpdateFailed);
    }, function (interest) {
        self.onTrustSchemaTimeout(interest, onUpdateSuccess, onUpdateFailed);
    });
    return
}

/**
 * Handling application producing authorizations
 */
// Wrapper for sendAppRequest, fills in already configured defaultCertificateName
Bootstrap.prototype.requestProducerAuthorization = function (dataPrefix, appName, onRequestSuccess, onRequestFailed)
{
    // TODO: update logic on this part, should the presence of default certificate name be mandatory? 
    // And allow application developer to send app request to a configured root/controller?
    if (this.defaultCertificateName === undefined) {
        console.log("Default certificate is missing! Try setupDefaultIdentityAndRoot first?");
        return;
    }

    this.sendAppRequest(this.defaultCertificateName, dataPrefix, appName, onRequestSuccess, onRequestFailed);
}
        
Bootstrap.prototype.sendAppRequest = function (certificateName, dataPrefix, applicationName, onRequestSuccess, onRequestFailed)
{
    var ProtoBuf = dcodeIO.ProtoBuf;
    var builder = ProtoBuf.loadProtoFile('app-request.proto');
    var descriptor = builder.lookup('AppRequestMessage');
    var AppRequestMessage = descriptor.build();

    var message = new AppRequestMessage();
    message.command = new AppRequestMessage.AppRequest();
    message.command.idName = new AppRequestMessage.Name();
    message.command.dataPrefix = new AppRequestMessage.Name();
    
    for (var i = 0; i < certificateName.size(); i++) {
        message.command.idName.add("components", certificateName.get(i).getValue().buf());
    }
    for (var i = 0; i < dataPrefix.size(); i++) {
        message.command.dataPrefix.add("components", dataPrefix.get(i).getValue().buf());
    }

    message.command.appName = applicationName;

    var paramComponent = new Name.Component(ProtobufTlv.encode(message, descriptor));
    var requestInterest = new Interest
      (new Name(this.controllerName).append("requests").append(paramComponent));
    requestInterest.setInterestLifetimeMilliseconds(4000);
    this.face.makeCommandInterest(requestInterest);
    var self = this;
    this.face.expressInterest(requestInterest, function (interest, data) {
        self.onAppRequestData(interest, data, onRequestSuccess, onRequestFailed); 
    }, function (interest) {
        self.onAppRequestTimeout(interest, onRequestSuccess, onRequestFailed);
    });
    console.log("Application publish request sent: " + requestInterest.getName().toUri())
    return;
}
        
Bootstrap.prototype.onAppRequestData = function (interest, data, onRequestSuccess, onRequestFailed)
{
    console.log("Got application publishing request data");
    function onVerified(data) {
        var responseObj = JSON.parse(data.getContent().toString("binary"));
        if (responseObj["status"] == "200") {
            if (onRequestSuccess !== undefined) {
                onRequestSuccess();
            }
        } else {
            console.log("Verified content: " + data.getContent().toString("binary"));
            if (onRequestFailed !== undefined) {
                onRequestFailed(data.getContent().toString("binary"));
            }
        }
    }
        
    function onVerifyFailed(data) {
        var msg = "Application request response verification failed!"
        console.log(msg);
        if (onRequestFailed !== undefined) {
            onRequestFailed(msg);
        }
    }
        
    this.keyChain.verifyData(data, onVerified, onVerifyFailed);
    return;
}
    
Bootstrap.prototype.onAppRequestTimeout = function(interest, onSetupComplete, onSetupFailed)
{
    console.log("Application publishing request times out");
    var newInterest = new Interest(interest);
    newInterest.refreshNonce();
    var self = this;
    this.face.expressInterest(newInterest, function (interest, data) {
        self.onAppRequestData(interest, data, onSetupComplete, onSetupFailed); 
    }, function (interest) {
        self.onAppRequestTimeout(interest, onSetupComplete, onSetupFailed);
    });
    return;    
}

/**
 * Helper functions
 */
Bootstrap.prototype.onRegisterFailed = function (prefix)
{
    console.log("register failed for prefix " + prefix.getName().toUri());
    return;
}

/*
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
*/

Bootstrap.prototype.getIdentityNameFromCertName = function (certName)
{
    var i = certName.size() - 1;

    var idString = "KEY";
    while (i >= 0) {
        if (certName.get(i).toEscapedString() == idString) {
            break;            
        }
        i -= 1;
    }
        
    if (i < 0) {
        console.log("Error: unexpected certName " + certName.toUri())
        return;
    }
    
    return new Name(certName.getPrefix(i));
}
    
/**
 * Getters and setters
 */
Bootstrap.prototype.getKeyChain = function()
{
    return this.keyChain;
}