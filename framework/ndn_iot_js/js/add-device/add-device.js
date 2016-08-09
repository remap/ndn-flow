default_prefix = new Name("/home/configure");

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
    setTimeout(this.beforeLoopStart.bind(this), 200);
};

IotNode.prototype.getSerial = function()
{
    if (this.serial === undefined) {
        console.log("work");
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
            console.log(defaultKey.get(i).getValue().buf());
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
                console.log(defaultPolicy);

                self.identityManager.addCertificate(new IdentityCertificate(data), function () {
                    // we already inserted into certificate cache, so could pass this verification for the received self signed cert
                    console.log("Verifying new cert!");
                    self.keyChain.verifyData(newCert, self.finalizeCertificateDownload.bind(self), function (error) {
                        console.log("New certificate verification failed.");
                    });
                }, function (error) {
                    console.log(error);
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
