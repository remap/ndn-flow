var HmacHelper = function HmacHelper()
{

};

HmacHelper.generatePin = function()
{
    pin = "";
    for (var i = 0; i < 8; i++) {
        pin += String.fromCharCode(Math.round(Math.random() * 255));
    }
    return DataUtils.stringToHex(pin);
};

HmacHelper.signInterest = function(interest, key, keyName, wireFormat)
{
    wireFormat = (typeof wireFormat === "function" || !wireFormat) ? WireFormat.getDefaultWireFormat() : wireFormat;

    // The random value is a TLV nonNegativeInteger too, but we know it is 8
    // bytes, so we don't need to call the nonNegativeInteger encoder.
    interest.getName().append(new Blob(Crypto.randomBytes(8), false));

    var timestamp = Math.round(new Date().getTime());
    // The timestamp is encoded as a TLV nonNegativeInteger.
    var encoder = new TlvEncoder(8);
    encoder.writeNonNegativeInteger(timestamp);
    interest.getName().append(new Blob(encoder.getOutput(), false));

    var s = new HmacWithSha256Signature();
    s.getKeyLocator().setType(KeyLocatorType.KEYNAME);
    s.getKeyLocator().setKeyName(keyName);
    
    interest.getName().append(wireFormat.encodeSignatureInfo(s));
    interest.getName().append(new Name.Component());

    var encoding = interest.wireEncode(wireFormat);
    var signer = Crypto.createHmac('sha256', key.buf());
    signer.update(encoding.signedBuf());
    s.setSignature(new Blob(signer.digest(), false));
    interest.setName(interest.getName().getPrefix(-1).append(wireFormat.encodeSignatureValue(s)));
}

HmacHelper.extractInterestSignature = function(interest, wireFormat)
{
    wireFormat = (typeof wireFormat === "function" || !wireFormat) ? WireFormat.getDefaultWireFormat() : wireFormat;
    
    try {
        signature = wireFormat.decodeSignatureInfoAndValue(
                        interest.getName().get(-2).getValue().buf(),
                        interest.getName().get(-1).getValue().buf());
    } catch (e) {
        console.log(e);
        signature = null;
    }

    return signature;
}

HmacHelper.verifyInterest = function(interest, key, wireFormat)
{
    wireFormat = (typeof wireFormat === "function" || !wireFormat) ? WireFormat.getDefaultWireFormat() : wireFormat;

    var signature = HmacHelper.extractInterestSignature(interest, wireFormat);
    var encoding = interest.wireEncode(wireFormat);

    var signer = Crypto.createHmac('sha256', key.buf());
    signer.update(encoding.signedBuf());
    var newSignatureBits = new Blob(signer.digest(), false);
    
    // Use the flexible Blob.equals operator.
    return newSignatureBits.equals(signature.getSignature());
};

/**
 * Data signing and verification were using library functions in the add-device script
 */

HmacHelper.signData = function(data, key, keyName, wireFormat)
{
    wireFormat = (typeof wireFormat === "function" || !wireFormat) ? WireFormat.getDefaultWireFormat() : wireFormat;

    data.setSignature(new HmacWithSha256Signature());
    var s = data.getSignature();

    s.getKeyLocator().setType(KeyLocatorType.KEYNAME);
    s.getKeyLocator().setKeyName(keyName);

    var encoding = data.wireEncode(wireFormat);
    var signer = Crypto.createHmac('sha256', key.buf());
    signer.update(encoding.signedBuf());
    s.setSignature(new Blob(signer.digest(), false));
    data.wireEncode(wireFormat);
};
     
HmacHelper.verifyData = function(data, key, wireFormat)
{
    wireFormat = (typeof wireFormat === "function" || !wireFormat) ? WireFormat.getDefaultWireFormat() : wireFormat;
    var encoding = data.wireEncode(wireFormat);
        
    var signer = Crypto.createHmac('sha256', key.buf());
    signer.update(encoding.signedBuf());
    var newSignatureBits = new Blob(signer.digest(), false);
    var sigBytes = data.getSignature().getSignature();
    
    // Use the flexible Blob.equals operator.
    return newSignatureBits.equals(sigBytes);
};      default_prefix = new Name("/home/configure");

var IotNode = function IotNode(host, dbName)
{
    this.face = new Face({host: host});
    if (dbName === undefined) {
        dbName = "iot-db";
    }
    if (typeof Dexie !== 'undefined') {
        this.database = new Dexie(dbName);
        
        // our DB stores the serial
        this.database.version(1).stores({
            device: "serial"
        });
        this.database.open().catch(function(error) {
            console.log("Dexie open DB error " + error);
        });
    } else {
        console.log("Dexie not defined");
    }

    this.getSerial();

    // KeyChain and Identity set up
    this.identityStorage = new IndexedDbIdentityStorage();
    this.privateKeyStorage = new IndexedDbPrivateKeyStorage();
    this.identityManager = new IdentityManager(this.identityStorage, this.privateKeyStorage);
    this.policyManager = new ConfigPolicyManager();
    this.keyChain = new KeyChain(this.identityManager, this.policyManager);
    this.keyChain.setFace(this.face);
    
    // Use this hack so that by the time loop starts we should already have filled in the serial number from IndexedDB
    setTimeout(this.beforeLoopStart.bind(this), 400);
};

IotNode.prototype.getSerial = function()
{
    if (this.serial === undefined) {
        var self = this;
        this.database.device.count(function (number) {
            if (number === 0) {
                id = makeid(6);
                self.database.device.put({"serial": id});
                self.serial = id;
            } else {
                self.database.device.toCollection().first(function (item) {
                    // TODO: this function could probably return a promise to get "serial"
                    self.serial = item["serial"];
                    console.log("Serial: " + self.serial);
                });
            }
        });
    } else {
        console.log("Serial: " + this.serial);
        return this.serial;
    }
};

IotNode.prototype.createNewPin = function()
{
    var pin = HmacHelper.generatePin();
    var hash = Crypto.createHash('sha256');
    hash.update(pin);
    this.key = new Blob(new Buffer(hash.digest()), false);
    return pin;
}

IotNode.prototype.beforeLoopStart = function()
{
    if (this.serial === undefined) {
        console.log("Error: get serial has not yet finished.");
        return;
    }
    console.log("Serial: " + this.serial + "; Configuration PIN: " + this.createNewPin());
    this.prefix = new Name(default_prefix).append(this.serial);

    // Make sure that we are trusted by the remote NFD to registerPrefix
    // For now as a quick hack, we can enable localhop security on the NFD of the web host (or whatever other NFDs this code defaults to)
    var self = this;
    this.keyChain.getDefaultCertificateName(function (certificateName) {
        self.face.setCommandSigningInfo(self.keyChain, certificateName);
        self.face.registerPrefix(self.prefix, self.onConfigurationReceived.bind(self), self.onRegisterFailed.bind(self));
    }, function (error) {
        self.keyChain.createIdentityAndCertificate(new Name("/temp/device/initial"), function (certificateName) {
            self.face.setCommandSigningInfo(self.keyChain, certificateName);
            self.face.setCommandSigningInfo(self.keyChain, certificateName);
            self.face.registerPrefix(self.prefix, self.onConfigurationReceived.bind(self), self.onRegisterFailed.bind(self));
        }, function (error) {
            console.log(error);
        });
    });
}

IotNode.prototype.onRegisterFailed = function(prefix)
{
    console.log("Registration failed for prefix: " + prefix.toUri());
}

IotNode.prototype.onConfigurationReceived = function(prefix, interest, face, interestFilterId, filter)
{
    // the interest we get here is signed by HMAC, let's verify it
    if (HmacHelper.verifyInterest(interest, this.key)) {
        this.tempPrefixId = interestFilterId /// didn't get it from register because of the event loop
        var dataName = new Name(interest.getName());
        var replyData = new Data(dataName);

        // we have a match! decode the controller's name
        var configComponent = interest.getName().get(prefix.size());
        replyData.setContent('200');
        
        KeyChain.signWithHmacWithSha256(replyData, this.key);
        this.face.putData(replyData);

        var ProtoBuf = dcodeIO.ProtoBuf;
        var builder = ProtoBuf.loadProtoFile('../commands/configure-device.proto');
        var descriptor = builder.lookup('DeviceConfigurationMessage');
        var DeviceConfigurationMessage = descriptor.build();

        var environmentConfig = new DeviceConfigurationMessage();
        try {
            ProtobufTlv.decode(environmentConfig, descriptor, configComponent.getValue());
        } catch (ex) {
            console.log(ex);
        }

        var networkPrefix = ProtobufTlv.toName(environmentConfig.configuration.networkPrefix.components);
        var controllerName = new Name(networkPrefix).append(ProtobufTlv.toName(environmentConfig.configuration.controllerName.components));

        this.trustRootIdentity = controllerName;
        console.log(controllerName.toUri());

        this.deviceSuffix = ProtobufTlv.toName(environmentConfig.configuration.deviceSuffix.components);
        this.configureIdentity = new Name(networkPrefix).append(this.deviceSuffix);

        this.sendCertificateRequest(this.configureIdentity);
    }
    // else, ignore!
}

IotNode.prototype.onConfigurationRegistrationFailure = function(prefix)
{
    console.log("Prefix registration failed: " + prefix.toUri());
    return ;
}

/**
 * Certificate signing requests
 * On startup, if we don't have a certificate signed by the controller, we request one.
 */
IotNode.prototype.sendCertificateRequest = function(keyIdentity)
{
    /**
     * We compose a command interest with our public key info so the controller
     * can sign us a certificate that can be used with other nodes in the network.
     */
    var self = this;

    function sendRequest(defaultKey) {
        console.log("Key name: " + defaultKey.toUri());

        var ProtoBuf = dcodeIO.ProtoBuf;
        var builder = ProtoBuf.loadProtoFile('../commands/cert-request.proto');
        var descriptor = builder.lookup('CertificateRequestMessage');
        var CertificateRequestMessage = descriptor.build();

        var message = new CertificateRequestMessage();
        message.command = new CertificateRequestMessage.CertificateRequest();
        message.command.keyName = new CertificateRequestMessage.Name();
        for (var i = 0; i < defaultKey.size(); i++) {
            message.command.keyName.add("components", defaultKey.get(i).getValue().buf());
        }
        self.identityManager.getPublicKey(defaultKey, function (publicKey) {
            message.command.keyType = publicKey.getKeyType();
            //var publicKeyBuffer = new dcodeIO.ByteBuffer();
            message.command.keyBits = dcodeIO.ByteBuffer.wrap(publicKey.getKeyDer().buf());
            //console.log(message.command.keyBits);

            var certRequestComponent = new Name.Component(ProtobufTlv.encode(message, descriptor));
            var interest = new Interest
              (new Name(self.trustRootIdentity).append("certificateRequest").append(certRequestComponent));
            
            interest.setInterestLifetimeMilliseconds(10000);
            HmacHelper.signInterest(interest, self.key, self.prefix);

            console.log("Sending certificate request to controller");
            console.log("Certificate request: " + interest.getName().toUri());
            self.face.expressInterest(interest, self.onCertificateReceived.bind(self), self.onCertificateTimeout.bind(self));
        }, function (error) {
            console.log(error);
        });
    }

    console.log("Key identity received is " + keyIdentity.toUri());
    this.identityManager.getDefaultKeyNameForIdentity(keyIdentity, sendRequest, function (error) {
        self.keyChain.createIdentityAndCertificate(keyIdentity, function (certName) {
            var defaultKey = IdentityCertificate.certificateNameToPublicKeyName(certName);
            sendRequest(defaultKey);
        }, function (error) {
            console.log(error);
        });
    });
}

IotNode.prototype.onCertificateTimeout = function(interest)
{
    // don't give up?
    var newInterest = new Interest(interest);
    newInterest.refreshNonce();
    this.face.expressInterest(newInterest, this.onCertificateReceived.bind(this), this.onCertificateTimeout.bind(this));
}

IotNode.prototype.processValidCertificate = function(data)
{
    // unpack the cert from the HMAC signed packet and verify
    try {
        var newCert = new IdentityCertificate();
        newCert.wireDecode(data.getContent());
        console.log("Received certificate from controller");

        /**
         * NOTE: we download and install the root certificate without verifying it (!)
         * otherwise our policy manager will reject it.
         * we may need a static method on KeyChain to allow verifying before adding
         */
        var rootCertName = newCert.getSignature().getKeyLocator().getKeyName();
        
        // update trust rules so we trust the controller
        //self._policyManager.setDeviceIdentity(self._configureIdentity) 
        //self._policyManager.updateTrustRules()

        var self = this;
        function onRootCertificateDownload(interest, data) {
            try {
                self.policyManager.certificateCache.insertCertificate(data);
                self.rootCertificate = data;
                var trustAnchorBase64 = data.wireEncode().buf().toString('base64');

                var defaultPolicy = 
                  "validator"                   + "\n" +
                  "{"                           + "\n" +
                  "  rule"                      + "\n" +
                  "  {"                         + "\n" +
                  "    id \"initial\""          + "\n" +
                  "    for data"                + "\n" +
                  "    checker"                 + "\n" +
                  "    {"                       + "\n" +
                  "      type customized"       + "\n" +
                  "      sig-type rsa-sha256"   + "\n" +
                  "      key-locator "          + "\n" +
                  "      {"                     + "\n" +
                  "         type name"          + "\n" +
                  "         name " + interest.getName().toUri()     + "\n" +
                  "         relation equal"     + "\n" +
                  "      }"                     + "\n" +
                  "    }"                       + "\n" +
                  "  }"                         + "\n" +
                  "  trust-anchor"              + "\n" +
                  "  {"                         + "\n" +
                  "    type base64"             + "\n" +
                  "    base64-string \"" + trustAnchorBase64 + "\"" + "\n" +
                  "  }"                         + "\n" +
                  "}";

                self.policyManager.load(defaultPolicy, "default-policy");
                var rootCert = new IdentityCertificate(data);

                self.identityManager.addCertificateAsDefault(rootCert, function () {
                    // we already inserted into certificate cache, so could pass this verification for the received self signed cert
                    console.log("Root cert added! Verifying new cert!");
                    self.keyChain.verifyData(newCert, self.finalizeCertificateDownload.bind(self), function (error) {
                        console.log("New certificate verification failed.");
                    });
                    var publicKeyName = rootCert.getPublicKeyName();
                    self.identityManager.setDefaultKeyForIdentity(publicKeyName, function () {
                        console.log("Default key for identity set");
                    }, function (error) {
                        console.log(error);
                    });
                }, function (error) {
                    console.log(error);
                    // assuming certificate already exist, set it as default then
                    // TODO: this handling of gateway cert already exists seems incomplete
                    self.identityManager.setDefaultCertificateForKey(rootCert);
                    var publicKeyName = rootCert.getPublicKeyName();
                    self.identityManager.setDefaultKeyForIdentity(publicKeyName, function () {
                        console.log("Default key for identity set");
                    }, function (error) {
                        console.log(error);
                    });
                });
            } catch (e) {
                console.log(e);
            }
        }
        function onRootCertificateTimeout(interest) {
            console.log("Root certificate times out");
            self.face.expressInterest(rootCertName, onRootCertificateDownload.bind(self), onRootCertificateTimeout.bind(self));
        }

        this.face.expressInterest(rootCertName, onRootCertificateDownload.bind(this), onRootCertificateTimeout.bind(this));
    } catch (e) {
        console.log("Could not import new certificate");
        console.log(e);
    }
}

IotNode.prototype.finalizeCertificateDownload = function(newCert)
{
    console.log("New cert verified!");
    var self = this;
    
    this.identityManager.addCertificate(new IdentityCertificate(newCert), function () {
        self.identityManager.setDefaultCertificateForKey(newCert, function() {
            console.log("Add device complete!");
        }, function (error) {
            console.log("Error setting default certificate for key!");
            console.log(error);
        });
    }, function (error) {
        console.log("Error adding new cert!");
        console.log(error);
    });
    
// For adding device only, the rest shouldn't matter
/*

    this.prefix = this.configureIdentity;
    this.face.setCommandCertificateName(this.getDefaultCertificateName());
    
    // Unported Python
    # unregister localhop prefix, register new prefix, change identity
    self.prefix = self._configureIdentity
    self._policyManager.setDeviceIdentity(self.prefix)

    self.face.setCommandCertificateName(self.getDefaultCertificateName())

    self.face.removeRegisteredPrefix(self.tempPrefixId)
    self.face.registerPrefix(self.prefix, self._onCommandReceived, self.onRegisterFailed)

    self.loop.call_later(5, self._updateCapabilities)
*/
}

IotNode.prototype.onCertificateReceived = function(interest, data)
{
    if (KeyChain.verifyDataWithHmacWithSha256(data, this.key)) {
        this.processValidCertificate(data);
    } else {
        console.log("Certificate from controller verification failed.");
    }
}

// Unported Python IotNode functions
/**
 * Device capabilities
 * On startup, tell the controller what types of commands are available
 */
/*
    def _onCapabilitiesAck(self, interest, data):
        self.log.debug('Received {}'.format(data.getName().toUri()))
        if not self._setupComplete:
            self._setupComplete = True
            self.log.info('Setup complete')
            self.loop.call_soon(self.setupComplete, self._configureIdentity)

    def _onCapabilitiesTimeout(self, interest):
        #try again in 30s
        self.log.info('Timeout waiting for capabilities update')
        self.loop.call_later(30, self._updateCapabilities)

    def _updateCapabilities(self):
        """
        Send the controller a list of our commands.
        """ 
        fullCommandName = Name(self._policyManager.getTrustRootIdentity()
                ).append('updateCapabilities')
        capabilitiesMessage = UpdateCapabilitiesCommandMessage()

        for command in self._commands:
            commandName = Name(self.prefix).append(Name(command.suffix))
            capability = capabilitiesMessage.capabilities.add()
            for i in range(commandName.size()):
                capability.commandPrefix.components.append(
                        str(commandName.get(i).getValue()))

            for kw in command.keywords:
                capability.keywords.append(kw)

            capability.needsSignature = command.isSigned

        encodedCapabilities = ProtobufTlv.encode(capabilitiesMessage)
        fullCommandName.append(encodedCapabilities)
        interest = Interest(fullCommandName)
        interest.setInterestLifetimeMilliseconds(5000)
        self.face.makeCommandInterest(interest)
        signature = self._policyManager._extractSignature(interest)

        self.log.info("Sending capabilities to controller")
        self.face.expressInterest(interest, self._onCapabilitiesAck, self._onCapabilitiesTimeout)

        # update twice a minute
        self.loop.call_later(30, self._updateCapabilities)
     
###
# Interest handling
# Verification of and responses to incoming (command) interests
##
    def verificationFailed(self, dataOrInterest):
        """
        Called when verification of a data packet or command interest fails.
        :param pyndn.Data or pyndn.Interest: The packet that could not be verified
        """
        print("Received invalid" + dataOrInterest.getName().toUri())
        self.log.info("Received invalid" + dataOrInterest.getName().toUri())

    def _makeVerifiedCommandDispatch(self, function):
        def onVerified(interest):
            self.log.info("Verified: " + interest.getName().toUri())
            responseData = function(interest)
            self.sendData(responseData)
        return onVerified

    def unknownCommandResponse(self, interest):
        """
        Called when the node receives an interest where the handler is unknown or unimplemented.
        :return: the Data packet to return in case of unhandled interests. Return None
            to ignore and let the interest timeout or be handled by another node.
        :rtype: pyndn.Data
        """
        responseData = Data(Name(interest.getName()).append("unknown"))
        responseData.setContent("Unknown command name")
        responseData.getMetaInfo().setFreshnessPeriod(1000) # expire soon

        return responseData

    def _onCommandReceived(self, prefix, interest, face, interestFilterId, filter):

        # first off, we shouldn't be here if we have no configured environment
        # just let this interest time out
        if (self._policyManager.getTrustRootIdentity() is None or
                self._policyManager.getEnvironmentPrefix() is None):
            return

        # if this is a cert request, we can serve it from our store (if it exists)
        certData = self._identityStorage.getCertificate(interest.getName())
        if certData is not None:
            self.log.info("Serving certificate request")
            # if we sign the certificate, we lose the controller's signature!
            self.sendData(certData, False)
            return

        # else we must look in our command list to see if this requires verification
        # we dispatch directly or after verification as necessary

        # now we look for the first command that matches in our config
        self.log.debug("Received {}".format(interest.getName().toUri()))
        
        for command in self._commands:
            fullCommandName = Name(self.prefix).append(Name(command.suffix))
            if fullCommandName.match(interest.getName()):
                dispatchFunc = command.function
                
                if not command.isSigned:
                    responseData = dispatchFunc(interest)
                    self.sendData(responseData)
                else:
                    try:
                        self._keyChain.verifyInterest(interest, 
                                self._makeVerifiedCommandDispatch(dispatchFunc),
                                self.verificationFailed)
                        return
                    except Exception as e:
                        self.log.exception("Exception while verifying command", exc_info=True)
                        self.verificationFailed(interest)
                        return
        #if we get here, just let it timeout
        return

#####
# Setup methods
####
    def addCommand(self, suffix, dispatchFunc, keywords=[], isSigned=True):
        """
        Install a command. When an interest is expressed for 
        /<node prefix>/<suffix>, dispatchFunc will be called with the interest
         name to get the reply data. 

        :param Name suffix: The command name. This will be appended to the node
            prefix.
        
        :param list keywords: A list of strings that can be used to look up this
            command in the controller's directory.
        
        :param function dispatchFunc: A function that is called when the 
            command is received. It must take an Interest argument and return a 
            Data object or None.

        :param boolean isSigned: Whether the command must be signed. If this is
            True and an unsigned command is received, it will be immediately
            rejected, and dispatchFunc will not be called.
        """
        if (suffix.size() == 0):
            raise RuntimError("Command suffix is empty")
        suffixUri = suffix.toUri()

        for command in self._commands:
            if (suffixUri == command.suffix):
                raise RuntimeError("Command is already registered")

        newCommand = Command(suffix=suffixUri, function=dispatchFunc, 
                keywords=tuple(keywords), isSigned=isSigned)

        self._commands.append(newCommand)

    def removeCommand(self, suffix):
        """
        Unregister a command. Does nothing if the command does not exist.

        :param Name suffix: The command name. 
        """
        suffixUri = suffix.ToUri()
        toRemove = None
        for command in self._commands:
            if (suffixUri == command.suffix):
                toRemove = command
                break
        if toRemove is not None:
            self._commands.remove(toRemove)


    def setupComplete(self, deviceIdentity):
        """
        Entry point for user-defined behavior. After this is called, the 
        certificates are in place and capabilities have been sent to the 
        controller. The node can now search for other devices, set up
        control logic, etc
        """
        pass
*/

// Helper function
function makeid(length)
{
    var text = "";
    var possible = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    for (var i = 0; i < length; i++)
        text += possible.charAt(Math.floor(Math.random() * possible.length));
    return text;
}
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

Bootstrap.prototype.onSchemaVerificationFailed = function (data, reason, onUpdateSuccess, onUpdateFailed)
{
    console.log("trust schema verification failed " + reason);
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
        }, function (data, reason) {
            self.onSchemaVerificationFailed(data, reason, onUpdateSuccess, onUpdateFailed); 
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
    return;
}

Bootstrap.prototype.getDefaultCertificateName = function()
{
    if (this.defaultCertificateName === undefined) {
        console.log("Default certificate is missing! Try setupDefaultIdentityAndRoot first?");
        return;
    } else {
        return this.defaultCertificateName;
    }
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
    
    var self = this;
    this.face.nodeMakeCommandInterest(requestInterest, this.keyChain, this.defaultCertificateName, TlvWireFormat.get(), function () {
        self.face.expressInterest(requestInterest, function (interest, data) {
            self.onAppRequestData(interest, data, onRequestSuccess, onRequestFailed); 
        }, function (interest) {
            self.onAppRequestTimeout(interest, onRequestSuccess, onRequestFailed);
        });
        console.log("Application publish request sent: " + requestInterest.getName().toUri())
    });
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
        
    function onVerifyFailed(data, reason) {
        var msg = "Application request response verification failed: " + reason;
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
};var AppConsumer = function AppConsumer(face, keyChain, certificateName, doVerify)
{
    this.face = face;
    this.keyChain = keyChain;
    this.certificateName = certificateName;
    this.doVerify = doVerify;
};var AppConsumerTimestamp = function AppConsumerTimestamp
  (face, keyChain, certificateName, doVerify, currentTimestamp)
{
    AppConsumer.call(this, face, keyChain, certificateName, doVerify);

    this.currentTimestamp = currentTimestamp;

    this.verifyFailedRetransInterval = 4000;
    this.defaultInterestLifetime = 4000;
}

// public interface
AppConsumerTimestamp.prototype.consume = function
  (prefix, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;

    var name = (new Name(prefix)).append(this.currentSeqNumber.toString());
    var interest = new Interest(name);
    interest.setInterestLifetimeMilliseconds(this.defaultInterestLifetime);

    if (this.currentTimestamp) {
        var exclude = new Exclude();
        exclude.appendAny();
        exclude.appendComponent(Name.Component.fromVersion(this.currentTimestamp));
        interest.setExclude();
    }

    this.face.expressInterest(interest, function (i, d) {
        self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
    }, function (i) {
        self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
    });
    return;
}

// internal functions
AppConsumerTimestamp.prototype.onData = function
  (interest, data, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    if (this.doVerify) {
        this.keyChain.verifyData(data, function (d) {
            self.beforeReplyDataVerified(d, onVerified, onVerifyFailed, onTimeout);
        }, function (d) {
            self.beforeReplyVerificationFailed(d, interest, onVerified, onVerifyFailed, onTimeout);
        });
    } else {
        this.beforeReplyDataVerified(data, onVerified, onVerifyFailed, onTimeout);
    }
    return;
}

AppConsumerTimestamp.prototype.beforeReplyDataVerified = function
  (data, onVerified, onVerifyFailed, onTimeout)
{
    this.currentTimestamp = data.getName().get(-1).toVersion();
    this.consume(data.getName().getPrefix(-1), onVerified, onVerifyFailed, onTimeout);
    this.onVerified(data);
    return;
}

AppConsumerTimestamp.prototype.beforeReplyVerificationFailed = function
  (data, interest, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    // for now internal to the library: verification failed cause the library to retransmit the interest after some time
    var newInterest = new Interest(interest);
    newInterest.refreshNonce();

    var dummyInterest = new Interest(Name("/local/timeout"));
    dummyInterest.setInterestLifetimeMilliseconds(this._verifyFailedRetransInterval)
    this.face.expressInterest(dummyInterest, this.onDummyData, function (i) {
        self.retransmitInterest(newInterest, onVerified, onVerifyFailed, onTimeout);
    });
    this.onVerifyFailed(data);
    return;
}

AppConsumerTimestamp.prototype.beforeReplyTimeout = function 
  (interest, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    var newInterest = new Interest(interest);
    newInterest.refreshNonce();
    
    this.face.expressInterest(newInterest, function (i, d) {
        self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
    }, function (i) {
        self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
    });
    this.onTimeout(interest);
    return;
}

AppConsumerTimestamp.prototype.retransmitInterest = function
  (interest, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    this.face.expressInterest(interest, function (i, d) {
        self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
    }, function (i) {
        self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
    });
}
        
AppConsumerTimestamp.prototype.onDummyData = function
  (interest, data)
{
    console.log("Got unexpected dummy data");
    return;
}
        
var AppConsumerSequenceNumber = function AppConsumerSequenceNumber
  (face, keyChain, certificateName, doVerify, defaultPipelineSize, startingSeqNumber)
{
    if (defaultPipelineSize === undefined) {
        defaultPipelineSize = 5;
    }
    if (startingSeqNumber === undefined) {
        startingSeqNumber = 0;
    }
    AppConsumer.call(this, face, keyChain, certificateName, doVerify);

    this.pipelineSize = defaultPipelineSize;
    this.emptySlot = defaultPipelineSize;
    this.currentSeqNumber = startingSeqNumber;

    this.verifyFailedRetransInterval = 4000;
    this.defaultInterestLifetime = 4000;
}

// public interface
AppConsumerSequenceNumber.prototype.consume = function
  (prefix, onVerified, onVerifyFailed, onTimeout)
{
    var num = this.emptySlot;
    var self = this;
    for (var i = 0; i < emptySlot; i++) {
        var name = (new Name(prefix)).append(this.currentSeqNumber.toString());
        var interest = new Interest(name);
        interest.setInterestLifetimeMilliseconds(this.defaultInterestLifetime);
        this.face.expressInterest(interest, function (i, d) {
            self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
        }, function (i) {
            self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
        });
        this.currentSeqNumber -= 1;
        this.emptySlot += 1;
    }
    return;
}

// internal functions
AppConsumerSequenceNumber.prototype.onData = function
  (interest, data, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    if (this.doVerify) {
        this.keyChain.verifyData(data, function (d) {
            self.beforeReplyDataVerified(d, onVerified, onVerifyFailed, onTimeout);
        }, function (d) {
            self.beforeReplyVerificationFailed(d, interest, onVerified, onVerifyFailed, onTimeout);
        });
    } else {
        this.beforeReplyDataVerified(data, onVerified, onVerifyFailed, onTimeout);
    }
    return;
}

AppConsumerSequenceNumber.prototype.beforeReplyDataVerified = function
  (data, onVerified, onVerifyFailed, onTimeout)
{
    // fill the pipeline
    this.currentSeqNumber += 1;
    this.emptySlot += 1;
    this.consume(data.getName().getPrefix(-1), onVerified, onVerifyFailed, onTimeout);
    this.onVerified(data);
    return;
}

AppConsumerSequenceNumber.prototype.beforeReplyVerificationFailed = function
  (data, interest, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    // for now internal to the library: verification failed cause the library to retransmit the interest after some time
    var newInterest = new Interest(interest);
    newInterest.refreshNonce();

    var dummyInterest = new Interest(Name("/local/timeout"));
    dummyInterest.setInterestLifetimeMilliseconds(this._verifyFailedRetransInterval)
    this.face.expressInterest(dummyInterest, this.onDummyData, function (i) {
        self.retransmitInterest(newInterest, onVerified, onVerifyFailed, onTimeout);
    });
    this.onVerifyFailed(data);
    return;
}

AppConsumerSequenceNumber.prototype.beforeReplyTimeout = function 
  (interest, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    var newInterest = new Interest(interest);
    newInterest.refreshNonce();
    
    this.face.expressInterest(newInterest, function (i, d) {
        self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
    }, function (i) {
        self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
    });
    this.onTimeout(interest);
    return;
}

AppConsumerSequenceNumber.prototype.retransmitInterest = function
  (interest, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    this.face.expressInterest(interest, function (i, d) {
        self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
    }, function (i) {
        self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
    });
}
        
AppConsumerSequenceNumber.prototype.onDummyData = function
  (interest, data)
{
    console.log("Got unexpected dummy data");
    return;
}
        
// Generic sync-based discovery implementation
var SyncBasedDiscovery = function SyncBasedDiscovery
  (face, keyChain, certificateName, syncPrefix, observer, serializer, 
   syncDataFreshnessPeriod, initialDigest, syncInterestLifetime, syncInterestMinInterval,
   timeoutCntThreshold, maxResponseWaitPeriod, minResponseWaitPeriod, entityDataFreshnessPeriod)
{
    if (syncDataFreshnessPeriod === undefined) {
        syncDataFreshnessPeriod = 4000;
    }
    if (initialDigest === undefined) {
        initialDigest = "00";
    }
    if (syncInterestLifetime === undefined) {
        syncInterestLifetime = 4000;
    }
    if (syncInterestMinInterval === undefined) {
        syncInterestMinInterval = 500;
    }
    if (timeoutCntThreshold === undefined) {
        timeoutCntThreshold = 3;
    }
    if (maxResponseWaitPeriod === undefined) {
        maxResponseWaitPeriod = 2000;
    }
    if (minResponseWaitPeriod === undefined) {
        minResponseWaitPeriod = 400;
    }
    if (entityDataFreshnessPeriod === undefined) {
        entityDataFreshnessPeriod = 10000;
    }

    this.face = face;
    this.keyChain = keyChain;
    this.syncPrefix = syncPrefix;
    
    this.objects = {};
    this.hostedObjects = {};

    this.memoryContentCache = new MemoryContentCache(this.face);
    this.certificateName = new Name(certificateName);

    this.currentDigest = initialDigest;
    this.syncDataFreshnessPeriod = syncDataFreshnessPeriod;
    this.initialDigest = initialDigest;
    this.syncInterestLifetime = syncInterestLifetime;

    this.syncInterestMinInterval = syncInterestMinInterval;
    this.timeoutCntThreshold = timeoutCntThreshold;
    this.entityDataFreshnessPeriod = entityDataFreshnessPeriod;

    this.observer = observer;
    this.serializer = serializer;

    this.numOutstandingInterest = 0;

    return;
}

//Public facing interface
SyncBasedDiscovery.prototype.start = function ()
{
    this.updateDigest();
    var interest = new Interest((new Name(this.syncPrefix)).append(this.currentDigest));

    interest.setMustBeFresh(true);
    interest.setInterestLifetimeMilliseconds(this.syncInterestLifetime);
    this.face.expressInterest(interest, this.onSyncData.bind(this), this.onSyncTimeout.bind(this));
    this.numOutstandingInterest += 1;

    console.log("Express interest: " + interest.getName().toUri());
    return;
}

SyncBasedDiscovery.prototype.stop = function ()
{
    this.memoryContentCache.unregisterAll();
    return;
}

SyncBasedDiscovery.prototype.getHostedObjects = function ()
{
    return this.hostedObjects;
}

SyncBasedDiscovery.prototype.getObjects = function ()
{
    return this.objects;
}

SyncBasedDiscovery.prototype.publishObject = function
  (name, entityInfo)
{
    // If this is the first object we host, we register for sync namespace: meaning a participant not hosting anything 
    //   is only "listening" for sync, and will not help in the sync process
    if (Object.keys(this.hostedObjects).length == 0) {
        this.memoryContentCache.registerPrefix(this.syncPrefix, this.onRegisterFailed.bind(this), this.onSyncInterest.bind(this));
    }
    if (this.addObject(name)) {
        this.hostedObjects[name] = entityInfo;
        // TODO: debug this, seems to not working as intended yet
        this.contentCacheAddEntityData(name, entityInfo);
        this.memoryContentCache.registerPrefix(new Name(name), this.onRegisterFailed.bind(this), this.onEntityDataNotFound.bind(this));
    } else {
        console.log("Item with this name already added");
    }

    return;
}

SyncBasedDiscovery.prototype.removeHostedObject = function (name)
{
    if (name in this.hostedObjects) {
        delete this.hostedObjects[name];
        if (Object.keys(this.hostedObjects).length == 0) {
            self._memoryContentCache.unregisterAll();
        }
        if (this.removeObject(name)) {
            return true;
        } else {
            console.log("Hosted item not in objects list");
            return false;
        }
    } else {
        return false;
    }
}

//Internal functions
SyncBasedDiscovery.prototype.contentCacheAddEntityData = function
  (name, entityInfo)
{
    var content = this.serializer.serialize(entityInfo);
    var data = new Data(new Name(name));

    data.setContent(content);
    // Interest issuer should not ask for mustBeFresh in this case, for now
    data.getMetaInfo().setFreshnessPeriod(this.entityDataFreshnessPeriod);

    var self = this;
    this.keyChain.sign(data, this.certificateName, function() {
        self.memoryContentCache.add(data);
    });
}

SyncBasedDiscovery.prototype.contentCacheAddSyncData = function
  (dataName)
{
    var keys = Object.keys(this.objects);
    keys.sort();

    var content = "";
    for (var item in keys) {
        content += keys[item] + "\n";
    }
    content.trim();

    var data = new Data(new Name(dataName));
        
    data.setContent(content);
    data.getMetaInfo().setFreshnessPeriod(this.syncDataFreshnessPeriod);
    
    var self = this;
    this.keyChain.sign(data, this.certificateNamem, function() {
        // adding this data to memoryContentCache should satisfy the pending interest
        self.memoryContentCache.add(data);
    });
}
        

SyncBasedDiscovery.prototype.onSyncInterest = function
  (prefix, interest, face, interestFilterId, filter)
{
    if (interest.getName().size() !== this.syncPrefix.size() + 1) {
        // Not an interest for us
        return;
    }
            
    var digest = interest.getName().get(-1).toEscapedString();
    this.updateDigest();
    if (digest != this.currentDigest) {
        // Wait a random period before replying; rationale being that "we are always doing ChronoSync recovery...this is the recovery timer but randomized"
        // Consider this statement: we are always doing ChronoSync recovery
        // TODO: this has the problem of potentially answering with wrong data, there will be more interest exchanges needed for the lifetime duration of one wrong answer
        // Consider appending "answerer" as the last component of data name?
        // TODO2: don't see why we should wait here

        this.replySyncInterest(interest, digest);
        //dummyInterest = Interest(Name("/local/timeout1"))
        //dummyInterest.setInterestLifetimeMilliseconds(random.randint(self._minResponseWaitPeriod, self._maxResponseWaitPeriod))
        //self._face.expressInterest(dummyInterest, self.onDummyData, lambda a : self.replySyncInterest(a, digest))
    }

    return;
}
        

SyncBasedDiscovery.prototype.replySyncInterest = function
  (interest, receivedDigest)
{
    this.updateDigest();
    if (receivedDigest != self._currentDigest) {
        // TODO: one participant may be answering with wrong info: scenario: 1 has {a}, 2 has {b}
        // 2 gets 1's {a} and asks again before 1 gets 2's {b}, 2 asks 1 with the digest of {a, b}, 1 will 
        // create a data with the content {a} for the digest of {a, b}, and this data will be able to answer
        // later steady state interests from 2 until it expires (and by which time 1 should be updated with
        // {a, b} as well)
        this.contentCacheAddSyncData(new Name(this.syncPrefix).append(receivedDigest));
    }
    return;
}

SyncBasedDiscovery.prototype.onSyncData = function
  (interest, data)
{
    this.numOutstandingInterest -= 1;
    //TODO: do verification first
    console.log("Got sync data; name: " + data.getName().toUri() + "; content: " + data.getContent().buf());
    var content = String(data.getContent().buf()).split('\n');

    for (var itemName in content) {
        if (!(content[itemName] in this.objects)) {
            if (content[itemName] != "") {
                this.onReceivedSyncData(content[itemName]);
            }
        }
    }
    
    // Hack for re-expressing sync interest after a short interval
    var dummyInterest = new Interest(new Name("/local/timeout"));
    dummyInterest.setInterestLifetimeMilliseconds(this.syncInterestLifetime);
    this.face.expressInterest(dummyInterest, this.onDummyData.bind(this), this.expressSyncInterest.bind(this));
    return;
}

SyncBasedDiscovery.prototype.onSyncTimeout = function
  (interest)
{
    this.numOutstandingInterest -= 1;
    console.log("Sync interest times out: " + interest.getName().toUri());
    if (this.numOutstandingInterest <= 0) {
        this.expressSyncInterest();
    }
    return;
}

// Handling received sync data: express entity interest
SyncBasedDiscovery.prototype.onReceivedSyncData = function 
  (itemName)
{
    console.log("Received itemName: " + itemName);
    var interest = new Interest(new Name(itemName));
    interest.setInterestLifetimeMilliseconds(4000);
    interest.setMustBeFresh(false);
    this.face.expressInterest(interest, this.onEntityData.bind(this), this.onEntityTimeout.bind(this));
    
    return;
}

SyncBasedDiscovery.prototype.onEntityTimeout = function 
  (interest)
{
    console.log("Item interest times out: " + interest.getName().toUri());
    return;
}

SyncBasedDiscovery.prototype.onEntityData = function 
  (interest, data)
{
    var self = this;
    console.log("Got data: " + data.getName().toUri());
    this.addObject(interest.getName().toUri());
    console.log("Added device: " + interest.getName().toUri());

    var dummyInterest = new Interest(new Name("/local/timeout"));
    dummyInterest.setInterestLifetimeMilliseconds(4000);
    this.face.expressInterest(dummyInterest, this.onDummyData.bind(this), function (a) {
        self.expressHeartbeatInterest(a, interest)
    });
    return;
}

SyncBasedDiscovery.prototype.expressHeartbeatInterest = function
  (dummyInterest, entityInterest)
{
    var newInterest = new Interest(entityInterest);
    newInterest.refreshNonce();

    this.face.expressInterest(entityInterest, this.onHeartbeatData.bind(this), this.onHeartbeatTimeout.bind(this)); 
}

SyncBasedDiscovery.prototype.onHeartbeatData = function
  (interest, data)
{
    var self = this;
    this.resetTimeoutCnt(interest.getName().toUri());
    var dummyInterest = new Interest(new Name("/local/timeout"));
    dummyInterest.setInterestLifetimeMilliseconds(4000);
    this.face.expressInterest(dummyInterest, this.onDummyData.bind(this), function (a) {
        self.expressHeartbeatInterest(a, interest);
    });
}

SyncBasedDiscovery.prototype.onHeartbeatTimeout = function 
  (interest)
{
    if (this.incrementTimeoutCnt(interest.getName().toUri())) {
        console.log("Remove: " + interest.getName().toUri() + " because of consecutive timeout cnt exceeded");
    } else {
        var newInterest = new Interest(interest.getName());
        console.log("Express interest: " + newInterest.getName().toUri());
        newInterest.setInterestLifetimeMilliseconds(4000);
        this.face.expressInterest(newInterest, this.onHeartbeatData.bind(this), this.onHeartbeatTimeout.bind(this));
    }
}

SyncBasedDiscovery.prototype.onDummyData = function
  (interest, data)
{
    console.log("Unexpected reply to dummy interest: " + data.getContent().buf());
    return;
}

SyncBasedDiscovery.prototype.expressSyncInterest = function
  (interest)
{
    var newInterest = new Interest(new Name(this.syncPrefix).append(this.currentDigest));
    newInterest.setInterestLifetimeMilliseconds(this.syncInterestLifetime);
    newInterest.setMustBeFresh(true);
    this.face.expressInterest(newInterest, this.onSyncData.bind(this), this.onSyncTimeout.bind(this));
    this.numOutstandingInterest += 1;
    console.log("Dummy timeout; Express interest: " + newInterest.getName().toUri());
    return;
}

SyncBasedDiscovery.prototype.addObject = function 
  (name)
{
    if (name in this.objects) {
        return false;
    } else {
        this.objects[name] = {"timeout_count": 0};
        this.notifyObserver(name, "ADD", "");
        this.contentCacheAddSyncData(new Name(this.syncPrefix).append(this.currentDigest));
        this.updateDigest();
        return true;
    }
}

SyncBasedDiscovery.prototype.removeObject = function
  (name)
{
    if (name in this.objects) {
        delete self._objects[name]
        
        this.notifyObserver(name, "REMOVE", "");
        this.contentCacheAddSyncData(new Name(this.syncPrefix).append(this.currentDigest));
        this.updateDigest();
        return true;
    } else {
        return false;
    }
}

SyncBasedDiscovery.prototype.updateDigest = function ()
{
    // TODO: for now, may change the format of the list encoding for easier cross language compatibility
    var keys = Object.keys(this.objects);
    keys.sort();

    if (keys.length > 0) {
        var m = Crypto.createHash('sha256');
        for (var i = 0; i < keys.length; i++) {
            m.md.updateString(keys[i]);
        }
        this.currentDigest = m.digest('hex');
    } else {
        this.currentDigest = this.initialDigest;
    }
    return;
}
        
SyncBasedDiscovery.prototype.incrementTimeoutCnt = function 
  (name)
{
    if (name in this.objects) {
        this.objects[name]["timeout_count"] += 1;
        if (this.objects[name]["timeout_count"] >= this.timeoutCntThreshold) {
            return this.removeObject(name);
        } else {
            return false;
        }
    } else {
        return false;
    }
}  

SyncBasedDiscovery.prototype.resetTimeoutCnt = function
  (name)
{
    if (name in this.objects) {
        this.objects[name]["timeout_count"] = 0;
        return true;
    } else {
        return false;
    }
}

SyncBasedDiscovery.prototype.notifyObserver = function 
  (name, msgType, msg)
{
    this.observer.onStateChanged(name, msgType, msg);
    return;
}

SyncBasedDiscovery.prototype.onRegisterFailed = function
  (prefix)
{
    console.log("Prefix registration failed: " + prefix.toUri());
    return;
}

SyncBasedDiscovery.prototype.onEntityDataNotFound = function 
  (prefix, interest, face, interestFilterId, filter)
{
    var name = interest.getName().toUri();
    if (name in this.hostedObjects) {
        var content = this.serializer.serialize(this.hostedObjects[name]);
        var data = new Data(new Name(name));

        data.setContent(content);
        // Interest issuer should not ask for mustBeFresh in this case, for now
        data.getMetaInfo().setFreshnessPeriod(this.entityDataFreshnessPeriod);

        var self = this;
        this.keyChain.sign(data, this.certificateName, function() {
            self.memoryContentCache.add(data);
        });
    }
    
    return;
}
