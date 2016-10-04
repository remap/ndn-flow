namespace ndn_iot.consumer {
    using System;
    
    using net.named_data.jndn;
    using net.named_data.jndn.security;

    // TODO: the timeout / verify failed re-expression mechanism here will likely cause dangling: 
    // DataHandler gets created over and over again, for each interest that times out. Should check implementation again.
    // actually, not just the retrans mechanism, ordinary fetching could cause dangling, too

    public class AppConsumerSequenceNumber : AppConsumer {
        public AppConsumerSequenceNumber
          (Face face, KeyChain keyChain, Name certificateName, bool doVerify, int defaultPipelineSize = 5, int defaultSeqNumber = 0) {
            face_ = face;
            keyChain_ = keyChain;
            certificateName_ = certificateName;
            doVerify_ = doVerify;

            pipelineSize_ = defaultPipelineSize;
            currentSeqNumber_ = defaultSeqNumber;
            emptySlot_ = defaultPipelineSize;

            verifyFailedRetransInterval_ = 4000;
            defaultInterestLifetime_ = 4000;
        }

        public void consume(Name prefix, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout) {
            int num = emptySlot_;

            for (int i = 0; i < num; i++) {
                Name name = new Name(prefix);
                name.append(Convert.ToString(currentSeqNumber_));
                Interest interest = new Interest(name);
                
                // interest configuration / template?
                interest.setInterestLifetimeMilliseconds(defaultInterestLifetime_);

                DataHandler dh = new DataHandler(this, onVerified, onVerifyFailed, onTimeout);
                face_.expressInterest(interest, dh, dh);
                Console.Out.WriteLine("interest expressed: " + interest.getName().toUri());

                currentSeqNumber_ += 1;
                emptySlot_ -= 1;
            }
        }

        public class DataHandler : OnData, OnTimeout, OnVerified {
            public DataHandler(AppConsumerSequenceNumber aps, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout) {
                face_ = aps.getFace();
                keyChain_ = aps.getKeyChain();
                doVerify_ = aps.getDoVerify();
                verifyFailedRetransInterval_ = aps.getVerifyFailedRetransInterval();
                aps_ = aps;

                onVerified_ = onVerified;
                onVerifyFailed_ = onVerifyFailed;
                onTimeout_ = onTimeout;
            }

            public void onData(Interest interest, Data data) {
                if (doVerify_) {
                    VerifyFailedHandler vfh = new VerifyFailedHandler(this, interest);
                    keyChain_.verifyData(data, this, vfh);
                } else {
                    onVerified(data);
                }
            }

            public void onTimeout(Interest interest) {
                Interest newInterest = new Interest(interest);
                newInterest.refreshNonce();

                face_.expressInterest(newInterest, this, this);
                onTimeout_.onTimeout(interest);
                Console.Out.WriteLine("interest expressed: " + newInterest.getName().toUri());
            }

            public void onVerified(Data data) {
                //aps_.incrementCurrentSeqNumber();
                aps_.incrementEmptySlot();

                aps_.consume(data.getName().getPrefix(-1), onVerified_, onVerifyFailed_, onTimeout_);
                onVerified_.onVerified(data);
            }

            public AppConsumerSequenceNumber aps_;
            public Face face_;
            public KeyChain keyChain_;
            public bool doVerify_;
            public int verifyFailedRetransInterval_;

            public OnVerified onVerified_;
            public OnVerifyFailed onVerifyFailed_;
            public OnTimeout onTimeout_;

            public class VerifyFailedHandler : OnData, OnTimeout, OnVerifyFailed {
                public VerifyFailedHandler(DataHandler dh, Interest interest) {
                    dh_ = dh;
                    interest_ = interest;
                }

                public void onVerifyFailed(Data data) {
                    Interest newInterest = new Interest(interest_);
                    newInterest.refreshNonce();
                    
                    Interest dummyInterest = new Interest(new Name("/local/timeout"));
                    dummyInterest.setInterestLifetimeMilliseconds(dh_.verifyFailedRetransInterval_);

                    dh_.face_.expressInterest(dummyInterest, this, this);
                    dh_.onVerifyFailed_.onVerifyFailed(data);
                }

                public void onData(Interest interest, Data data) {
                    Console.Out.WriteLine("Unexpected: got dummy data");
                }

                public void onTimeout(Interest interest) {
                    dh_.face_.expressInterest(interest, dh_, dh_);
                }

                DataHandler dh_;
                Interest interest_;
            }
        }

        public Face getFace() { return face_; }

        public KeyChain getKeyChain() { return keyChain_; }

        public bool getDoVerify() { return doVerify_; }

        public int getEmptySlot() { return emptySlot_; }

        public void setEmptySlot(int emptySlot) { emptySlot_ = emptySlot; return; }

        public void incrementEmptySlot() { emptySlot_ += 1; }

        public int getCurrentSeqNumber() { return currentSeqNumber_; }

        public void setCurrentSeqNumber(int seqNumber) { currentSeqNumber_ = seqNumber; return; }

        public void incrementCurrentSeqNumber() { currentSeqNumber_ += 1; }

        public int getVerifyFailedRetransInterval() { return verifyFailedRetransInterval_; }

        private Face face_;
        private KeyChain keyChain_;
        private Name certificateName_;
        private bool doVerify_;
        
        private int pipelineSize_;
        private int currentSeqNumber_;
        private int emptySlot_;

        private int verifyFailedRetransInterval_;
        private int defaultInterestLifetime_;
    }
}

