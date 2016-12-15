import binascii
import time

from pyndn.encoding.element_reader import ElementReader
from btle_node import BtleNode

from bluepy.btle import UUID, Peripheral, DefaultDelegate

service_uuid = UUID(0x2220)
my_uuid = UUID(0x2221)
p = Peripheral("EE:C5:46:65:D3:C1", "random")

def onBtleData(data):
    print "got data: " + data.getName().toUri()

el = BtleNode(onBtleData, None, None)
em = ElementReader(el)


class MyDelegate(DefaultDelegate):
    def __init__(self):
        DefaultDelegate.__init__(self)
        
    def handleNotification(self, cHandle, data):
        em.onReceivedData(data[2:])

p.setDelegate(MyDelegate())
p.writeCharacteristic(p.getCharacteristics(uuid=my_uuid)[0].valHandle + 1, "\x01\x00")

while True:
    if p.waitForNotifications(1.0):
        continue

"""
try:
    #p.writeCharacteristic(0x2221, bytes(0x01), True)
    #print p.getCharacteristics()
    ch = p.getCharacteristics(uuid=my_uuid)[0]
    if (ch.supportsRead()):
        while True:
            val = ch.read()
            print binascii.hexlify(bytearray(val))
            if len(val) > 5:
                #print binascii.hexlify(bytearray(val))
                em.onReceivedData(val[2:])
            #time.sleep(0.001)
finally:
    p.disconnect()
"""
