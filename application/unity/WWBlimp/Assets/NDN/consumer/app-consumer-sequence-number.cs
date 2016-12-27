namespace ndn_iot.consumer {
    using System;
    
    using net.named_data.jndn;
    using net.named_data.jndn.security;

    public class AppConsumerSequenceNumber : AppConsumer {
        // Check: the default param to -1 does not work, for now manually passing -1 to this function to start from rightMostChild
        public AppConsumerSequenceNumber
          (Face face, KeyChain keyChain, bool doVerify, int defaultPipelineSize = 5, int defaultSeqNumber = -1) {
            face_ = face;
            keyChain_ = keyChain;
            doVerify_ = doVerify;

            pipelineSize_ = defaultPipelineSize;

            currentSeqNumber_ = defaultSeqNumber;
            emptySlot_ = defaultPipelineSize;

            verifyFailedRetransInterval_ = 4000;
            defaultInterestLifetime_ = 4000;
            dh_ = null;
        }

        public int getPipelineSize() {
            return pipelineSize_;
        }

        public void consume(Name prefix, OnVerified onVerified, OnDataValidationFailed onVerifyFailed, OnTimeout onTimeout) {
            if (dh_ == null) {
                dh_ = new DataHandler(this, onVerified, onVerifyFailed, onTimeout, currentSeqNumber_ < 0);
            }
            if (currentSeqNumber_ < 0) {
                Name name = new Name(prefix);
                Interest interest = new Interest(name);
                interest.setChildSelector(1);
                interest.setInterestLifetimeMilliseconds(defaultInterestLifetime_);
                face_.expressInterest(interest, dh_, dh_);
            } else {
                int num = emptySlot_;
                for (int i = 0; i < num; i++) {
                    Name name = new Name(prefix);
                    name.append(Convert.ToString(currentSeqNumber_));
                    Interest interest = new Interest(name);
                    
                    // interest configuration / template?
                    interest.setInterestLifetimeMilliseconds(defaultInterestLifetime_);

                    face_.expressInterest(interest, dh_, dh_);

                    currentSeqNumber_ += 1;
                    emptySlot_ -= 1;
                }
            }
        }

        public class DataHandler : OnData, OnTimeout, OnVerified {
            public DataHandler(AppConsumerSequenceNumber aps, OnVerified onVerified, OnDataValidationFailed onVerifyFailed, OnTimeout onTimeout, bool resetSeqNumber = false) {
                face_ = aps.getFace();
                keyChain_ = aps.getKeyChain();
                doVerify_ = aps.getDoVerify();
                verifyFailedRetransInterval_ = aps.getVerifyFailedRetransInterval();
                aps_ = aps;

                onVerified_ = onVerified;
                onVerifyFailed_ = onVerifyFailed;
                onTimeout_ = onTimeout;
                resetSeqNumber_ = resetSeqNumber;
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
            }

            public void onVerified(Data data) {
                //aps_.incrementCurrentSeqNumber();
                if (!resetSeqNumber_) {
                    aps_.incrementEmptySlot();
                } else {
                    var seqNumber = Convert.ToInt32(data.getName().get(-1).toEscapedString());
                    aps_.currentSeqNumber_ = seqNumber + 1;
                    resetSeqNumber_ = false;
                }
                
                aps_.consume(data.getName().getPrefix(-1), onVerified_, onVerifyFailed_, onTimeout_);
                onVerified_.onVerified(data);
            }

            public AppConsumerSequenceNumber aps_;
            public Face face_;
            public KeyChain keyChain_;
            public bool doVerify_;
            public int verifyFailedRetransInterval_;

            public OnVerified onVerified_;
            public OnDataValidationFailed onVerifyFailed_;
            public OnTimeout onTimeout_;
            private bool resetSeqNumber_;

            public class VerifyFailedHandler : OnData, OnTimeout, OnDataValidationFailed {
                public VerifyFailedHandler(DataHandler dh, Interest interest) {
                    dh_ = dh;
                    interest_ = interest;
                }

                public void onDataValidationFailed(Data data, string reason) {
                    Interest newInterest = new Interest(interest_);
                    newInterest.refreshNonce();
                    
                    Interest dummyInterest = new Interest(new Name("/local/timeout"));
                    dummyInterest.setInterestLifetimeMilliseconds(dh_.verifyFailedRetransInterval_);

                    dh_.face_.expressInterest(dummyInterest, this, this);
                    dh_.onVerifyFailed_.onDataValidationFailed(data, reason);
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
        private bool doVerify_;
        
        private int pipelineSize_;
        private int currentSeqNumber_;
        private int emptySlot_;

        private int verifyFailedRetransInterval_;
        private int defaultInterestLifetime_;

        private DataHandler dh_;
    }
}

