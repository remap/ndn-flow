namespace ndn_iot.bootstrap {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

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

    class TestBootstrap {
        static void onRequestSuccess() {
            Console.Out.WriteLine("Request granted!");
        }

        static void onRequestFailed(string msg) {
            Console.Out.WriteLine(msg);
        }

        static void Main(string[] args)
        {
            Face face = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo("localhost"));
            Bootstrap bootstrap = new Bootstrap(face);
            KeyChain keyChain = bootstrap.setupDefaultIdentityAndRoot(new Name("/home/flow-csharp"), new Name());

            Console.Out.WriteLine((double) (DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds);
            
            // separate debug function for creating ID and cert
            //bootstrap.createIdentityAndCertificate(new Name("/home/flow/csharp-publisher-1"));

            // main is static so cannot refer to non-static members here, if want to make onRequestSuccess and onRequestFailed non-static
            bootstrap.requestProducerAuthorization(
              new Name("/home/flow/csharp-publisher-1"), 
              "flow", 
              new OnRequestSuccess(onRequestSuccess), 
              new OnRequestFailed(onRequestFailed));

            Interest interest = new Interest(new Name("/abc"));
            keyChain.sign(interest, bootstrap.getDefaultCertificateName());
            Console.Out.WriteLine(interest.getName().toUri());

            while (true) {
                face.processEvents();
                // We need to sleep for a few milliseconds so we don't use 100% of the CPU.
                System.Threading.Thread.Sleep(5);
            }
        }
    }
}