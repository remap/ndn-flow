// Generic sync-based discovery implementation
var SyncBasedDiscovery = function SyncBasedDiscovery
  (face, keyChain, certificateName, syncPrefix, observer, serializer, 
   syncDataFreshnessPeriod, initialDigest, syncInterestLifetime, syncInterestMinInterval,
   timeoutCntThreshold, maxResponseWaitPeriod, minResponseWaitPeriod, entityDataFreshnessPeriod)
{
    if (syncDataFreshnessPeriod === undefined) {
        syncDataFreshnessPeriod = 4000;
    }
    if (initialDigest === undefined) {
        initialDigest = "00";
    }
    if (syncInterestLifetime === undefined) {
        syncInterestLifetime = 4000;
    }
    if (syncInterestMinInterval === undefined) {
        syncInterestMinInterval = 500;
    }
    if (timeoutCntThreshold === undefined) {
        timeoutCntThreshold = 3;
    }
    if (maxResponseWaitPeriod === undefined) {
        maxResponseWaitPeriod = 2000;
    }
    if (minResponseWaitPeriod === undefined) {
        minResponseWaitPeriod = 400;
    }
    if (entityDataFreshnessPeriod === undefined) {
        entityDataFreshnessPeriod = 10000;
    }

    this.face = face;
    this.keyChain = keyChain;
    this.syncPrefix = syncPrefix;
    
    this.objects = {};
    this.hostedObjects = {};

    this.memoryContentCache = new MemoryContentCache(this.face);
    this.certificateName = new Name(certificateName);

    this.currentDigest = initialDigest;
    this.syncDataFreshnessPeriod = syncDataFreshnessPeriod;
    this.initialDigest = initialDigest;
    this.syncInterestLifetime = syncInterestLifetime;

    this.syncInterestMinInterval = syncInterestMinInterval;
    this.timeoutCntThreshold = timeoutCntThreshold;
    this.entityDataFreshnessPeriod = entityDataFreshnessPeriod;

    this.observer = observer;
    this.serializer = serializer;

    return;
}

//Public facing interface
SyncBasedDiscovery.prototype.start = function ()
{
    this.updateDigest();
    var interest = new Interest((new Name(this.syncPrefix)).append(this.currentDigest));

    interest.setMustBeFresh(true);
    interest.setInterestLifetimeMilliseconds(this.syncInterestLifetime);
    this.face.expressInterest(interest, this.onSyncData.bind(this), this.onSyncTimeout.bind(this));

    console.log("Express interest: " + interest.getName().toUri());
    return;
}

SyncBasedDiscovery.prototype.stop = function ()
{
    this.memoryContentCache.unregisterAll();
    return;
}


SyncBasedDiscovery.prototype.getHostedObjects = function ()
{
    return this.hostedObjects;
}

SyncBasedDiscovery.prototype.getObjects = function ()
{
    return this.objects;
}

SyncBasedDiscovery.prototype.addHostedObject = function
  (name, entityInfo)
{
    // If this is the first object we host, we register for sync namespace: meaning a participant not hosting anything 
    //   is only "listening" for sync, and will not help in the sync process
    if (Object.keys(this.hostedObjects).length == 0) {
        this.memoryContentCache.registerPrefix(this.syncPrefix, this.onRegisterFailed.bind(this), this.onSyncInterest.bind(this));
    }
    if (this.addObject(name)) {
        this.hostedObjects[name] = entityInfo;
        // TODO: debug this, seems to not working as intended yet
        this.contentCacheAddEntityData(name, entityInfo);
        this.memoryContentCache.registerPrefix(new Name(name), this.onRegisterFailed.bind(this), this.onEntityDataNotFound.bind(this));
    } else {
        console.log("Item with this name already added");
    }

    return;
}

SyncBasedDiscovery.prototype.removeHostedObject = function (name)
{
    if (name in this.hostedObjects) {
        delete this.hostedObjects[name];
        if (Object.keys(this.hostedObjects).length == 0) {
            self._memoryContentCache.unregisterAll();
        }
        if (this.removeObject(name)) {
            return true;
        } else {
            console.log("Hosted item not in objects list");
            return false;
        }
    } else {
        return false;
    }
}

//Internal functions
SyncBasedDiscovery.prototype.contentCacheAddEntityData = function
  (name, entityInfo)
{
    var content = this.serializer.serialize(entityInfo);
    var data = new Data(new Name(name));

    data.setContent(content);
    // Interest issuer should not ask for mustBeFresh in this case, for now
    data.getMetaInfo().setFreshnessPeriod(this.entityDataFreshnessPeriod);

    var self = this;
    this.keyChain.sign(data, this.certificateName, function() {
        self.memoryContentCache.add(data);
    });
}

SyncBasedDiscovery.prototype.contentCacheAddSyncData = function
  (dataName)
{
    var keys = Object.keys(this.objects);
    keys.sort();

    var content = "";
    for (var item in keys) {
        content += keys[item] + "\n";
    }
    content.trim();

    var data = new Data(new Name(dataName));
        
    data.setContent(content);
    data.getMetaInfo().setFreshnessPeriod(this.syncDataFreshnessPeriod);
    
    var self = this;
    this.keyChain.sign(data, this.certificateNamem, function() {
        // adding this data to memoryContentCache should satisfy the pending interest
        self.memoryContentCache.add(data);
    });
}
        

SyncBasedDiscovery.prototype.onSyncInterest = function
  (prefix, interest, face, interestFilterId, filter)
{
    if (interest.getName().size() !== this.syncPrefix.size() + 1) {
        // Not an interest for us
        return;
    }
            
    var digest = interest.getName().get(-1).toEscapedString();
    this.updateDigest();
    if (digest != this.currentDigest) {
        // Wait a random period before replying; rationale being that "we are always doing ChronoSync recovery...this is the recovery timer but randomized"
        // Consider this statement: we are always doing ChronoSync recovery
        // TODO: this has the problem of potentially answering with wrong data, there will be more interest exchanges needed for the lifetime duration of one wrong answer
        // Consider appending "answerer" as the last component of data name?
        // TODO2: don't see why we should wait here

        this.replySyncInterest(interest, digest);
        //dummyInterest = Interest(Name("/local/timeout1"))
        //dummyInterest.setInterestLifetimeMilliseconds(random.randint(self._minResponseWaitPeriod, self._maxResponseWaitPeriod))
        //self._face.expressInterest(dummyInterest, self.onDummyData, lambda a : self.replySyncInterest(a, digest))
    }

    return;
}
        

SyncBasedDiscovery.prototype.replySyncInterest = function
  (interest, receivedDigest)
{
    this.updateDigest();
    if (receivedDigest != self._currentDigest) {
        // TODO: one participant may be answering with wrong info: scenario: 1 has {a}, 2 has {b}
        // 2 gets 1's {a} and asks again before 1 gets 2's {b}, 2 asks 1 with the digest of {a, b}, 1 will 
        // create a data with the content {a} for the digest of {a, b}, and this data will be able to answer
        // later steady state interests from 2 until it expires (and by which time 1 should be updated with
        // {a, b} as well)
        this.contentCacheAddSyncData(new Name(this.syncPrefix).append(receivedDigest));
    }
    return;
}

SyncBasedDiscovery.prototype.onSyncData = function
  (interest, data)
{
    //TODO: do verification first
    console.log("Got sync data; name: " + data.getName().toUri() + "; content: " + data.getContent().buf());
    var content = String(data.getContent().buf()).split('\n');

    for (var itemName in content) {
        if (!(content[itemName] in this.objects)) {
            if (content[itemName] != "") {
                this.onReceivedSyncData(content[itemName]);
            }
        }
    }
    
    // Hack for re-expressing sync interest after a short interval
    var dummyInterest = new Interest(new Name("/local/timeout"));
    dummyInterest.setInterestLifetimeMilliseconds(this.syncInterestLifetime);
    this.face.expressInterest(dummyInterest, this.onDummyData.bind(this), this.expressSyncInterest.bind(this));
    return;
}

SyncBasedDiscovery.prototype.onSyncTimeout = function
  (interest)
{
    console.log("Sync interest times out: " + interest.getName().toUri());
    var newInterest = new Interest(new Name(this.syncPrefix).append(this.currentDigest));
    newInterest.setInterestLifetimeMilliseconds(this.syncInterestLifetime);
    newInterest.setMustBeFresh(true);
    this.face.expressInterest(newInterest, this.onSyncData.bind(this), this.onSyncTimeout.bind(this));
    console.log("Express interest: " + newInterest.getName().toUri());

    return;
}

// Handling received sync data: express entity interest
SyncBasedDiscovery.prototype.onReceivedSyncData = function 
  (itemName)
{
    console.log("Received itemName: " + itemName);
    var interest = new Interest(new Name(itemName));
    interest.setInterestLifetimeMilliseconds(4000);
    interest.setMustBeFresh(false);
    this.face.expressInterest(interest, this.onEntityData.bind(this), this.onEntityTimeout.bind(this));
    
    return;
}

SyncBasedDiscovery.prototype.onEntityTimeout = function 
  (interest)
{
    console.log("Item interest times out: " + interest.getName().toUri());
    return;
}

SyncBasedDiscovery.prototype.onEntityData = function 
  (interest, data)
{
    var self = this;
    console.log("Got data: " + data.getName().toUri());
    this.addObject(interest.getName().toUri());
    console.log("Added device: " + interest.getName().toUri());

    var dummyInterest = new Interest(new Name("/local/timeout"));
    dummyInterest.setInterestLifetimeMilliseconds(4000);
    this.face.expressInterest(dummyInterest, this.onDummyData.bind(this), function (a) {
        self.expressHeartbeatInterest(a, interest)
    });
    return;
}

SyncBasedDiscovery.prototype.expressHeartbeatInterest = function
  (dummyInterest, entityInterest)
{
    var newInterest = new Interest(entityInterest);
    newInterest.refreshNonce();

    this.face.expressInterest(entityInterest, this.onHeartbeatData.bind(this), this.onHeartbeatTimeout.bind(this)); 
}

SyncBasedDiscovery.prototype.onHeartbeatData = function
  (interest, data)
{
    var self = this;
    this.resetTimeoutCnt(interest.getName().toUri());
    var dummyInterest = new Interest(new Name("/local/timeout"));
    dummyInterest.setInterestLifetimeMilliseconds(4000);
    this.face.expressInterest(dummyInterest, this.onDummyData.bind(this), function (a) {
        self.expressHeartbeatInterest(a, interest);
    });
}

SyncBasedDiscovery.prototype.onHeartbeatTimeout = function 
  (interest)
{
    if (this.incrementTimeoutCnt(interest.getName().toUri())) {
        console.log("Remove: " + interest.getName().toUri() + " because of consecutive timeout cnt exceeded");
    } else {
        var newInterest = new Interest(interest.getName());
        console.log("Express interest: " + newInterest.getName().toUri());
        newInterest.setInterestLifetimeMilliseconds(4000);
        this.face.expressInterest(newInterest, this.onHeartbeatData.bind(this), this.onHeartbeatTimeout.bind(this));
    }
}

SyncBasedDiscovery.prototype.onDummyData = function
  (interest, data)
{
    console.log("Unexpected reply to dummy interest: " + data.getContent().buf());
    return;
}

SyncBasedDiscovery.prototype.expressSyncInterest = function
  (interest)
{
    var newInterest = new Interest(new Name(this.syncPrefix).append(this.currentDigest));
    newInterest.setInterestLifetimeMilliseconds(this.syncInterestLifetime);
    newInterest.setMustBeFresh(true);
    this.face.expressInterest(newInterest, this.onSyncData.bind(this), this.onSyncTimeout.bind(this));
    console.log("Dummy timeout; Express interest: " + newInterest.getName().toUri());
    return;
}

SyncBasedDiscovery.prototype.addObject = function 
  (name)
{
    if (name in this.objects) {
        return false;
    } else {
        this.objects[name] = {"timeout_count": 0};
        this.notifyObserver(name, "ADD", "");
        this.contentCacheAddSyncData(new Name(this.syncPrefix).append(this.currentDigest));
        this.updateDigest();
        return true;
    }
}

SyncBasedDiscovery.prototype.removeObject = function
  (name)
{
    if (name in this.objects) {
        delete self._objects[name]
        
        this.notifyObserver(name, "REMOVE", "");
        this.contentCacheAddSyncData(new Name(this.syncPrefix).append(this.currentDigest));
        this.updateDigest();
        return true;
    } else {
        return false;
    }
}

SyncBasedDiscovery.prototype.updateDigest = function ()
{
    // TODO: for now, may change the format of the list encoding for easier cross language compatibility
    var keys = Object.keys(this.objects);
    keys.sort();

    if (keys.length > 0) {
        var m = Crypto.createHash('sha256');
        for (var i = 0; i < keys.length; i++) {
            m.md.updateString(keys[i]);
        }
        this.currentDigest = m.digest('hex');
    } else {
        this.currentDigest = this.initialDigest;
    }
    return;
}
        
SyncBasedDiscovery.prototype.incrementTimeoutCnt = function 
  (name)
{
    if (name in this.objects) {
        this.objects[name]["timeout_count"] += 1;
        if (this.objects[name]["timeout_count"] >= this.timeoutCntThreshold) {
            return this.removeObject(name);
        } else {
            return false;
        }
    } else {
        return false;
    }
}  

SyncBasedDiscovery.prototype.resetTimeoutCnt = function
  (name)
{
    if (name in this.objects) {
        this.objects[name]["timeout_count"] = 0;
        return true;
    } else {
        return false;
    }
}

SyncBasedDiscovery.prototype.notifyObserver = function 
  (name, msgType, msg)
{
    this.observer.onStateChanged(name, msgType, msg);
    return;
}

SyncBasedDiscovery.prototype.onRegisterFailed = function
  (prefix)
{
    console.log("Prefix registration failed: " + prefix.toUri());
    return;
}

SyncBasedDiscovery.prototype.onEntityDataNotFound = function 
  (prefix, interest, face, interestFilterId, filter)
{
    var name = interest.getName().toUri();
    if (name in this.hostedObjects) {
        var content = this.serializer.serialize(this.hostedObjects[name]);
        var data = new Data(new Name(name));

        data.setContent(content);
        // Interest issuer should not ask for mustBeFresh in this case, for now
        data.getMetaInfo().setFreshnessPeriod(this.entityDataFreshnessPeriod);

        var self = this;
        this.keyChain.sign(data, this.certificateName, function() {
            self.memoryContentCache.add(data);
        });
    }
    
    return;
}
