namespace ndn_iot.tests {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

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

    class AppProducer {

        void onRequestSuccess() {
            Console.Out.WriteLine("Request granted!");
            Data data = new Data(new Name("/home/flow/csharp-publisher-1/0"));
            string content = "good good";
            data.setContent(new Blob(content));
            keyChain.sign(data, certificateName);

            memoryContentCache.add(data);
        }

        void onRequestFailed(string msg) {
            Console.Out.WriteLine(msg);
        }

        public void run()
        {
            face = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo("localhost"));
            Bootstrap bootstrap = new Bootstrap(face);
            keyChain = bootstrap.setupDefaultIdentityAndRoot(new Name("/home/flow-csharp"), new Name());
            certificateName = bootstrap.getDefaultCertificateName();
            memoryContentCache = new MemoryContentCache(face);
            
            // separate debug function for creating ID and cert
            //bootstrap.createIdentityAndCertificate(new Name("/home/flow/csharp-publisher-1"));

            // main is static so cannot refer to non-static members here, if want to make onRequestSuccess and onRequestFailed non-static
            bootstrap.requestProducerAuthorization(
              new Name("/home/flow/csharp-publisher-1"), 
              "flow", 
              new OnRequestSuccess(onRequestSuccess), 
              new OnRequestFailed(onRequestFailed));

            while (true) {
                face.processEvents();
                // We need to sleep for a few milliseconds so we don't use 100% of the CPU.
                System.Threading.Thread.Sleep(5);
            }
        }

        public KeyChain keyChain;
        public Face face; 
        public Name certificateName;
        public MemoryContentCache memoryContentCache;
    }

    class TestProducer {
        static void Main(string[] args) {
            AppProducer apProducer = new AppProducer();
            apProducer.run();
        }
    }
}