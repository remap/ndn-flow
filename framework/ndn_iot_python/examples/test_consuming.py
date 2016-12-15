#!/usr/bin/python

from pyndn import Name, Interest, Data
from pyndn.util.memory_content_cache import MemoryContentCache
from pyndn.security import KeyChain
from pyndn.threadsafe_face import ThreadsafeFace

from ndn_iot_python.bootstrap.bootstrap import Bootstrap

import time
import sys
import json

import logging

try:
    import asyncio
except ImportError:
    import trollius as asyncio

class AppConsumer():
    def __init__(self, face, certificateName, keyChain, dataPrefix):
        self._keyChain = keyChain
        self._defaultCertificateName = certificateName
        self._face = face
        self._dataPrefix = dataPrefix
        return

    def start(self):
        interest = Interest(self._dataPrefix)
        self._face.expressInterest(interest, self.onData, self.onTimeout)
        print "Interest expressed " + interest.getName().toUri()
        return

    def onData(self, interest, data):
        print "Got data " + data.getName().toUri()
        print "Data keyLocator KeyName " + data.getSignature().getKeyLocator().getKeyName().toUri()
        def onVerified(data):
            print "data verified: " + data.getContent().toRawStr()
            return
        def onVerifyFailed(data, reason):
            print "data verification failed: " + reason
            return
        self._keyChain.verifyData(data, onVerified, onVerifyFailed)
        return

    def onTimeout(self, interest):
        print "Interest times out " + interest.getName().toUri()
        return

if __name__ == '__main__':
    try:
        import psutil as ps
    except Exception as e:
        print str(e)

    loop = asyncio.get_event_loop()
    face = ThreadsafeFace(loop)
    
    bootstrap = Bootstrap(face)

    def onSetupComplete(defaultCertificateName, keyChain):
        def onUpdateFailed(msg):
            print "Trust scheme update failed"
            return
            
        def onUpdateSuccess(trustSchemaString, isInitial):
            print "Got a trust schema"
            if isInitial:
                consumer.start()
            return
        consumer = AppConsumer(face, defaultCertificateName, keyChain, Name("/home/flow/ps-publisher-4"))
        bootstrap.startTrustSchemaUpdate(Name("/home/gateway/flow"), onUpdateSuccess, onUpdateFailed)
        
    def onSetupFailed(msg):
        print("Setup failed " + msg)

    bootstrap.setupDefaultIdentityAndRoot("app.conf", onSetupComplete = onSetupComplete, onSetupFailed = onSetupFailed)

    loop.run_forever()
