namespace ndn_iot.consumer {
    using System;
    
    using net.named_data.jndn;
    using net.named_data.jndn.security;

    // TODO: the timeout / verify failed re-expression mechanism here will likely cause dangling: 
    // DataHandler gets created over and over again, for each interest that times out. Should check implementation again.
    // actually, not just the retrans mechanism, ordinary fetching could cause dangling, too

    public class AppConsumerTimestamp : AppConsumer {
        public AppConsumerTimestamp
          (Face face, KeyChain keyChain, Name certificateName, bool doVerify, long currentTimestamp = -1) {
            face_ = face;
            keyChain_ = keyChain;
            certificateName_ = certificateName;
            doVerify_ = doVerify;

            currentTimestamp_ = currentTimestamp;
            verifyFailedRetransInterval_ = 4000;
            defaultInterestLifetime_ = 4000;
        }

        public void consume(Name prefix, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout) {
            Name name = new Name(prefix);
            Interest interest = new Interest(name);
            interest.setInterestLifetimeMilliseconds(defaultInterestLifetime_);

            if (currentTimestamp_ >= 0) {
                Exclude exclude = new Exclude();
                exclude.appendAny();
                exclude.appendComponent(Name.Component.fromVersion(currentTimestamp_));

                interest.setExclude(exclude);
            }

            DataHandler dh = new DataHandler(this, onVerified, onVerifyFailed, onTimeout);
            face_.expressInterest(interest, dh, dh);
        }

        public class DataHandler : OnData, OnTimeout, OnVerified {
            public DataHandler(AppConsumerTimestamp apt, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout) {
                face_ = apt.getFace();
                keyChain_ = apt.getKeyChain();
                doVerify_ = apt.getDoVerify();
                verifyFailedRetransInterval_ = apt.getVerifyFailedRetransInterval();
                apt_ = apt;

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
            }

            public void onVerified(Data data) {
                long version = data.getName().get(-1).toVersion();
                apt_.setCurrentTimestamp(version);

                apt_.consume(data.getName().getPrefix(-1), onVerified_, onVerifyFailed_, onTimeout_);
                onVerified_.onVerified(data);
            }

            public AppConsumerTimestamp apt_;
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

        public long getCurrentTimestamp() { return currentTimestamp_; }

        public void setCurrentTimestamp(long timestamp) { currentTimestamp_ = timestamp; return; }

        public int getVerifyFailedRetransInterval() { return verifyFailedRetransInterval_; }

        private Face face_;
        private KeyChain keyChain_;
        private Name certificateName_;
        private bool doVerify_;
        
        private long currentTimestamp_;
        private int verifyFailedRetransInterval_;
        private int defaultInterestLifetime_;
    }
}

