using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using net.named_data.jndn;
using net.named_data.jndn.encoding;
using net.named_data.jndn.util;
using net.named_data.jndn.transport;
using net.named_data.jndn.security;

using ndn_iot.bootstrap;
using ndn_iot.consumer;

using SimpleJSON;

public class OptConsumer {
    public Face face_;
    public KeyChain keyChain_;
    public Name certificateName_;

    public Bootstrap bootstrap_;
    public Dictionary<string, List<string>> tracks_;

    public const string instanceName = "/home/flow-csharp";
    public const string hostName = "localhost";
    public const string optPrefix = "/ndn/edu/ucla/remap/opt/node0";

    public const int defaultInitialLifetime = 4000; 
    Name.Component startTimeComponent;
    InitialDataHandler initialHandler_;

    // Use this for initialization
    public void start () {
        face_ = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo(hostName));

        bootstrap_ = new Bootstrap(face_);
        keyChain_ = bootstrap_.setupDefaultIdentityAndRoot(new Name(instanceName), new Name());
        certificateName_ = bootstrap_.getDefaultCertificateName();

        // initialize the gyro instance
        startOptConsumer();
        initialHandler_ = new InitialDataHandler (this);

        while (true) {
            face_.processEvents();
            System.Threading.Thread.Sleep(5);
        }
    }

    public void startOptConsumer() {
        Interest initialInterest = new Interest(new Name(optPrefix));
        initialInterest.setMustBeFresh(true);
        initialInterest.setInterestLifetimeMilliseconds(defaultInitialLifetime);
        // for initial interest, the rightMostChild is preferred
        initialInterest.setChildSelector(1);
        
        face_.expressInterest(initialInterest, initialHandler_, initialHandler_);
    }

    public void startHintConsumer() {
        AppConsumerTimestamp hintConsumer = new AppConsumerTimestamp(face_, keyChain_, false);
        HintHandler hh = new HintHandler(this);
        hintConsumer.consume(new Name(optPrefix).append(startTimeComponent), hh, hh, hh);
    }

    public void startTrackConsumer(string trackId) {
        AppConsumerSequenceNumber trackConsumer = new AppConsumerSequenceNumber(face_, keyChain_, false, 5, -1);
        TrackHandler th = new TrackHandler(this);
        tracks_[trackId] = new List<string>();
        trackConsumer.consume(new Name(optPrefix).append("tracks").append(trackId), th, th, th);
    }

    public void setStartTimeComponent(Name.Component comp) {
        startTimeComponent = comp;
    }

    public class InitialDataHandler : OnData, OnTimeout {
        public InitialDataHandler(OptConsumer optConsumer) {
            optConsumer_ = optConsumer;
        }

        public void onData (Interest interest, Data data) {
            Name dataName = data.getName ();

            if (dataName.size () > (new Name(OptConsumer.optPrefix)).size() + 1) {
                optConsumer_.setStartTimeComponent(dataName.get((new Name(OptConsumer.optPrefix)).size()));
                optConsumer_.startHintConsumer();
            } else {
                Console.Out.WriteLine("Got initial data whose name length did not match");
            }
        }
        public void onTimeout (Interest interest) {
            optConsumer_.startOptConsumer();
        }

        OptConsumer optConsumer_;
    }

    // HINT DATA
    // Expected data name: [root]/opt/[node_num]/[start_timestamp]/track_hint/[num]
    public class HintHandler : OnVerified, OnDataValidationFailed , OnTimeout {
        public HintHandler(OptConsumer optConsumer) {
            this.optConsumer_ = optConsumer;
        }

        public void onVerified(Data data) {
            JSONNode parsedHint = JSON.Parse (data.getContent ().toString ());

            foreach (JSONNode track in parsedHint["tracks"].AsArray) {
                string trackId = track ["id"];
                //    Debug.Log ("   " + trackID);
                // The consumer ignores the sequence number field in the hint for now;
                // As the consumer assumes it's getting the latest via outstanding interest.

                if (optConsumer_.tracks_.ContainsKey(trackId)) {
                    
                } else {
                    optConsumer_.startTrackConsumer(trackId);
                }
            }
        }

        public void onDataValidationFailed(Data data, string reason) {
            Console.Out.WriteLine("Data validation failed: " + data.getName().toUri() + " : " + reason);
        }

        public void onTimeout (Interest interest) {
            Console.Out.WriteLine("Hint interest times out");
        }

        OptConsumer optConsumer_;
    }

    public class TrackHandler : OnVerified, OnDataValidationFailed, OnTimeout {
        public TrackHandler(OptConsumer optConsumer) {
            this.optConsumer_ = optConsumer;
        }

        public void onVerified(Data data) {
            JSONNode parsedTrack = JSON.Parse (data.getContent().toString());
            string trackId = parsedTrack ["id"];
            //Debug.Log ("TrackHandler data for " + trackID);
            try {
                optConsumer_.tracks_[trackId].Add(data.getContent().toString());
            } catch (KeyNotFoundException) {
                Console.Out.WriteLine("Got Track  data for non-existant track re-creating" + trackId);
                optConsumer_.startTrackConsumer(trackId);
            }
        }

        public void onDataValidationFailed(Data data, string reason) {
            Console.Out.WriteLine("Data validation failed: " + data.getName().toUri() + " : " + reason);
        }

        public void onTimeout(Interest interest) {
            Console.Out.WriteLine("TrackHandler timeout for " + interest.getName().toUri());
        }

        OptConsumer optConsumer_;
    }
}

public class OptConsumerTest {
    public static void Main() {
        OptConsumer optConsumer = new OptConsumer();
        optConsumer.start();
    }
}
