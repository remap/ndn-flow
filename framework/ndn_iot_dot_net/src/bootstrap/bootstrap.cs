namespace ndn_iot.bootstrap {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    //using Google.Protobuf;

    using net.named_data.jndn.security.policy;    
    using net.named_data.jndn;
    using net.named_data.jndn.encoding;
    using net.named_data.jndn.util;
    using net.named_data.jndn.security;
    using net.named_data.jndn.security.identity;
    using net.named_data.jndn.security.certificate;

    // TODO: to be removed
    using net.named_data.jndn.transport;

    // TODO: abandoned protobuf-C# for now, not wise to investigate given we don't have enough time, hack controller instead
    //using ndn_iot.bootstrap.command;

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

        public void setupDefaultIdentityAndRoot(Name defaultIdentityName, Name signerName) {
            setupDefaultIdentityAndRoot(defaultIdentityName, defaultCertFileName_, signerName);
        }

        public void setupDefaultIdentityAndRoot(Name defaultIdentityName, string certFilePath, Name signerName) {
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
                    throw new SystemException("Given certificate file does not match with the default key for the configured identity!");
                }

                PublicKey publicKey = filePrivateKeyStorage_.getPublicKey(defaultKeyName_);
                memoryIdentityStorage_.addKey(defaultKeyName_, publicKey.getKeyType(), new Blob(publicKey.getKeyDer()));
            } catch (SecurityException ex) {
                Console.Out.WriteLine(ex.Message);
                throw new SystemException("Security exception: " + ex.Message + " (default identity: " + defaultIdentity_.toUri() + ")");
            }

            Name actualSignerName = KeyLocator.getFromSignature(certData.getSignature()).getKeyName();
            if (signerName.size() > 0 && !(actualSignerName.equals(signerName))) {
                throw new SystemException("Security exception: expected signer name does not match with actual signer name: " + signerName.toUri() + " " + actualSignerName.toUri());
            }

            face_.setCommandSigningInfo(keyChain_, defaultCertificateName_);
            certificateContentCache_.registerPrefix(new Name(defaultCertificateName_).getPrefix(-1), this);
        }

        public void sendAppRequest(Name certificateName, Name dataPrefix, string applicationName) {
            
        }

        public void onRegisterFailed(Name prefix) {
            Console.Out.WriteLine("Registration failed for prefix: " + prefix.toUri());
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
        Dictionary<string, string> trustSchemas_;
    }

    class TestEchoConsumer {
        static void Main(string[] args)
        {
            var face = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo("localhost"));
            Bootstrap bootstrap = new Bootstrap(face);
            bootstrap.setupDefaultIdentityAndRoot(new Name("/org/openmhealth/zhehaowang"), new Name());
        }
    }
}