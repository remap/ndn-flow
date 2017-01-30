# for ble receive functionalities
import binascii
import time

from pyndn.encoding.element_reader import ElementReader
from btle_node import BtleNode

from bluepy.btle import UUID, Peripheral, DefaultDelegate, BTLEException

# for publishing functionalities
from pyndn import Name, Interest, Data
from pyndn.util.memory_content_cache import MemoryContentCache
from pyndn.security import KeyChain
from pyndn.threadsafe_face import ThreadsafeFace

from ndn_iot_python.bootstrap.bootstrap import Bootstrap

import time
import sys
import json
import struct
import math
import logging
import argparse

try:
    import asyncio
except ImportError:
    import trollius as asyncio

class AppProducer():
    def __init__(self, face, certificateName, keyChain, dataPrefix, security = False):
        self._keyChain = keyChain
        self._certificateName = certificateName
        self._face = face
        self._dataPrefix = dataPrefix
        self._security = security
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
        content = data.getContent().toRawStr()
        print "got data: " + data.getName().toUri() + " : " + content

        
        if self._security:
            # Hmac verify the data we receive
            pass

        pyr = content.split(',')
        if len(pyr) >= 3:
            resultingContent = "{\"p\":" + pyr[0] + ",\"y\":" + pyr[1] + ",\"r\":" + pyr[2] + "}"
            timestamp = time.time() * 1000
            dataOut = Data(Name(self._dataPrefix).appendVersion(int(timestamp)))
            dataOut.setContent(resultingContent)
            dataOut.getMetaInfo().setFreshnessPeriod(10000)
            self._keyChain.sign(dataOut, self._certificateName)

            self._dataCache.add(dataOut)
            print "data added: " + dataOut.getName().toUri()

    def makePublicKeyInterest(self):
        interest = Interest(Name("/"))
        interest.getName().append(self._keyChain.getCertificate(self._certificateName).getPublicKeyInfo().getKeyDer())
        return interest

class Logger(object):
    def prepareLogging(self):
        self.log = logging.getLogger(str(self.__class__))
        self.log.setLevel(logging.DEBUG)
        logFormat = "%(asctime)-15s %(name)-20s %(funcName)-20s (%(levelname)-8s):\n\t%(message)s"
        self._console = logging.StreamHandler()
        self._console.setFormatter(logging.Formatter(logFormat))
        self._console.setLevel(logging.INFO)
        # without this, a lot of ThreadsafeFace errors get swallowed up
        logging.getLogger("trollius").addHandler(self._console)
        self.log.addHandler(self._console)

    def setLogLevel(self, level):
        """
        Set the log level that will be output to standard error
        :param level: A log level constant defined in the logging module (e.g. logging.INFO) 
        """
        self._console.setLevel(level)

    def getLogger(self):
        """
        :return: The logger associated with this node
        :rtype: logging.Logger
        """
        return self.log

class BtlePeripheral():
    def __init__(self, addr, producer, loop, receive_uuid = 0x2221, send_uuid = 0x2222, security = False):
        self._addr = addr
        self._producer = producer
        self._receive_uuid = receive_uuid
        self._send_uuid = send_uuid
        self._loop = loop
        self._security = security
        self._p = None

    def start(self):
        # init btle ElementReader
        el = BtleNode(self._producer.onBtleData, None, None)
        em = ElementReader(el)

        class MyDelegate(DefaultDelegate):
            def __init__(self):
                DefaultDelegate.__init__(self)
                
            def handleNotification(self, cHandle, data):
                # TODO: this should handle incorrect format caused by packet losses
                try:
                    em.onReceivedData(data[2:])
                except ValueError as e:
                    print "Decoding value error: " + str(e)
                # connect ble
        
        while not self._p:
            try:
                self._p = Peripheral(self._addr, "random")
            except BTLEException as e:
                print "Failed to connect: " + str(e) + "; trying again"
                self._p = None

        # tell rfduino we are ready for notifications
        self._p.setDelegate(MyDelegate())

        self._p.writeCharacteristic(self._p.getCharacteristics(uuid = self._receive_uuid)[0].valHandle + 1, "\x01\x00")
        self._loop.create_task(self.btleNotificationListen())
        
        if self._security:
            # send our public key if configured to do so
            print "security on, sending our public key"
            interest = self._producer.makePublicKeyInterest()
            # write characteristics
            data = interest.wireEncode().toRawStr()
            num_fragments = int(math.ceil(float(len(data)) / 18))
            print "length of data: " + str(len(data)) + "; number of fragments: " + str(num_fragments)
            current = 0
            for i in range(0, num_fragments - 1):
                fragment = struct.pack(">B", i) + struct.pack(">B", num_fragments) + data[current:current + 18]
                current += 18
                self._p.writeCharacteristic(self._p.getCharacteristics(uuid = self._send_uuid)[0].valHandle, fragment)
                print " ".join(x.encode('hex') for x in fragment)
            fragment = struct.pack(">B", num_fragments - 1) + struct.pack(">B", num_fragments) + data[current:]
            self._p.writeCharacteristic(self._p.getCharacteristics(uuid = self._send_uuid)[0].valHandle, fragment)

            print " ".join(x.encode('hex') for x in fragment)

    @asyncio.coroutine
    def btleNotificationListen(self):
        try:
            while True:
                if self._p.waitForNotifications(0.2):
                    pass
                time.sleep(0.01)
                yield None
        except BTLEException as e:
            print("Btle exception: " + str(e) + "; try to restart")
            self._p = None
            self.start()    
                

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description = 'RaspberryPi helper for RFduino in Flow')
    parser.add_argument('--addr', help = 'peripheral address (comma separated addrs if multiple)')
    parser.add_argument('--namespace', help = 'namespaces (comma separated names corresponding with each peripheral)')
    parser.add_argument('--security', help = 'flag for if key exchange should happen initially and if data should be verified')
    parser.add_argument('--request', help = 'request namespace to ask controller to grant publishing permission to (mutual parent of publishing namespaces)')

    logger = Logger()
    logger.prepareLogging()

    args = parser.parse_args()

    service_uuid = 0x2220
    send_uuid = 0x2222
    receive_uuid = 0x2221

    default_addr = "EE:C5:46:65:D3:C1"
    default_prefix = "/home/flow1/gyros/gyro1"
    default_request_prefix = "/home/flow1/gyros"
    default_security_option = False

    if args.addr:
        addrs = args.addr.split(',')
    else:
        addrs = [default_addr]

    if args.namespace:
        namespaces = args.namespace.split(',')
    else:
        namespaces = [default_prefix]

    if args.security:
        security_option = args.security
    else:
        security_option = default_security_option

    if args.request:
        request_prefix = args.request
    else:
        request_prefix = default_request_prefix

    loop = asyncio.get_event_loop()
    face = ThreadsafeFace(loop)
    
    bootstrap = Bootstrap(face)
    appName = "flow1"
    
    def startProducers(defaultCertificateName, keyChain):
        if len(addrs) != len(namespaces):
            print "argument length mismatch: addrs(" + str(len(addrs)) + ") and namespaces(" + str(len(namespaces)) + ")"
            return
        for i in range(0, len(addrs)):
            producer = AppProducer(face, defaultCertificateName, keyChain, Name(namespaces[i]))
            producer.start()
            peripheral = BtlePeripheral(addrs[i], producer, loop, receive_uuid, send_uuid, security_option)
            peripheral.start()
        return

    def onSetupComplete(defaultCertificateName, keyChain):
        def onRequestSuccess():
            print "data production authorized by controller"
            startProducers(defaultCertificateName, keyChain)
            return

        def onRequestFailed(msg):
            print "data production request failed : " + msg
            # For this test, we start anyway
            startProducers(defaultCertificateName, keyChain)
            return

        bootstrap.requestProducerAuthorization(Name(request_prefix), appName, onRequestSuccess, onRequestFailed)
        
    def onSetupFailed(msg):
        print(msg)
        print("In this test, try start publishing with default keychain certificate anyway")
        keyChain = KeyChain()
        try:
            defaultCertificateName = keyChain.getDefaultCertificateName()
            startProducers(defaultCertificateName, keyChain)
        except SecurityException as e:
            print str(e)

    bootstrap.setupDefaultIdentityAndRoot("app.conf", onSetupComplete = onSetupComplete, onSetupFailed = onSetupFailed)

    loop.run_forever()
