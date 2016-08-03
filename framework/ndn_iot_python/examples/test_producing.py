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

class AppProducer():
    def __init__(self, face, certificateName, keyChain, dataPrefix):
        self._keyChain = keyChain
        self._certificateName = certificateName
        self._face = face
        self._dataPrefix = dataPrefix
        return

    def start(self):
        self._dataCache = MemoryContentCache(self._face, 100000)
        self.registerCachePrefix()
        print "Serving data at {}".format(self._dataPrefix.toUri())
        self._face.callLater(5000, self.publishData)
        return

    def registerCachePrefix(self):
        self._dataCache.registerPrefix(self._dataPrefix, self.cacheRegisterFail, self.onDataMissing)

    def cacheRegisterFail(self, interest):
        # just try again
        self.log.warn('Could not register data cache')
        self.registerCachePrefix()

    def onDataMissing(self, prefix, interest, face, interestFilterId, filter):
        print "data not found for " + interest.getName().toUri()
        # let it timeout

    def publishData(self):
        timestamp = time.time() 
        cpu_use = ps.cpu_percent()
        users = [u.name for u in ps.users()]
        nProcesses = len(ps.pids())
        memUse = ps.virtual_memory().percent
        swapUse = ps.swap_memory().percent

        info = {'cpu_usage':cpu_use, 'users':users, 'processes':nProcesses,
                 'memory_usage':memUse, 'swap_usage':swapUse}
    
        dataOut = Data(Name(self._dataPrefix).appendVersion(int(timestamp)))
        dataOut.setContent(json.dumps(info))
        dataOut.getMetaInfo().setFreshnessPeriod(10000)
        self._keyChain.sign(dataOut, self._certificateName)

        self._dataCache.add(dataOut)
        print "data added: " + dataOut.getName().toUri()

        # repeat every 5 seconds
        self._face.callLater(5000, self.publishData)

if __name__ == '__main__':
    try:
        import psutil as ps
    except Exception as e:
        print str(e)

    loop = asyncio.get_event_loop()
    face = ThreadsafeFace(loop)
    
    bootstrap = Bootstrap(face)
    appName = "flow"
    dataPrefix = Name("/home/flow/ps-publisher-4")

    def onSetupComplete(defaultCertificateName, keyChain):
        def onRequestSuccess():
            print "data production authorized by controller"
            producer = AppProducer(face, defaultCertificateName, keyChain, dataPrefix)
            producer.start()
            return

        def onRequestFailed(msg):
            print "data production not authorized by controller : " + msg
            # For this test, we start anyway
            producer = AppProducer(face, defaultCertificateName, keyChain, dataPrefix)
            producer.start()
            return

        bootstrap.requestProducerAuthorization(dataPrefix, appName, onRequestSuccess, onRequestFailed)
        
    def onSetupFailed(msg):
        print("Setup failed " + msg)

    bootstrap.setupDefaultIdentityAndRoot("app.conf", onSetupComplete = onSetupComplete, onSetupFailed = onSetupFailed)

    loop.run_forever()