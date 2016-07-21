#!/usr/bin/python

from pyndn import Name, Face, Interest, Data
from pyndn.util.memory_content_cache import MemoryContentCache
from pyndn.transport.udp_transport import UdpTransport

import time
import sys
import psutil as ps
import json
from ndn_pi.iot_node import IotNode

from pir import Pir

import logging
class OpenloopPublisher(IotNode):
    def __init__(self, transport, conn, pir = None):
        super(OpenloopPublisher, self).__init__(transport, conn)
        self._missedRequests = 0
        self._dataPrefix = None
        self.addCommand(Name('listPrefixes'), self.listDataPrefixes, ['repo'],
            False)
        self._count = 0
        self._pir = pir

    def setupComplete(self):
        # The cache will clear old values every 100s
        #self._dataCache = MemoryContentCache(self.face, 100000)
        self._dataPrefix = Name(self.prefix).append('data')
        #self.registerCachePrefix()
        print "Serving data at {}".format(self._dataPrefix.toUri())
        self.loop.call_soon(self.publishData)

    def listDataPrefixes(self, interest):
        d = Data(interest.getName())
        if self._dataPrefix is not None:
            d.setContent(json.dumps([self._dataPrefix.toUri()]))
        d.getMetaInfo().setFreshnessPeriod(10000)
        return d

    #def registerCachePrefix(self):
    #    self._dataCache.registerPrefix(self._dataPrefix, self.cacheRegisterFail , self.onDataMissing)

    def unknownCommandResponse(self, interest):
        # we override this so the MemoryContentCache can handle data requests
        afterPrefix = interest.getName().get(self.prefix.size()).toEscapedString()
        if afterPrefix == 'data':
            return None
        else:
            return super(OpenloopPublisher, self).unknownCommandResponse(interest)

    #def cacheRegisterFail(self, interest):
        # just try again
    #    self.log.warn('Could not register data cache')
    #    self.registerCachePrefix()

    def onDataMissing(self, prefix, interest, transport, prefixId):
        self._missedRequests += 1
        # let it timeout

    def publishData(self):
        timestamp = time.time()
        info = {''}
        if self._pir == None:
            cpu_use = ps.cpu_percent()
            users = [u.name for u in ps.users()]
            nProcesses = len(ps.pids())
            memUse = ps.virtual_memory().percent
            swapUse = ps.swap_memory().percent

            info = {'count': self._count, 'cpu_usage':cpu_use, 'users':users, 'processes':nProcesses,
                    'memory_usage':memUse, 'swap_usage':swapUse}
        else:
            info = {'count': self._count, 'pir_bool': self._pir.read()}
        self._count += 1
        
        dataOut = Data(Name(self._dataPrefix).appendVersion(int(timestamp)))
        dataOut.setContent(json.dumps(info))
        dataOut.getMetaInfo().setFreshnessPeriod(10000)
        self.signData(dataOut)

        #self._dataCache.add(dataOut)
        # instead of adding data to content cache, we put data to nfd anyway
        self.send(dataOut.wireEncode().buf())
        print('data name: ' + dataOut.getName().toUri() + '; content: ' + str(info))

        # repeat every 1 seconds
        self.loop.call_later(1, self.publishData)

    def send(self, encoding):
        # Interesting, yet I thought self.face was a threadsafe face.
        self.face._node._transport.send(encoding)
        #self.face._loop.call_soon_threadsafe(
        #    super(ThreadsafeFace, self.face)._node._transport.send, encoding)

if __name__ == '__main__':
    # We take pin 25 as default
    pir = Pir(25)
    
    transport = UdpTransport()
    # TODO: This multicast address should read from nfd configuration
    conn = UdpTransport.ConnectionInfo("224.0.23.170")
    #conn = UdpTransport.ConnectionInfo("192.168.10.161")
    n = OpenloopPublisher(transport, conn, pir)
    n.start()
