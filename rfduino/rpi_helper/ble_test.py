import binascii
import time
from bluepy.btle import UUID, Peripheral

my_uuid = UUID(0x2221)
p = Peripheral("EE:C5:46:65:D3:C1", "random")

try:
    ch = p.getCharacteristics(uuid=my_uuid)[0]
    if (ch.supportsRead()):
        while True:
            print ch.read()
            val = binascii.b2a_hex(ch.read())
            val = binascii.unhexlify(val)
            print "got " + val
            time.sleep(1)
finally:
    p.disconnect()
