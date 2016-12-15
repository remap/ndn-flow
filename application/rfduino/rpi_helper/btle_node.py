import inspect
import logging
import threading
from random import SystemRandom

from pyndn.node import Node
from pyndn.data import Data
from pyndn.encoding.tlv.tlv import Tlv
from pyndn.encoding.tlv.tlv_decoder import TlvDecoder
from pyndn.encoding.tlv_wire_format import TlvWireFormat
from pyndn.impl.delayed_call_table import DelayedCallTable
from pyndn.lp.lp_packet import LpPacket

class BtleNode(Node):
    def __init__(self, onBtleData, transport, connectionInfo):
        super(BtleNode, self).__init__(transport, connectionInfo)
        self._onBtleData = onBtleData

    def onReceivedElement(self, element):
        """
        This is called by the transport's ElementReader to process an
        entire received Data or Interest element.
        :param element: The bytes of the incoming element.
        :type element: An array type with int elements
        """

        lpPacket = None
        if element[0] == Tlv.LpPacket_LpPacket:
            # Decode the LpPacket and replace element with the fragment.
            lpPacket = LpPacket()
            # Set copy False so that the fragment is a slice which will be
            # copied below. The header fields are all integers and don't need to
            # be copied.
            TlvWireFormat.get().decodeLpPacket(lpPacket, element, False)
            element = lpPacket.getFragmentWireEncoding().buf()

        # First, decode as Interest or Data.
        data = None
        decoder = TlvDecoder(element)
        if decoder.peekType(Tlv.Data, len(element)):
            data = Data()
            data.wireDecode(element, TlvWireFormat.get())

            if lpPacket != None:
                data.setLpPacket(lpPacket)

        # Now process as Interest or Data.
        if data != None:
            if self._onBtleData:
                self._onBtleData(data)
            
