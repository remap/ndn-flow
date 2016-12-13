namespace ndn_iot.tests {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    // TODO: to be finished up with consumer functions as in other languages
    using System.Runtime.InteropServices;

    using net.named_data.jndn.security.policy;    
    using net.named_data.jndn;
    using net.named_data.jndn.encoding;
    using net.named_data.jndn.util;
    using net.named_data.jndn.security;
    using net.named_data.jndn.security.identity;
    using net.named_data.jndn.security.certificate;
    using net.named_data.jndn.encoding.tlv;

    using net.named_data.jndn.transport;

    using ndn_iot.bootstrap;
    using ndn_iot.discovery;

    class TestDiscovery {
        class DataHandler : OnVerified, OnVerifyFailed, OnTimeout {
            public void onVerified(Data data) {
                Console.Out.WriteLine("Data received: " + data.getName().toUri());
            }

            public void onVerifyFailed(Data data) {
                Console.Out.WriteLine("Data verify failed: " + data.getName().toUri());

            }

            public void onTimeout(Interest interest) {
                Console.Out.WriteLine("Interest times out: " + interest.getName().toUri());
                return;
            }
        }

        class EntityInfo : EntityInfoBase {
            public string getDescription() {
                return description_;
            }

            public void setDescription(string desc) {
                description_ = desc;
            }

            private string description_;
        }

        class Observer : ExternalObserver {
            public void onStateChanged(string name, string msgTyle, string message) {
                Console.Out.WriteLine("Observer: " + name + " : " + msgTyle + " : " + message);
            }
        }

        class Serializer : EntitySerializer {
            public string serialize(EntityInfo info) {
                return info.getDescription();
            }
        }

        private static Random random = new Random();
        public static string getRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static void Main(string[] args)
        {
            Face face = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo("localhost"));
            Bootstrap bootstrap = new Bootstrap(face);
            string objectPrefix = "/home/flow-csharp";

            KeyChain keyChain = bootstrap.setupDefaultIdentityAndRoot(new Name(objectPrefix), new Name());
            Name certificateName = bootstrap.getDefaultCertificateName();

            // separate debug function for creating ID and cert
            //bootstrap.createIdentityAndCertificate(new Name("/home/flow/csharp-publisher-1"));
            
            Name syncPrefix = new Name("/home/discovery");
            Observer observer = new Observer();
            EntitySerializer serializer = new EntitySerializer();

            SyncBasedDiscovery discovery = new SyncBasedDiscovery(face, keyChain, certificateName, syncPrefix, observer, serializer);
            discovery.start();

            Name entityName = new Name(objectPrefix).append(getRandomString(3));
            EntityInfo ei = new EntityInfo();
            ei.setDescription(entityName.toUri());

            discovery.addHostedObject(entityName.toUri(), ei);

            while (true) {
                face.processEvents();
                // We need to sleep for a few milliseconds so we don't use 100% of the CPU.
                System.Threading.Thread.Sleep(1);
            }
        }
    }
}