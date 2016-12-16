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

    class AppProducer : OnRegisterFailed, OnInterestCallback, OnRegisterSuccess {
        class DummyHandler : OnData, OnTimeout {
            public DummyHandler(AppProducer apProducer) {
                apProducer_ = apProducer;
            }

            public void onData(Interest interest, Data data) {
                Console.Out.WriteLine("Got dummy data, unexpected");
            }

            public void onTimeout(Interest interest) {
                apProducer_.publishData();
            }

            AppProducer apProducer_;
        }

        public AppProducer(Name producerNamespace) {
            producerNamespace_ = producerNamespace;
            dh_ = new DummyHandler(this);
        }

        public void publishData() {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);

            Data data = new Data(new Name(producerNamespace_).append(Name.Component.fromVersion((long)t.TotalSeconds)));
            string content = "good good";
            data.setContent(new Blob(content));
            keyChain_.sign(data, certificateName_);

            memoryContentCache_.add(data);
            Console.Out.WriteLine("Data published: " + data.getName().toUri());

            Interest dummyInterest = new Interest(new Name("/local/timeout"));
            dummyInterest.setInterestLifetimeMilliseconds(4000);
            face_.expressInterest(dummyInterest, dh_, dh_);
        }

        void onRequestSuccess() {
            Console.Out.WriteLine("Request granted!");
            publishData();
        }

        void onRequestFailed(string msg) {
            Console.Out.WriteLine(msg);
            Console.Out.WriteLine("Request failed, but in this test start publishing anyway");
            publishData();
        }

        public void run()
        {
            face_ = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo("localhost"));
            Bootstrap bootstrap = new Bootstrap(face_);
            keyChain_ = bootstrap.setupDefaultIdentityAndRoot(new Name("/home/flow-csharp"), new Name());
            certificateName_ = bootstrap.getDefaultCertificateName();
            memoryContentCache_ = new MemoryContentCache(face_);
            
            // separate debug function for creating ID and cert
            //bootstrap.createIdentityAndCertificate(new Name("/home/flow/csharp-publisher-1"));

            // main is static so cannot refer to non-static members here, if want to make onRequestSuccess and onRequestFailed non-static
            bootstrap.requestProducerAuthorization(
              new Name(producerNamespace_), 
              "flow", 
              new OnRequestSuccess(onRequestSuccess), 
              new OnRequestFailed(onRequestFailed));

            memoryContentCache_.registerPrefix(producerNamespace_, this, this, this);

            while (true) {
                face_.processEvents();
                // We need to sleep for a few milliseconds so we don't use 100% of the CPU.
                System.Threading.Thread.Sleep(5);
            }
        }

        public void onRegisterSuccess(Name prefix, long id) {
            Console.Out.WriteLine("Prefix registration success");
        }

        public void onRegisterFailed(Name prefix) {
            Console.Out.WriteLine("Prefix registration failed for: " + prefix.toUri());
        }

        public void onInterest(Name prefix, Interest interest, Face face, long interestFilterId, InterestFilter filter) {
            Console.Out.WriteLine("Data not found for interest: " + interest.getName().toUri());
        }

        public KeyChain keyChain_;
        public Face face_; 
        public Name certificateName_;
        public MemoryContentCache memoryContentCache_;

        private Name producerNamespace_;
        private DummyHandler dh_;
    }

    class TestProducer {
        static void Main(string[] args) {
            AppProducer apProducer = new AppProducer(new Name("/home/flow/ts-publisher-1"));
            apProducer.run();
        }
    }
}