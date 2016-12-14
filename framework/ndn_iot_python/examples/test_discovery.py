import time
import sys
import logging
import random
import json
import hashlib

from pyndn import Name, Data, Interest, Face
from pyndn.security import KeyChain
from pyndn.util import MemoryContentCache

from ndn_iot_python.discovery.sync_based_discovery import SyncBasedDiscovery        
from ndn_iot_python.discovery.entity_info import EntityInfo
from ndn_iot_python.discovery.entity_serializer import EntitySerializer
from ndn_iot_python.discovery.external_observer import ExternalObserver

class MyEntityInfo(EntityInfo):
    def __init__(self, content):
        self._content = content

    def getContent(self):
        return self._content

class MySerializer(EntitySerializer):
    def serialize(self, entityInfo):
        return entityInfo.getContent()

class MyObserver(ExternalObserver):
    def onStateChanged(self, name, msgType, msg):
        print "Got message: " + str(name) + " : " + str(msgType) + " : " + str(msg)

class DiscoveryTest(object):
    def __init__(self, face, keyChain, certificateName):
        self._face = face
        self._keyChain = keyChain
        self._certificateName = certificateName

        self._serializer = MySerializer()
        self._syncPrefix = Name("/home/discovery")
        self._observer = MyObserver()

        self._discovery = SyncBasedDiscovery(face, keyChain, certificateName, self._syncPrefix, self._observer, self._serializer)
        return

    def start(self):
        testObjectName = Name("home").append("python-publisher-" + str(random.randint(1, 100)))
        self._discovery.publishEntity(testObjectName.toUri(), MyEntityInfo("good"))
        self._discovery.start()

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
