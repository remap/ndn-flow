import time
import sys
import logging
import random
import json
import hashlib

from pyndn import Name, Data, Interest, Face
from pyndn.security import KeyChain
from pyndn.util import MemoryContentCache

try:
    import asyncio
except ImportError:
    import trollius as asyncio

# Generic sync-based discovery implementation
class Discovery(object):
    def __init__(self, face, keyChain, certificateName, syncPrefix, onReceivedSyncData, 
      syncDataFreshnessPeriod = 4000, initialDigest = "00", syncInterestLifetime = 4000, syncInterestMinInterval = 500,
      timeoutCntThreshold = 3, maxResponseWaitPeriod = 2000, minResponseWaitPeriod = 400):
        self._face = face
        self._keyChain = keyChain
        self._syncPrefix = Name(syncPrefix)
        self._onReceivedSyncData = onReceivedSyncData

        self._objects = dict()
        self._hostedObjects = []

        self._memoryContentCache = MemoryContentCache(self._face)
        self._certificateName = Name(certificateName)

        self._currentDigest = initialDigest
        self._syncDataFreshnessPeriod = syncDataFreshnessPeriod
        self._initialDigest = initialDigest
        self._syncInterestLifetime = syncInterestLifetime

        self._syncInterestMinInterval = syncInterestMinInterval
        self._timeoutCntThreshold = timeoutCntThreshold
        # TODO: policy manager etc setup

        self._maxResponseWaitPeriod = maxResponseWaitPeriod
        self._minResponseWaitPeriod = minResponseWaitPeriod
        return

    def start(self):
        self.updateDigest()
        interest = Interest(Name(self._syncPrefix).append(self._currentDigest))
        interest.setMustBeFresh(True)
        interest.setInterestLifetimeMilliseconds(self._syncInterestLifetime)
        self._face.expressInterest(interest, self.onSyncData, self.onSyncTimeout)
        if __debug__:
            print("Express interest: " + interest.getName().toUri())
        return

    def stop(self):
        self._memoryContentCache.unregisterAll()
        return

    def contentCacheAdd(self, dataName):
        content = json.dumps(sorted(self._objects.keys()))
        data = Data(Name(dataName))
        
        data.setContent(content)
        data.getMetaInfo().setFreshnessPeriod(self._syncDataFreshnessPeriod)
        self._keyChain.sign(data, self._certificateName)
        # adding this data to memoryContentCache should satisfy the pending interest
        self._memoryContentCache.add(data)
        if __debug__:
            print("* added data: " + data.getName().toUri() + "; content: " + content)

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
            dummyInterest = Interest(Name("/local/timeout1"))
            dummyInterest.setInterestLifetimeMilliseconds(random.randint(self._minResponseWaitPeriod, self._maxResponseWaitPeriod))
            self._face.expressInterest(dummyInterest, self.onDummyData, lambda a : self.replySyncInterest(a, digest))
        return

    def replySyncInterest(self, interest, receivedDigest):
        self.updateDigest()
        if receivedDigest != self._currentDigest:
            self.contentCacheAdd(Name(self._syncPrefix).append(receivedDigest))
        return

    def onSyncData(self, interest, data):
        # TODO: do verification first
        if __debug__:
            print("Got sync data; name: " + data.getName().toUri() + "; content: " + data.getContent().toRawStr())
        content = json.loads(data.getContent().toRawStr())
        for itemName in content:
            if itemName not in self._objects:
                if self._onReceivedSyncData:
                    self._onReceivedSyncData(itemName)

        # Hack for re-expressing sync interest
        dummyInterest = Interest(Name("/local/timeout"))
        dummyInterest.setInterestLifetimeMilliseconds(self._syncInterestMinInterval)
        self._face.expressInterest(dummyInterest, self.onDummyData, self.onDummyTimeout)
        return

    def onDummyData(self, interest, data):
        if __debug__:
            print("Unexpected reply to dummy interest: " + data.getContent().toRawStr())
        return

    def onDummyTimeout(self, interest):
        newInterest = Interest(Name(self._syncPrefix).append(self._currentDigest))
        newInterest.setInterestLifetimeMilliseconds(self._syncInterestLifetime)
        newInterest.setMustBeFresh(True)
        self._face.expressInterest(newInterest, self.onSyncData, self.onSyncTimeout)
        if __debug__:
            print("Dummy timeout; Express interest: " + newInterest.getName().toUri())
        return

    def onSyncTimeout(self, interest):
        if __debug__:
            print("Sync interest times out: " + interest.getName().toUri())
        newInterest = Interest(Name(self._syncPrefix).append(self._currentDigest))
        newInterest.setInterestLifetimeMilliseconds(self._syncInterestLifetime)
        newInterest.setMustBeFresh(True)
        self._face.expressInterest(newInterest, self.onSyncData, self.onSyncTimeout)
        if __debug__:
            print("Express interest: " + newInterest.getName().toUri())
        return

    def addHostedObject(self, name):
        # If this is the first object we host, we register for sync namespace: meaning a participant not hosting anything 
        #   is only "listening" for sync, and will not help in the sync process
        if len(self._hostedObjects) == 0:
            self._memoryContentCache.registerPrefix(self._syncPrefix, self.onRegisterFailed, self.onSyncInterest)
        if self.addObject(name):
            self._hostedObjects.append(name)
        else:
            if __debug__:
                print("Item with this name already added")
        return

    def removeHostedObject(self, name):
        if name in self._hostedObjects:
            self._hostedObjects.remove(name)
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

    def addObject(self, name):
        if name in self._objects:
            return False
        else:
            self._objects[name] = {"timeout_count": 0}
            self.contentCacheAdd(Name(self._syncPrefix).append(self._currentDigest))
            self.updateDigest()
            return True

    def removeObject(self, name):
        if name in self._objects:
            del self._objects[name]
            self.contentCacheAdd(Name(self._syncPrefix).append(self._currentDigest))
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

    def onRegisterFailed(self, prefix):
        if __debug__:
            print("Prefix registration failed: " + prefix.toUri())
        return
