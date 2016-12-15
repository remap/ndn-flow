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

    class AppConsumer {
        class VerificationHandler : OnVerified, OnDataValidationFailed {
            public void onVerified(Data data) {
                Console.Out.WriteLine("Data verified");
            }

            public void onDataValidationFailed(Data data, string reason) {
                Console.Out.WriteLine("Data verification failed: " + reason);
            }
        }

        class DataHandler : OnData, OnTimeout {
            public DataHandler(AppConsumer ac) {
                ac_ = ac;
            }

            public void onData(Interest interest, Data data) {
                Console.Out.WriteLine("data received: " + data.getName().toUri() + "; verifying!");
                VerificationHandler vh = new VerificationHandler();
                ac_.keyChain_.verifyData(data, vh, vh);
            }

            public void onTimeout(Interest interest) {
                Console.Out.WriteLine("interest timed out: " + interest.getName().toUri());
            }

            AppConsumer ac_;
        }

        void onUpdateSuccess(string schema, bool isInitial) {
            Console.Out.WriteLine("trust schema update success");
            DataHandler dh = new DataHandler(this);

            if (isInitial) {
                face_.expressInterest(new Name("/home/flow/cpp-publisher-1"), dh, dh);
            }
        }

        void onUpdateFailed(string msg) {
            Console.Out.WriteLine("Trust schema update failed: " + msg);
        }

        public void run()
        {
            face_ = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo("localhost"));
            Bootstrap bootstrap = new Bootstrap(face_);
            keyChain_ = bootstrap.setupDefaultIdentityAndRoot(new Name("/home/flow-csharp"), new Name());

            bootstrap.startTrustSchemaUpdate(new Name("/home/gateway/flow"), onUpdateSuccess, onUpdateFailed);
            while (true) {
                face_.processEvents();
                // We need to sleep for a few milliseconds so we don't use 100% of the CPU.
                System.Threading.Thread.Sleep(5);
            }
        }

        KeyChain keyChain_;
        Face face_;
    }

    class TestConsumer {
        static void Main(string[] args) {
            AppConsumer apConsumer = new AppConsumer();
            apConsumer.run();
        }
    }
}