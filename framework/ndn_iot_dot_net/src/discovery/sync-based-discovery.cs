namespace ndn_iot.discovery {
    using System;
    
    using net.named_data.jndn;
    using net.named_data.jndn.util;
    using net.named_data.jndn.security;

    public class SyncBasedDiscovery {
        public SyncBasedDiscovery
          (Face face, KeyChain keyChain, Name certificateName, Name syncPrefix, 
           ExternalObserver observer, EntitySerializer serializer) {
               face_ = face;
               keyChain_ = keyChain;
               certificateName_ = certificateName;
               syncPrefix_ = syncPrefix;

               observer_ = observer;
               serializer_ = serializer;

               // defaults set by the library
               initialDigest_ = "00";
               currentDigest_ = "00";

               syncDataFreshnessPeriod_ = 4000;
               syncInterestLifetime_ = 4000;
               syncInterestMinInterval_ = 500;
               timeoutCntThreshold_ = 3;
               entityDataFreshnessPeriod_ = 10000;
        }

        // public facing interface
        public void start() {
            updateDigest();
        }

        public void stop() {

        }

        public Dictionary<string, EntityInfo> getObjects() {
            return objects_;
        }

        public Dictionary<string, EntityInfo> getHostedObjects() {
            return hostedObjects_;
        }

        public void addHostedObject(string name, EntityInfo entityInfo) {

        }

        public bool removeHostedObject(string name) {

        }

        // internal functions
        private void contentCacheAddEntityData(string name, EntityInfo entityInfo) {

        }

        private void contentCacheAddSyncData(string name, EntityInfo entityInfo) {

        }

        public class SyncInterestHandler: OnInterest, OnRegisterFailed {
            public SyncInterestHandler() {

            }

            public void onInterest
              (Name prefix, Interest interest, Face face, long interestFilterId, 
               InterestFilter filter) {

            }

            public void onRegisterFailed(Name prefix) {

            }
        }

        public class SyncDataHandler: OnData, OnTimeout {
            public SyncDataHandler() {

            }

            public void onData(Interest interest, Data data) {

            }

            public void onTimeout(Interest interest) {

            }
        }

        public class EntityDataHandler: OnData, OnTimeout {
            public EntityDataHandler() {

            }

            public void onData(Interest interest, Data data) {

            }

            public void onTimeout(Interest interest) {

            }
        }

        public class HeartbeatDataHandler: OnData, OnTimeout() {
            public HeartbeatDataHandler() {

            }
            
            public void onData(Interest interest, Data data) {

            }

            public void onTimeout(Interest interest) {

            }      
        }

        private bool addObject(string name) {

        }

        private bool removeObject(string name) {

        }

        private void updateDigest() {

        }

        Face face_;
        keyChain keyChain_;
        Name syncPrefix_;

        Dictionary<string, EntityInfo> objects_;
        Dictionary<string, EntityInfo> hostedObjects_;

        MemoryContentCache memoryContentCache_;
        Name certificateName_;

        string currentDigest_;
        int syncDataFreshnessPeriod_;
        string initialDigest_;
        int syncInterestLifetime_;

        int syncInterestMinInterval_;
        int timeoutCntThreshold_;
        int entityDataFreshnessPeriod_;

        ExternalObserver observer_;
        EntitySerializer serializer_;
    }
}