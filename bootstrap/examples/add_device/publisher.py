#!/usr/bin/python

#
# This is the data publisher using MemoryContentCache
#

from pyndn import Name, Face, Interest, Data
from pyndn.util.memory_content_cache import MemoryContentCache

import time
import sys
import json
from ndn_pi.iot_node import IotNode

import logging

class CachedContentPublisher(IotNode):
    def __init__(self):
        super(CachedContentPublisher, self).__init__()
        self._missedRequests = 0
        self._dataPrefix = None
        self.addCommand(Name('listPrefixes'), self.listDataPrefixes, ['repo'],
            False)

    def setupComplete(self):
        # The cache will clear old values every 100s
        self._dataCache = MemoryContentCache(self.face, 100000)
        self._dataPrefix = Name(self.prefix).append('data')
        self.registerCachePrefix()
        print "Serving data at {}".format(self._dataPrefix.toUri())
        self.loop.call_soon(self.publishData)

    def listDataPrefixes(self, interest):
        d = Data(interest.getName())
        if self._dataPrefix is not None:
            d.setContent(json.dumps([self._dataPrefix.toUri()]))
        d.getMetaInfo().setFreshnessPeriod(10000)
        return d

    def registerCachePrefix(self):
        self._dataCache.registerPrefix(self._dataPrefix, self.cacheRegisterFail , self.onDataMissing)

    def unknownCommandResponse(self, interest):
        # we override this so the MemoryContentCache can handle data requests
        afterPrefix = interest.getName().get(self.prefix.size()).toEscapedString()
        if afterPrefix == 'data':
            return None
        else:
            return super(CachedContentPublisher, self).unknownCommandResponse(interest)

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
        self.signData(dataOut)

        self._dataCache.add(dataOut)

        # repeat every 5 seconds
        self.loop.call_later(5, self.publishData)

if __name__ == '__main__':
    try:
        import psutil as ps
    except Exception as e:
        print str(e)
    n = CachedContentPublisher()
    n.start()
