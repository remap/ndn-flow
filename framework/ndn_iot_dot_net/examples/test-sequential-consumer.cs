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
    using ndn_iot.consumer;

    class TestSequentialConsumer {
        class ConsumerDataHandler : OnVerified, OnVerifyFailed, OnTimeout {
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

        static void Main(string[] args)
        {
            Face face = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo("localhost"));
            Bootstrap bootstrap = new Bootstrap(face);
            KeyChain keyChain = bootstrap.setupDefaultIdentityAndRoot(new Name("/home/flow-csharp"), new Name());
            Name certificateName = bootstrap.getDefaultCertificateName();

            // separate debug function for creating ID and cert
            //bootstrap.createIdentityAndCertificate(new Name("/home/flow/csharp-publisher-1"));

            // main is static so cannot refer to non-static members here, if want to make onRequestSuccess and onRequestFailed non-static
            AppConsumerSequenceNumber consumer = new AppConsumerSequenceNumber(face, keyChain, certificateName, false, 5, -1);
            ConsumerDataHandler cdh = new ConsumerDataHandler();

            // todo: fill in simulator prefix
            consumer.consume(new Name("/home/flow1/gyro-sim1"), cdh, cdh, cdh);

            //Interest interest = new Interest(new Name("/abc"));
            //keyChain.sign(interest, bootstrap.getDefaultCertificateName());
            //Console.Out.WriteLine(interest.getName().toUri());

            while (true) {
                face.processEvents();
                // We need to sleep for a few milliseconds so we don't use 100% of the CPU.
                System.Threading.Thread.Sleep(1);
            }
        }
    }
}