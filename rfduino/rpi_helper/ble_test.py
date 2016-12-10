import binascii
import time
from bluepy.btle import UUID, Peripheral

from pyndn.encoding.element_reader import ElementReader

my_uuid = UUID(0x2221)
p = Peripheral("EE:C5:46:65:D3:C1", "random")

el = ElementListener
em = ElementReader(el)

try:
    ch = p.getCharacteristics(uuid=my_uuid)[0]
    if (ch.supportsRead()):
        while True:
            val = ch.read()
            print len(val)
            em.onReceivedData(val)
            time.sleep(0.001)
finally:
    p.disconnect()
