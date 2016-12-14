import time
import sys
import logging
import random
import hashlib

from pyndn import Name, Data, Interest, Face
from pyndn.security import KeyChain
from pyndn.util import MemoryContentCache

try:
    import asyncio
except ImportError:
    import trollius as asyncio

# Generic sync-based discovery implementation
class SyncBasedDiscovery(object):
    """
    Sync (digest exchange) based name discovery.
    Discovery maintains a list of discovered, and a list of hosted objects.
    Calls observer.onStateChanged(name, msgType, msg) when an entity is discovered or removed.
    Uses serializer.serialize(entityObject) to serialize a hosted entity's entityInfo into string.

    :param face:
    :type face: Face
    :param keyChain: 
    :type keyChain: KeyChain
    :param certificateName:
    :type certificateName: Name
    :param syncPrefix:
    :type syncPrefix: Name
    :param observer:
    :type observer: ExternalObserver
    :param serializer:
    :type serializer: EntitySerializer
    """
    def __init__(self, face, keyChain, certificateName, syncPrefix, observer, serializer, 
      syncDataFreshnessPeriod = 4000, initialDigest = "00", syncInterestLifetime = 4000, syncInterestMinInterval = 500,
      timeoutCntThreshold = 3, maxResponseWaitPeriod = 2000, minResponseWaitPeriod = 400, entityDataFreshnessPeriod = 10000):
        self._face = face
        self._keyChain = keyChain
        self._syncPrefix = Name(syncPrefix)

        self._objects = dict()
        self._hostedObjects = dict()

        self._memoryContentCache = MemoryContentCache(self._face)
        self._certificateName = Name(certificateName)

        self._currentDigest = initialDigest
        self._syncDataFreshnessPeriod = syncDataFreshnessPeriod
        self._initialDigest = initialDigest
        self._syncInterestLifetime = syncInterestLifetime

        self._syncInterestMinInterval = syncInterestMinInterval
        self._timeoutCntThreshold = timeoutCntThreshold
        self._entityDataFreshnessPeriod = entityDataFreshnessPeriod
        # TODO: policy manager etc setup

        #self._maxResponseWaitPeriod = maxResponseWaitPeriod
        #self._minResponseWaitPeriod = minResponseWaitPeriod

        self._observer = observer
        self._serializer = serializer
        self._numOutstandingInterest = 0

        return

    """
    Public facing interface
    """
    def start(self):
        """
        Starts the discovery
        """
        interest = Interest(Name(self._syncPrefix).append(self._initialDigest))
        interest.setMustBeFresh(True)
        interest.setInterestLifetimeMilliseconds(self._syncInterestLifetime)
        self._face.expressInterest(interest, self.onSyncData, self.onSyncTimeout)
        self._numOutstandingInterest += 1
        if __debug__:
            print("Express interest: " + interest.getName().toUri())
        return

    def stop(self):
        """
        Stops the discovery
        """
        # TODO: interest expression and data response flag
        self._memoryContentCache.unregisterAll()
        return

    def getHostedObjects(self):
        return self._hostedObjects

    def getObjects(self):
        return self._objects

    def publishObject(self, name, entityInfo):
        """
        Adds another object and registers prefix for that object's name

        :param name: the object's name string
        :type name: str
        :param entityInfo: the application given entity info to describe this object name with
        :type entityInfo: EntityInfo
        """
        # If this is the first object we host, we register for sync namespace: meaning a participant not hosting anything 
        #   is only "listening" for sync, and will not help in the sync process
        if len(self._hostedObjects.keys()) == 0:
            self._memoryContentCache.registerPrefix(self._syncPrefix, self.onRegisterFailed, self.onSyncInterest)
        if self.addObject(name, False):
            # Do not add itself to contentCache if its currentDigest is "00".
            if self._currentDigest != self._initialDigest:
                self.contentCacheAddSyncData(Name(self._syncPrefix).append(self._currentDigest))
            
            self.updateDigest()
            
            interest = Interest(Name(self._syncPrefix).append(self._currentDigest))
            interest.setInterestLifetimeMilliseconds(self._syncInterestLifetime)
            interest.setMustBeFresh(True)
            
            self._face.expressInterest(interest, self.onSyncData, self.onSyncTimeout)
            self._numOutstandingInterest += 1

            self._hostedObjects[name] = entityInfo
            self.contentCacheAddEntityData(name, entityInfo)
            self.notifyObserver(name, "ADD", "")
            # TODO: should the user configure this prefix as well?
            self._memoryContentCache.registerPrefix(Name(name), self.onRegisterFailed, self.onEntityDataNotFound)
        else:
            if __debug__:
                print("Item with this name already added")
        return

    def removeHostedObject(self, name):
        """
        Removes a locally hosted object

        :param name: the object's name string
        :type name: str
        :return: whether removal's successful or not
        :rtype: bool
        """
        if name in self._hostedObjects:
            del self._hostedObjects[name]
            if len(self._hostedObjects) == 0:
                self._memoryContentCache.unregisterAll()
            if self.removeObject(name):    
                return True
            else:
                if __debug__:
                    print("Hosted item not in objects list")
                return False
        else:
            return False

    """
    Internal functions
    """
    def contentCacheAddEntityData(self, name, entityInfo):
        content = self._serializer.serialize(entityInfo)
        data = Data(Name(name))

        data.setContent(content)

        data.getMetaInfo().setFreshnessPeriod(self._entityDataFreshnessPeriod)
        self._keyChain.sign(data, self._certificateName)
        self._memoryContentCache.add(data)
        print "added entity to cache: " + data.getName().toUri() + "; " + data.getContent().toRawStr()

    def contentCacheAddSyncData(self, dataName):
        sortedKeys = sorted(self._objects.keys())
        content = ""
        for key in sortedKeys:
            content += key + "\n"
        content.strip()

        data = Data(Name(dataName))
        
        data.setContent(content)
        data.getMetaInfo().setFreshnessPeriod(self._syncDataFreshnessPeriod)
        self._keyChain.sign(data, self._certificateName)
        # adding this data to memoryContentCache should satisfy the pending interest
        self._memoryContentCache.add(data)

    def onSyncInterest(self, prefix, interest, face, interestFilterId, filter):
        if interest.getName().size() != self._syncPrefix.size() + 1:
            # Not an interest for us
            return
        digest = interest.getName().get(-1).toEscapedString()
        self.updateDigest()
        if digest != self._currentDigest:
            # Wait a random period before replying; rationale being that "we are always doing ChronoSync recovery...this is the recovery timer but randomized"
            # Consider this statement: we are always doing ChronoSync recovery
            # TODO: this has the problem of potentially answering with wrong data, there will be more interest exchanges needed for the lifetime duration of one wrong answer
            # Consider appending "answerer" as the last component of data name?
            # TODO2: don't see why we should wait here

            self.replySyncInterest(interest, digest)
            #dummyInterest = Interest(Name("/local/timeout1"))
            #dummyInterest.setInterestLifetimeMilliseconds(random.randint(self._minResponseWaitPeriod, self._maxResponseWaitPeriod))
            #self._face.expressInterest(dummyInterest, self.onDummyData, lambda a : self.replySyncInterest(a, digest))
        return

    def replySyncInterest(self, interest, receivedDigest):
        self.updateDigest()
        if receivedDigest != self._currentDigest:
            # TODO: one participant may be answering with wrong info: scenario: 1 has {a}, 2 has {b}
            # 2 gets 1's {a} and asks again before 1 gets 2's {b}, 2 asks 1 with the digest of {a, b}, 1 will 
            # create a data with the content {a} for the digest of {a, b}, and this data will be able to answer
            # later steady state interests from 2 until it expires (and by which time 1 should be updated with
            # {a, b} as well)
            self.contentCacheAddSyncData(Name(self._syncPrefix).append(receivedDigest))
        return

    def onSyncData(self, interest, data):
        # TODO: do verification first
        if __debug__:
            print("Got sync data; name: " + data.getName().toUri() + "; content: " + data.getContent().toRawStr())
        content = data.getContent().toRawStr().split('\n')
        self._numOutstandingInterest -= 1
        for itemName in content:
            if itemName not in self._objects:
                if itemName != "":
                    self.onReceivedSyncData(itemName)

        # Hack for re-expressing sync interest after a short interval
        dummyInterest = Interest(Name("/local/timeout"))
        dummyInterest.setInterestLifetimeMilliseconds(self._syncInterestLifetime)
        self._face.expressInterest(dummyInterest, self.onDummyData, self.onSyncTimeout)
        self._numOutstandingInterest += 1
        return

    def onSyncTimeout(self, interest):
        newInterest = Interest(Name(self._syncPrefix).append(self._currentDigest))
        newInterest.setInterestLifetimeMilliseconds(self._syncInterestLifetime)
        newInterest.setMustBeFresh(True)
        self._numOutstandingInterest -= 1
        print "re-expressing: " + newInterest.getName().toUri()

        if self._numOutstandingInterest <= 0:
            self._face.expressInterest(newInterest, self.onSyncData, self.onSyncTimeout)
            self._numOutstandingInterest += 1
        return

    """
    Handling received sync data: express entity interest
    """
    def onReceivedSyncData(self, itemName):
        interest = Interest(Name(itemName))
        interest.setInterestLifetimeMilliseconds(4000)
        interest.setMustBeFresh(False)
        self._face.expressInterest(interest, self.onEntityData, self.onEntityTimeout) 
        return

    def onEntityTimeout(self, interest):
        print "Item interest times out: " + interest.getName().toUri()
        return

    def onEntityData(self, interest, data):
        self.addObject(interest.getName().toUri(), True)
        self.notifyObserver(interest.getName().toUri(), "ADD", "");

        dummyInterest = Interest(Name("/local/timeout"))
        dummyInterest.setInterestLifetimeMilliseconds(4000)
        self._face.expressInterest(dummyInterest, self.onDummyData, lambda a : self.expressHeartbeatInterest(a, interest))
        return

    def expressHeartbeatInterest(self, dummyInterest, entityInterest):
        newInterest = Interest(entityInterest)
        newInterest.refreshNonce()
        self._face.expressInterest(newInterest, self.onHeartbeatData, self.onHeartbeatTimeout)

    def onHeartbeatData(self, interest, data):
        self.resetTimeoutCnt(interest.getName().toUri())
        dummyInterest = Interest(Name("/local/timeout"))
        dummyInterest.setInterestLifetimeMilliseconds(4000)
        self._face.expressInterest(dummyInterest, self.onDummyData, lambda a : self.expressHeartbeatInterest(a, interest))

    def onHeartbeatTimeout(self, interest):
        if self.incrementTimeoutCnt(interest.getName().toUri()):
            print "Remove: " + interest.getName().toUri() + " because of consecutive timeout cnt exceeded"
        else:
            newInterest = Interest(interest.getName())
            newInterest.setInterestLifetimeMilliseconds(4000)
            self._face.expressInterest(newInterest, self.onHeartbeatData, self.onHeartbeatTimeout)

    def onDummyData(self, interest, data):
        if __debug__:
            print("Unexpected reply to dummy interest: " + data.getContent().toRawStr())
        return

    def expressSyncInterest(self, interest):
        self.onSyncTimeout(interest)
        return

    def addObject(self, name, update):
        if name in self._objects:
            return False
        else:
            self._objects[name] = {"timeout_count": 0}
            if update:
                self.updateDigest()
            return True

    def removeObject(self, name):
        if name in self._objects:
            del self._objects[name]
            self.notifyObserver(name, "REMOVE", "")
            self.contentCacheAddSyncData(Name(self._syncPrefix).append(self._currentDigest))
            self.updateDigest()
            return True
        else:
            return False

    def updateDigest(self):
        # TODO: for now, may change the format of the list encoding for easier cross language compatibility
        if len(self._objects) > 0:
            m = hashlib.sha256()
            for item in sorted(self._objects.keys()):
                m.update(item)
            self._currentDigest = str(m.hexdigest())
        else:
            self._currentDigest = self._initialDigest
        return

    def incrementTimeoutCnt(self, name):
        if name in self._objects:
            self._objects[name]["timeout_count"] += 1
            if self._objects[name]["timeout_count"] >= self._timeoutCntThreshold:
                return self.removeObject(name)
            else:
                return False
        else:
            return False

    def resetTimeoutCnt(self, name):
        if name in self._objects:
            self._objects[name]["timeout_count"] = 0
            return True
        else:
            return False

    def notifyObserver(self, name, msgType, msg):
        self._observer.onStateChanged(name, msgType, msg)
        return

    def onRegisterFailed(self, prefix):
        if __debug__:
            print("Prefix registration failed: " + prefix.toUri())
        return

    def onEntityDataNotFound(self, prefix, interest, face, interestFilterId, filter):
        name = interest.getName().toUri()
        if name in self._hostedObjects:
            content = self._serializer.serialize(self._hostedObjects[name])
            data = Data(Name(name))
            data.setContent(content)
            data.getMetaInfo().setFreshnessPeriod(self._entityDataFreshnessPeriod)
            self._keyChain.sign(data, self._certificateName)
            self._face.putData(data)
        return
