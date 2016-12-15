# for ble receive functionalities
import binascii
import time

from pyndn.encoding.element_reader import ElementReader
from btle_node import BtleNode

from bluepy.btle import UUID, Peripheral, DefaultDelegate

# for publishing functionalities
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

    def onBtleData(self, data):
        # expect data format like "0.2,0.1,0.3"
        print "got data: " + data.getName().toUri()
        content = data.getContent().toRawStr()
        pyr = content.split(',')
        if len(pyr) >= 3:
            resultingContent = "{\"p\":" + pyr[0] + ",\"y\":" + pyr[1] + ",\"r\":" + pyr[2] + "}"
            timestamp = time.time()
            dataOut = Data(Name(self._dataPrefix).appendVersion(int(timestamp)))
            dataOut.setContent(resultingContent)
            dataOut.getMetaInfo().setFreshnessPeriod(10000)
            self._keyChain.sign(dataOut, self._certificateName)

            self._dataCache.add(dataOut)
            print "data added: " + dataOut.getName().toUri()

if __name__ == '__main__':
    # TODO: take customized parameters
    service_uuid = UUID(0x2220)
    my_uuid = UUID(0x2221)

    peripheral = None
    loop = asyncio.get_event_loop()
    face = ThreadsafeFace(loop)
    
    bootstrap = Bootstrap(face)
    appName = "flow"
    dataPrefix = Name("/home/flow/gyro-1")

    @asyncio.coroutine
    def btleNotificationListen(peripheral):
        while True:
            if peripheral.waitForNotifications(0.2):
                pass
            time.sleep(0.01)
            yield None
        
    def startProducer(defaultCertificateName, keyChain):
        producer = AppProducer(face, defaultCertificateName, keyChain, dataPrefix)
        producer.start()

        # init btle
        el = BtleNode(producer.onBtleData, None, None)
        em = ElementReader(el)

        class MyDelegate(DefaultDelegate):
            def __init__(self):
                DefaultDelegate.__init__(self)
                
            def handleNotification(self, cHandle, data):
                em.onReceivedData(data[2:])
        
        peripheral = Peripheral("EE:C5:46:65:D3:C1", "random")
        # tell rfduino we are ready for notifications
        peripheral.setDelegate(MyDelegate())
        peripheral.writeCharacteristic(peripheral.getCharacteristics(uuid=my_uuid)[0].valHandle + 1, "\x01\x00")
        loop.create_task(btleNotificationListen(peripheral))
        return

    def onSetupComplete(defaultCertificateName, keyChain):
        def onRequestSuccess():
            print "data production authorized by controller"
            startProducer(defaultCertificateName, keyChain)
            return

        def onRequestFailed(msg):
            print "data production request failed : " + msg
            # For this test, we start anyway
            startProducer(defaultCertificateName, keyChain)
            return

        bootstrap.requestProducerAuthorization(dataPrefix, appName, onRequestSuccess, onRequestFailed)
        
    def onSetupFailed(msg):
        print("Setup failed " + msg)

    bootstrap.setupDefaultIdentityAndRoot("app.conf", onSetupComplete = onSetupComplete, onSetupFailed = onSetupFailed)

    loop.run_forever()
