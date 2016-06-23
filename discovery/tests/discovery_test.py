import time
import sys
import logging
import random
import json

from pyndn import Name, Data, Interest, Face
from discovery import Discovery

from pyndn.security import KeyChain

from pyndn.security.identity.file_private_key_storage import FilePrivateKeyStorage
from pyndn.security.identity.basic_identity_storage import BasicIdentityStorage
from pyndn.security.identity.identity_manager import IdentityManager
from pyndn.security.policy.config_policy_manager import ConfigPolicyManager

from pyndn.util import MemoryContentCache, Blob

class DiscoveryTest(object):
    def __init__(self, face, keyChain, certificateName):
        self._face = face
        self._keyChain = keyChain
        self._certificateName = certificateName

        self._discovery = Discovery(face, keyChain, keyChain.getDefaultCertificateName(), Name("/my-home/living-room/devices/discovery"), self.onReceivedSyncData)
    
    def start(self):
        testObjectName = Name("/my-home/living-room/devices/").append("pc-" + str(random.randint(1, 100)))
        self._discovery.addHostedObject(testObjectName.toUri())
        print "hosting object " + testObjectName.toUri()

        self._discovery.start()
        self._face.registerPrefix(testObjectName, self.onInterest, self.onRegisterFailed)
        return

    def onInterest(self, prefix, interest, face, interestFilterId, filter):
        data = Data(Name(interest.getName()))
        content = "here!"
        data.setContent(content)
        data.getMetaInfo().setFreshnessPeriod(4000)
        self._face.putData(data)
        return

    def onReceivedSyncData(self, itemName):
        print "Received itemName: " + itemName
        interest = Interest(Name(itemName))
        interest.setInterestLifetimeMilliseconds(4000)
        self._face.expressInterest(interest, self.onData, self.onTimeout)
        return

    def onData(self, interest, data):
        print "Got data: " + data.getName().toUri()
        self._discovery.addObject(interest.getName().toUri())
        # Start heartbeat, TODO: heartbeat mechanism
        print "Added device: " + interest.getName().toUri()

        dummyInterest = Interest(Name("/local/timeout"))
        dummyInterest.setInterestLifetimeMilliseconds(4000)
        self._face.expressInterest(dummyInterest, self.onDummyData, lambda a : self.onDummyTimeout(a, interest))
        return

    def onDummyData(self, interest, data):
        print "Unexpected dummy data: " + data.getName().toUri()
        return

    def onDummyTimeout(self, interest, heartbeatInterest):
        self.expressHeartbeatInterest(heartbeatInterest)
        return

    def expressHeartbeatInterest(self, heartbeatInterest):
        newInterest = Interest(heartbeatInterest.getName())
        newInterest.setInterestLifetimeMilliseconds(4000)
        print "Express interest: " + newInterest.getName().toUri()
        self._face.expressInterest(newInterest, self.onHeartbeatData, self.onHeartbeatTimeout)

    def onHeartbeatData(self, interest, data):
        self._discovery.resetTimeoutCnt(interest.getName().toUri())
        dummyInterest = Interest(Name("/local/timeout"))
        dummyInterest.setInterestLifetimeMilliseconds(4000)
        self._face.expressInterest(dummyInterest, self.onDummyData, lambda a : self.onDummyTimeout(a, interest))

    def onHeartbeatTimeout(self, interest):
        if self._discovery.incrementTimeoutCnt(interest.getName().toUri()):
            print "Remove: " + interest.getName().toUri() + " because of consecutive timeout cnt exceeded"
        else:
            newInterest = Interest(interest.getName())
            print "Express interest: " + newInterest.getName().toUri()
            newInterest.setInterestLifetimeMilliseconds(4000)
            self._face.expressInterest(newInterest, self.onHeartbeatData, self.onHeartbeatTimeout)

    def onTimeout(self, interest):
        print "Item interest times out: " + interest.getName().toUri()
        return

    def onRegisterFailed(self, prefix):
        print "Prefix registration failed: " + prefix.toUri()
        return

def usage():
    print("Usage")
    return

def main():
    face = Face()

    # Use the system default key chain and certificate name to sign commands.
    keyChain = KeyChain()
    certificateName = keyChain.getDefaultCertificateName()
    face.setCommandSigningInfo(keyChain, certificateName)
    test = DiscoveryTest(face, keyChain, certificateName)
    test.start()

    while True:
        face.processEvents()
        # We need to sleep for a few milliseconds so we don't use 100% of the CPU.
        time.sleep(0.01)

    face.shutdown()

main()
