#!/usr/bin/python

#
# This is the data publisher using MemoryContentCache
#

from pyndn import Name, Interest, Data
from pyndn.util.memory_content_cache import MemoryContentCache
from pyndn.security import KeyChain, SecurityException
from pyndn.security.identity import IdentityManager, FilePrivateKeyStorage, BasicIdentityStorage
from pyndn.security.policy import NoVerifyPolicyManager
from pyndn.threadsafe_face import ThreadsafeFace

import time
import sys
import json

import logging

try:
    import asyncio
except ImportError:
    import trollius as asyncio

class FlowPublisher(object):
    def __init__(self, face, keyChain, loop, confFile = "flow.conf"):
        self._keyChain = keyChain
        self._defaultIdentity = None
        self._dataPrefix = None
        self._loop = loop

        self._face = face

        self.processConfiguration(confFile)
        self._defaultCertificateName = self._keyChain.getIdentityManager().getDefaultCertificateNameForIdentity(self._defaultIdentity)

        self._face.setCommandSigningInfo(self._keyChain, self._defaultCertificateName)
        print "Using default certificate name: " + self._defaultCertificateName.toUri()

    def processConfiguration(self, confFile):
        with open(confFile, "r") as f:
            conf = f.read()
            confObj = json.loads(conf)
            if "identity" in confObj:
                if confObj["identity"] == "default":
                    self._defaultIdentity = self._keyChain.getDefaultIdentity()
                else:
                    defaultIdName = Name(confObj["identity"])
                    try:
                        defaultKey = self._keyChain.getIdentityManager().getDefaultKeyNameForIdentity(defaultIdName)
                        self._defaultIdentity = defaultIdName
                    except SecurityException:
                        print "Identity " + defaultIdName.toUri() + " in configuration file " + confFile + " does not exist. Please configure the device with this identity first"
            if "prefix" in confObj:
                self._dataPrefix = Name(confObj["prefix"])
            else:
                print "Configuration file " + confFile + " is missing application data prefix"
        return
    
    def start(self):
        self._dataCache = MemoryContentCache(self._face, 100000)
        self.registerCachePrefix()
        print "Serving data at {}".format(self._dataPrefix.toUri())
        self._loop.call_soon(self.publishData)
        return

    def registerCachePrefix(self):
        self._dataCache.registerPrefix(self._dataPrefix, self.cacheRegisterFail, self.onDataMissing)

    def cacheRegisterFail(self, interest):
        # just try again
        self.log.warn('Could not register data cache')
        self.registerCachePrefix()

    def onDataMissing(self, prefix, interest, transport, prefixId):
        self._missedRequests += 1
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
        self._keyChain.sign(dataOut, self._defaultCertificateName)

        self._dataCache.add(dataOut)

        # repeat every 5 seconds
        self._loop.call_later(5, self.publishData)

if __name__ == '__main__':
    try:
        import psutil as ps
    except Exception as e:
        print str(e)
    identityManager = IdentityManager(BasicIdentityStorage(), FilePrivateKeyStorage())
    
    loop = asyncio.get_event_loop()
    face = ThreadsafeFace(loop)
    keyChain = KeyChain()

    n = FlowPublisher(face, keyChain, loop)
    n.start()

    loop.run_forever()
