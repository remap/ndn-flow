namespace ndn_iot.discovery {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Linq;

    // Sha256 for digest computation    
    using System.Security.Cryptography;

    using net.named_data.jndn;
    using net.named_data.jndn.util;
    using net.named_data.jndn.security;
    using net.named_data.jndn.transport;

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
            entityDataFreshnessPeriod_ = 10000;

            sdh_ = new SyncDataHandler(this);
            sih_ = new SyncInterestHandler(this);
            edh_ = new EntityDataHandler(this);
            cih_ = new CacheInterestHandler(this);
            dsdh_ = new DummySyncDataHandler(this);
            hdh_ = new HeartbeatDataHandler(this);

            objects_ = new SortedDictionary<string, EntityInfoBase>();
            hostedObjects_ = new SortedDictionary<string, EntityInfoBase>();

            memoryContentCache_ = new MemoryContentCache(face_);
        }

        // public facing interface
        public void start() {
            updateDigest();
            Interest interest = new Interest((new Name(syncPrefix_)).append(currentDigest_));
            interest.setMustBeFresh(true);
            interest.setInterestLifetimeMilliseconds(syncInterestLifetime_);
            face_.expressInterest(interest, sdh_, sdh_);
        }

        public void stop() {
            memoryContentCache_.unregisterAll();
        }

        public SortedDictionary<string, EntityInfoBase> getObjects() {
            return objects_;
        }

        public SortedDictionary<string, EntityInfoBase> getHostedObjects() {
            return hostedObjects_;
        }

        public void addHostedObject(string name, EntityInfoBase entityInfo) {
            // If this is the first object we host, we register for sync namespace: meaning a participant not hosting anything 
            // is only "listening" for sync, and will not help in the sync process
            if (hostedObjects_.Count == 0) {
                memoryContentCache_.registerPrefix(syncPrefix_, sih_, sih_);
            }
            if (addObject(name)) {
                hostedObjects_[name] = entityInfo;
                contentCacheAddEntityData(name, entityInfo);
                // TODO: should the user configure this prefix as well?
                memoryContentCache_.registerPrefix(new Name(name), cih_, cih_);
            } else {

            }
        }

        public bool removeHostedObject(string name) {
            if (hostedObjects_.ContainsKey(name)) {
                hostedObjects_.Remove(name);
                if (hostedObjects_.Count == 0) {
                    memoryContentCache_.unregisterAll();
                }
                if (removeObject(name)) {
                    return true;
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }

        // getters
        public Name getSyncPrefix() {
            return syncPrefix_;
        }

        public string getCurrentDigest() {
            return currentDigest_;
        }

        public Face getFace() {
            return face_;
        }

        // internal functions
        public string contentToString(Data data) {
            var content = data.getContent().buf();
            var contentString = "";
            for (int i = content.position(); i < content.limit(); ++i)
                contentString += (char)content.get(i);
                Console.Out.WriteLine(contentString);
            return contentString;
        }

        public void expressSyncInterest() {
            Interest newInterest = new Interest(new Name(syncPrefix_).append(currentDigest_));
            newInterest.setInterestLifetimeMilliseconds(syncInterestLifetime_);
            newInterest.setMustBeFresh(true);
            face_.expressInterest(newInterest, sdh_, sdh_);
        }

        public void onReceivedSyncData(string itemName) {
            Console.Out.WriteLine("Sync data received: " + itemName);
            Interest interest = new Interest(new Name(itemName));
            interest.setInterestLifetimeMilliseconds(4000);
            interest.setMustBeFresh(false);
            face_.expressInterest(interest, edh_, edh_);
        }
        
        private void contentCacheAddEntityData(string name, EntityInfoBase entityInfo) {
            string content = serializer_.serialize(entityInfo);
            Data data = new Data(new Name(name));

            data.setContent(new Blob(content));
            // Interest issuer should not ask for mustBeFresh in this case, for now
            data.getMetaInfo().setFreshnessPeriod(entityDataFreshnessPeriod_);
            keyChain_.sign(data, certificateName_);
            memoryContentCache_.add(data);
        }

        public void contentCacheAddSyncData(Name name) {
            string content = "";
            if (objects_.Count == 0) {
                content = "";
            } else {
                List<string> keys = objects_.Keys.ToList();
                content = keys[0];
                for (int i = 1; i < objects_.Keys.Count; i++)
                    content += "\n" + keys[i];
            }
            Console.Out.WriteLine("added sync data: " + content);
            
            Data data = new Data(new Name(name));
            data.setContent(new Blob(content));
            data.getMetaInfo().setFreshnessPeriod(syncDataFreshnessPeriod_);
            keyChain_.sign(data, certificateName_);
            // adding this data to memoryContentCache should satisfy the pending interest
            memoryContentCache_.add(data);
        }

        public class SyncInterestHandler: OnInterestCallback, OnRegisterFailed {
            public SyncInterestHandler(SyncBasedDiscovery sbd) {
                sbd_ = sbd;
            }

            public void onInterest
              (Name prefix, Interest interest, Face face, long interestFilterId,
                InterestFilter filter) {
                if (interest.getName().size() != sbd_.getSyncPrefix().size() + 1) {
                    // not an interest for us
                    return;
                }
                string digest = interest.getName().get(-1).toEscapedString();
                sbd_.updateDigest();
                if (sbd_.getCurrentDigest() != digest) {
                    /*
                      TODO: one participant may be answering with wrong info: scenario: 1 has {a}, 2 has {b}
                      2 gets 1's {a} and asks again before 1 gets 2's {b}, 2 asks 1 with the digest of {a, b}, 1 will 
                      create a data with the content {a} for the digest of {a, b}, and this data will be able to answer
                      later steady state interests from 2 until it expires (and by which time 1 should be updated with
                      {a, b} as well)
                    */
                    sbd_.contentCacheAddSyncData(new Name(sbd_.getSyncPrefix()).append(digest));
                }
            }

            public void onRegisterFailed(Name prefix) {
                Console.Out.WriteLine("Prefix registration failed: " + prefix.toUri());
            }

            SyncBasedDiscovery sbd_;
        }

        public class CacheInterestHandler: OnInterestCallback, OnRegisterFailed {
            public CacheInterestHandler(SyncBasedDiscovery sbd) {
                sbd_ = sbd;
            }

            // this is still using the old onInterest interface
            public void onInterest
              (Name prefix, Interest interest, Face face, long interestFilterId,
                InterestFilter filter) {
                Console.Out.WriteLine("Data not found in cache: " + interest.getName().toUri());
            }

            public void onRegisterFailed(Name prefix) {
                Console.Out.WriteLine("Prefix registration failed: " + prefix.toUri());
            }

            SyncBasedDiscovery sbd_;
        }

        public class SyncDataHandler: OnData, OnTimeout {
            public SyncDataHandler(SyncBasedDiscovery sbd) {
                sbd_ = sbd;
            }

            public void onData(Interest interest, Data data) {
                string[] content = sbd_.contentToString(data).Split('\n');

                for (int i = 0; i < content.Length; i++) {
                    if (!(sbd_.getObjects().ContainsKey(content[i]))) {
                        if (content[i] != "") {
                            sbd_.onReceivedSyncData(content[i]);
                        }
                    }
                }

                // Hack for re-expressing sync interest after a short interval
                Interest dummyInterest = new Interest(new Name("/local/timeout"));
                dummyInterest.setInterestLifetimeMilliseconds(sbd_.syncInterestMinInterval_);
                sbd_.getFace().expressInterest(dummyInterest, sbd_.dsdh_, sbd_.dsdh_);
            }

            public void onTimeout(Interest interest) {
                Interest newInterest = new Interest(new Name(sbd_.getSyncPrefix()).append(sbd_.getCurrentDigest()));
                newInterest.setInterestLifetimeMilliseconds(sbd_.syncInterestLifetime_);
                newInterest.setMustBeFresh(true);
                sbd_.getFace().expressInterest(newInterest, this, this);
            }

            SyncBasedDiscovery sbd_;
        }

        public class EntityDataHandler: OnData, OnTimeout {
            public EntityDataHandler(SyncBasedDiscovery sbd) {
                sbd_ = sbd;
            }

            public void onData(Interest interest, Data data) {
                sbd_.addObject(interest.getName().toUri());

                Interest dummyInterest = new Interest(new Name("/local/timeout"));
                dummyInterest.setInterestLifetimeMilliseconds(4000);

                DummyHeartbeatDataHandler dhdh = new DummyHeartbeatDataHandler(sbd_, interest);
                sbd_.getFace().expressInterest(dummyInterest, dhdh, dhdh);
            }

            public void onTimeout(Interest interest) {

            }

            SyncBasedDiscovery sbd_;
        }

        public class HeartbeatDataHandler: OnData, OnTimeout {
            public HeartbeatDataHandler(SyncBasedDiscovery sbd) {
                sbd_ = sbd;
            }

            public void onData(Interest interest, Data data) {
                string entityName = interest.getName().toUri();

                if (sbd_.getObjects().ContainsKey(entityName)) {
                    sbd_.getObjects()[entityName].resetTimeoutCnt();
                }
                Interest dummyInterest = new Interest(new Name("/local/timeout"));
                dummyInterest.setInterestLifetimeMilliseconds(4000);

                DummyHeartbeatDataHandler dhdh = new DummyHeartbeatDataHandler(sbd_, interest);
                sbd_.getFace().expressInterest(dummyInterest, dhdh, dhdh);
            }

            public void onTimeout(Interest interest) {
                string entityName = interest.getName().toUri();

                if (sbd_.getObjects().ContainsKey(entityName)) {
                    if (sbd_.getObjects()[entityName].incrementTimeoutCnt()) {
                        Console.Out.WriteLine("Remove: " + interest.getName().toUri() + " because of consecutive timeout cnt exceeded");
                        sbd_.removeObject(entityName);
                    } else {
                        Interest newInterest = new Interest(interest.getName());
                        newInterest.setInterestLifetimeMilliseconds(4000);
                        sbd_.getFace().expressInterest(newInterest, this, this);
                    }
                }
            }

            SyncBasedDiscovery sbd_;
        }

        // expresses sync interest after expiration
        public class DummySyncDataHandler: OnData, OnTimeout {
            public DummySyncDataHandler(SyncBasedDiscovery sbd) {
                sbd_ = sbd;
            }
            
            public void onData(Interest interest, Data data) {
                Console.Out.WriteLine("Unexpected: dummy data");
            }

            public void onTimeout(Interest interest) {
                sbd_.expressSyncInterest();
            }

            SyncBasedDiscovery sbd_;
        }

        // expresses entity heartbeat interest after timeout
        public class DummyHeartbeatDataHandler: OnData, OnTimeout {
            public DummyHeartbeatDataHandler(SyncBasedDiscovery sbd, Interest interest) {
                sbd_ = sbd;
                interest_ = interest;
            }
            
            public void onData(Interest interest, Data data) {
                Console.Out.WriteLine("Unexpected: dummy data");
            }

            public void onTimeout(Interest interest) {
                Interest newInterest = new Interest(interest_);
                newInterest.refreshNonce();
                sbd_.getFace().expressInterest(newInterest, sbd_.hdh_, sbd_.hdh_);
            }

            SyncBasedDiscovery sbd_;
            Interest interest_;
        }

        private bool addObject(string name) {
            if (objects_.ContainsKey(name)) {
                return false;
            } else {
                // we don't actually store the entity-info for entities discovered by this instance?
                objects_[name] = new EntityInfoBase();
                notifyObserver(name, "ADD", "");
                contentCacheAddSyncData((new Name(syncPrefix_)).append(currentDigest_));
                updateDigest();
                return true;
            }
        }

        private bool removeObject(string name) {
            if (objects_.ContainsKey(name)) {
                objects_.Remove(name);
                notifyObserver(name, "REMOVE", "");
                contentCacheAddSyncData((new Name(syncPrefix_)).append(currentDigest_));
                updateDigest();
                return true;
            } else {
                return false;
            }
        }

        // SecuritySHA256 copied from ndn-dot-net
        public class SecuritySHA256 {
            public static SecuritySHA256
            Create() { return new SecuritySHA256(); }

            public void
            update(byte[] data) { memoryStream_.Write(data, 0, data.Length); }

            public byte[] Hash {
                get { 
                    memoryStream_.Flush();
                    var result = sha256_.ComputeHash(memoryStream_.ToArray());

                    // We don't need the data in the stream any more.
                    memoryStream_.Dispose();
                    memoryStream_ = null;

                    return result;
                }
            }

            private SHA256 sha256_ = SHA256Managed.Create();
            private MemoryStream memoryStream_ = new MemoryStream();
        }


        private static string hexStringFromBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }

        private void updateDigest() {
            if (objects_.Count > 0) {
                SecuritySHA256 sha256;
                try {
                    sha256 = SecuritySHA256.Create();
                } catch (Exception exception) {
                    // Don't expect this to happen.
                    throw new Exception("MessageDigest: SHA-256 is not supported: "
                            + exception.Message);
                }
                foreach (KeyValuePair<string, EntityInfoBase> entry in objects_) {
                    // TODO: this is assuming other languages use UTF8 and "updates" hash in such a way
                    sha256.update(Encoding.UTF8.GetBytes(entry.Key));
                }
                currentDigest_ = hexStringFromBytes(sha256.Hash);
            } else {
                currentDigest_ = initialDigest_;
            }
        }

        private void notifyObserver(string name, string msgType, string msg) {
            observer_.onStateChanged(name, msgType, msg);
        }

        Face face_;
        KeyChain keyChain_;
        Name syncPrefix_;

        SortedDictionary<string, EntityInfoBase> objects_;
        SortedDictionary<string, EntityInfoBase> hostedObjects_;

        MemoryContentCache memoryContentCache_;
        Name certificateName_;

        SyncDataHandler sdh_;
        SyncInterestHandler sih_;
        EntityDataHandler edh_;
        CacheInterestHandler cih_;
        public DummySyncDataHandler dsdh_;
        public HeartbeatDataHandler hdh_;

        string currentDigest_;
        int syncDataFreshnessPeriod_;
        string initialDigest_;
        int syncInterestLifetime_;

        public int syncInterestMinInterval_;
        public static int TimeoutCntThreshold = 3;
        int entityDataFreshnessPeriod_;

        ExternalObserver observer_;
        EntitySerializer serializer_;
    }
}