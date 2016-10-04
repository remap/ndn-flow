namespace ndn_iot.bootstrap {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    //using Google.Protobuf;

    // For callbacks
    using System.Runtime.InteropServices;

    using net.named_data.jndn.security.policy;    
    using net.named_data.jndn;
    using net.named_data.jndn.encoding;
    using net.named_data.jndn.util;
    using net.named_data.jndn.security;
    using net.named_data.jndn.security.identity;
    using net.named_data.jndn.security.certificate;
    using net.named_data.jndn.encoding.tlv;

    // TODO: to be removed after inline testing's done
    using net.named_data.jndn.transport;

    // TODO: abandoned protobuf-C# for now, not wise to investigate given we don't have enough time, hack controller instead
    //using ndn_iot.bootstrap.command;

    public delegate void OnRequestSuccess();
    public delegate void OnRequestFailed(string msg);
    
    public delegate void OnUpdateSuccess(string schema, bool isInitial);
    public delegate void OnUpdateFailed(string msg);

    public class Bootstrap : OnRegisterFailed {        

        public Bootstrap(Face face) {
            applicationName_ = "";

            string homePath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                                ? Environment.GetEnvironmentVariable("HOME")
                                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            keyPath_ = System.IO.Path.Combine(homePath, ".ndn/ndnsec-tpm-file/");
            filePrivateKeyStorage_ = new FilePrivateKeyStorage(keyPath_);
            memoryIdentityStorage_ = new MemoryIdentityStorage();

            identityManager_ = new IdentityManager(memoryIdentityStorage_, filePrivateKeyStorage_);
            
            policyManager_ = new ConfigPolicyManager();
            policyManager_.load(@"
              validator            
              {                    
                rule               
                {                  
                  id ""initial rule""
                  for data           
                  checker            
                  {                  
                    type hierarchical
                  }                
                }                  
              }", "initial-rule");

            keyChain_ = new KeyChain(identityManager_, policyManager_);
            
            face_ = face;
            keyChain_.setFace(face_);

            certificateContentCache_ = new MemoryContentCache(face_);
        }

        public KeyChain setupDefaultIdentityAndRoot(Name defaultIdentityName, Name signerName) {
            return setupDefaultIdentityAndRoot(defaultIdentityName, defaultCertFileName_, signerName);
        }

        public KeyChain setupDefaultIdentityAndRoot(Name defaultIdentityName, string certFilePath, Name signerName) {
            if (defaultIdentityName.size() == 0) {
                // Default identity does not exist
                throw new SystemException("Using system default identity is not supported!\n");
            }

            Data certData = new Data();
            try {
                defaultIdentity_ = new Name(defaultIdentityName);
                // Hack for getting the key names
                defaultKeyName_ = getDefaultKeyNameForIdentity(defaultIdentity_);
                if (defaultKeyName_.size() == 0) {
                    throw new SystemException("Cannot find a key name for identity: " + defaultIdentity_.toUri() + "\n");
                }
                string certBase64 = System.IO.File.ReadAllText(certFilePath);
                byte[] certBytes = Convert.FromBase64String(certBase64);
                
                certData.wireDecode(new Blob(certBytes, true));

                if (IdentityCertificate.certificateNameToPublicKeyName(certData.getName()).equals(defaultKeyName_)) {
                    defaultCertificateName_ = certData.getName();
                } else {
                    Console.Out.WriteLine(IdentityCertificate.certificateNameToPublicKeyName(certData.getName()));
                    Console.Out.WriteLine(defaultKeyName_);
                    throw new SystemException("Given certificate file does not match with the default key for the configured identity!");
                }

                PublicKey publicKey = filePrivateKeyStorage_.getPublicKey(defaultKeyName_);
                memoryIdentityStorage_.addKey(defaultKeyName_, publicKey.getKeyType(), new Blob(publicKey.getKeyDer()));
            } catch (SecurityException ex) {
                Console.Out.WriteLine(ex.Message);
                throw new SystemException("Security exception: " + ex.Message + " (default identity: " + defaultIdentity_.toUri() + ")");
            }

            Name actualSignerName = KeyLocator.getFromSignature(certData.getSignature()).getKeyName();
            Console.Out.WriteLine("Cert name is " + certData.getName().toUri());
            Console.Out.WriteLine("Cert is signed by " + actualSignerName.toUri());
            if (signerName.size() > 0 && !(actualSignerName.equals(signerName))) {
                throw new SystemException("Security exception: expected signer name does not match with actual signer name: " + signerName.toUri() + " " + actualSignerName.toUri());
            }
            controllerName_ = getIdentityNameFromCertName(actualSignerName);

            face_.setCommandSigningInfo(keyChain_, defaultCertificateName_);
            certificateContentCache_.registerPrefix(new Name(defaultCertificateName_).getPrefix(-1), this);
            certificateContentCache_.add(certData);
            return keyChain_;
        }

        /**
         * Publishing authorization
         */
        public void requestProducerAuthorization(Name dataPrefix, string applicationName, OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed) {
            if (defaultCertificateName_.size() == 0) {
                return;
            }
            sendAppRequest(defaultCertificateName_, dataPrefix, applicationName, onRequestSuccess, onRequestFailed);
        }

        public void sendAppRequest(Name certificateName, Name dataPrefix, string applicationName, OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed) {
            Blob encoding = AppRequestEncoder.encodeAppRequest(certificateName, dataPrefix, applicationName);
            //Console.Out.WriteLine("Encoding: " + encoding.toHex());
            Name requestInterestName = new Name(controllerName_);
            requestInterestName.append("requests").append(new Name.Component(encoding));

            Interest requestInterest = new Interest(requestInterestName);
            requestInterest.setInterestLifetimeMilliseconds(4000);
            // don't use keyChain.sign(interest), since it's of a different format from commandInterest
            face_.makeCommandInterest(requestInterest);

            AppRequestHandler appRequestHandler = new AppRequestHandler(keyChain_, onRequestSuccess, onRequestFailed);
            face_.expressInterest(requestInterest, appRequestHandler, appRequestHandler);
            
            Console.Out.WriteLine("Sent interest: " + requestInterest.getName().toUri());

            return ;
        }

        public class AppRequestHandler : OnData, OnTimeout {
            public AppRequestHandler(KeyChain keyChain, OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed) {
                onRequestSuccess_ = onRequestSuccess;
                onRequestFailed_ = onRequestFailed;
                keyChain_ = keyChain;
            }

            public void onData(Interest interest, Data data) {
                Console.Out.WriteLine("Got data: " + data.getName().toUri());
                AppRequestVerifyHandler verifyHandler = new AppRequestVerifyHandler(onRequestSuccess_, onRequestFailed_);
                keyChain_.verifyData(data, verifyHandler, verifyHandler);
            }

            public void onTimeout(Interest interest) {
                Console.Out.WriteLine("Interest times out: " + interest.getName().toUri());
            }

            OnRequestSuccess onRequestSuccess_;
            OnRequestFailed onRequestFailed_;
            KeyChain keyChain_;
        }

        public class AppRequestVerifyHandler: OnVerified, OnVerifyFailed {
            public AppRequestVerifyHandler(OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed) {
                onRequestSuccess_ = onRequestSuccess;
                onRequestFailed_ = onRequestFailed;
            }

            public void onVerified(Data data) {
                // TODO: JSON parsing of controller reply
            }

            public void onVerifyFailed(Data data) {

            }

            OnRequestSuccess onRequestSuccess_;
            OnRequestFailed onRequestFailed_;
        }

        /**
         * Handling application consumption (trust schema update)
         */
        public void startTrustSchemaUpdate(Name appPrefix, OnUpdateSuccess onUpdateSuccess, OnUpdateFailed onUpdateFailed) {
            string appNamespace = appPrefix.toUri();
            if (trustSchemas_.ContainsKey(appNamespace)) {
                if (trustSchemas_[appNamespace].getFollowing()) {
                    return;
                }
                trustSchemas_[appNamespace].setFollowing(true);
            } else {
                trustSchemas_[appNamespace] = new AppTrustSchema(true, "", 0, true);
            }

            Interest initialInterest = new Interest(new Name(appNamespace).append("_schema"));
            initialInterest.setChildSelector(1);

            TrustSchemaUpdateHandler trustSchemaUpdateHandler = new TrustSchemaUpdateHandler(keyChain_, onUpdateSuccess, onUpdateFailed);
            face_.expressInterest(initialInterest, trustSchemaUpdateHandler, trustSchemaUpdateHandler);
        }

        class TrustSchemaUpdateHandler : OnData, OnTimeout {
            public TrustSchemaUpdateHandler(KeyChain keyChain, OnUpdateSuccess onUpdateSuccess, OnUpdateFailed onUpdateFailed) {
                onUpdateSuccess_ = onUpdateSuccess;
                onUpdateFailed_ = onUpdateFailed;
                keyChain_ = keyChain;
            }

            public void onData(Interest interest, Data data) {
                Console.Out.WriteLine("Received trust schema update data");
                TrustSchemaVerifyHandler trustSchemaVerifyHandler = new TrustSchemaVerifyHandler(onUpdateSuccess_, onUpdateFailed_);
                keyChain_.verifyData(data, trustSchemaVerifyHandler, trustSchemaVerifyHandler);
            }

            public void onTimeout(Interest interest) {
                Console.Out.WriteLine("Trust schema update times out");
            }

            KeyChain keyChain_;
            OnUpdateSuccess onUpdateSuccess_;
            OnUpdateFailed onUpdateFailed_;
        }

        class TrustSchemaVerifyHandler : OnVerified, OnVerifyFailed {
            public TrustSchemaVerifyHandler(OnUpdateSuccess onUpdateSuccess, OnUpdateFailed onUpdateFailed) {
                onUpdateSuccess_ = onUpdateSuccess;
                onUpdateFailed_ = onUpdateFailed;
            }

            public void onVerified(Data data) {
                //onUpdateSuccess_();
            }

            public void onVerifyFailed(Data data) {
                onUpdateFailed_("Trust schema verification failed.");
            }

            OnUpdateSuccess onUpdateSuccess_;
            OnUpdateFailed onUpdateFailed_;
        }

        class AppTrustSchema {
            public AppTrustSchema(bool following, string schema, long version, bool isInitial) {
                following_ = following;
                schema_ = schema;
                version_ = version;
                isInitial_ = isInitial;
            }

            public bool getFollowing() {
                return following_;
            }

            public void setFollowing(bool following) {
                following_ = following;
            }

            public string getSchema() {
                return schema_;
            }

            public void setSchema(string schema) {
                schema_ = schema;
            }

            public long getVersion() {
                return version_;
            }

            public void setVersion(long version) {
                version_ = version;
            }

            public bool getIsInitial() {
                return isInitial_;
            }

            public void setIsInitial(bool isInitial) {
                isInitial_ = isInitial;
            }

            bool following_; 
            string schema_; 
            long version_; 
            bool isInitial_;
        }


        /**
         * Helper functions
         */
        // Hack: find the first key name according to the identity name, and think of that as default
        private Name getDefaultKeyNameForIdentity(Name defaultIdentity) {
            string[] lines = System.IO.File.ReadAllLines(keyPath_ + "mapping.txt");
            foreach (string line in lines) {
                // Use a tab to indent each line of the file.
                string[] components = line.Split(' ');
                string defaultIdentityString = defaultIdentity.toUri();
                if (components[0].Contains(defaultIdentityString)) {
                    return new Name(components[0]);
                }
            }
            return new Name();
        }

        // debug function: same extracted from ndn-dot-net library
        private string nameTransform(string keyName, string extension) {
            byte[] hash;
            try {
                hash = net.named_data.jndn.util.Common.digestSha256(ILOG.J2CsMapping.Util.StringUtil.GetBytes(keyName,"UTF-8"));
            } catch (IOException ex) {
                // We don't expect this to happen.
                throw new Exception("UTF-8 encoder not supported: " + ex.Message);
            }
            string digest = net.named_data.jndn.util.Common.base64Encode(hash);
            digest = digest.replace('/', '%');

            return digest + extension;
        }

        // get identity name from certificate name
        private Name getIdentityNameFromCertName(Name certName)
        {
            int i = certName.size() - 1;

            string idString = "KEY";
            while (i >= 0) {
                if (certName.get(i).toEscapedString() == idString)
                    break;
                i -= 1;
            }
              
            if (i < 0) {
                return new Name();
            }

            return certName.getPrefix(i);
        }

        // generate identity and certificate
        public void createIdentityAndCertificate(Name identityName) {
            Console.Out.WriteLine("Creating identity and certificate");
            Name certificateName = identityManager_.createIdentityAndCertificate(identityName, new RsaKeyParams());
            IdentityCertificate certificate = memoryIdentityStorage_.getCertificate(certificateName);
            Console.Out.WriteLine("Certificate name: " + certificateName.toUri());
            string certString = Convert.ToBase64String(certificate.wireEncode().getImmutableArray());
            Console.Out.WriteLine(certString);
        }

        public void onRegisterFailed(Name prefix) {
            Console.Out.WriteLine("Registration failed for prefix: " + prefix.toUri());
        }

        public Name getDefaultCertificateName() {
            return defaultCertificateName_;
        }


        static string defaultCertFileName_ = "my.cert";

        Name defaultIdentity_;
        Name defaultKeyName_;
        Name defaultCertificateName_;

        Name controllerName_;
        IdentityCertificate controllerCertificate_;

        string applicationName_;
        IdentityManager identityManager_;
        ConfigPolicyManager policyManager_;

        FilePrivateKeyStorage filePrivateKeyStorage_;
        IdentityStorage memoryIdentityStorage_;
        string keyPath_;

        KeyChain keyChain_;
        Face face_;
        MemoryContentCache certificateContentCache_;
        Dictionary<string, AppTrustSchema> trustSchemas_;
    }

    // TestEncodeAppRequest: credit to Jeff T
    class AppRequestEncoder {
        /// <summary>
        /// Encode the name as NDN-TLV to the encoder, using the given TLV type.
        /// </summary>
        ///
        /// <param name="name">The name to encode.</param>
        /// <param name="type">The TLV type</param>
        /// <param name="encoder">The TlvEncoder to receive the encoding.</param>
        private static void encodeName(Name name, int type, TlvEncoder encoder) 
        {
            int saveLength = encoder.getLength();

            // Encode the components backwards.
            for (int i = name.size() - 1; i >= 0; --i)
                encoder.writeBlobTlv(Tlv.NameComponent, name.get(i).getValue().buf());

            encoder.writeTypeAndLength(type, encoder.getLength() - saveLength);
        }

        /// <summary>
        /// Encode the value as NDN-TLV AppRequest, according to this Protobuf definition:
        /// </summary>
        ///
        /// <param name="idName">The idName.</param>
        /// <param name="dataPrefix">The dataPrefix</param>
        /// <param name="appName">The appName.</param>
        /// <returns>A Blob containing the encoding.</returns>
        static public Blob encodeAppRequest(Name idName, Name dataPrefix, string appName)
        {
            TlvEncoder encoder = new TlvEncoder();
            int saveLength = encoder.getLength();

            const int Tlv_idName = 220;
            const int Tlv_dataPrefix = 221;
            const int Tlv_appName = 222;
            const int Tlv_AppRequest = 223;

            // Encode backwards.
            encoder.writeBlobTlv(Tlv_appName, new Blob(appName).buf());
            encodeName(dataPrefix, Tlv_dataPrefix, encoder);
            encodeName(idName, Tlv_idName, encoder);

            encoder.writeTypeAndLength(Tlv_AppRequest, encoder.getLength() - saveLength);
            return new Blob(encoder.getOutput(), false);
        }
    }
}