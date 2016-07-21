# -*- Mode:python; c-file-style:"gnu"; indent-tabs-mode:nil -*- */
#
# Copyright (C) 2014 Regents of the University of California.
# Author: Jeff Thompson <jefft0@remap.ucla.edu>
# 
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU Lesser General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.
# A copy of the GNU General Public License is in the file COPYING.

"""
This module defines the Sha256HmacSignature class which extends Signature and 
holds the signature bits and other info representing a SHA256-HMAC signature
in a data packet.
"""

from pyndn.util.change_counter import ChangeCounter
from pyndn.util import Blob
from pyndn.signature import Signature
from pyndn.key_locator import KeyLocator

class Sha256HmacSignature(Signature):
    """
    Create a new Sha256HmacSignature object, possibly copying values from 
    another object.
    
    :param value: (optional) If value is a Sha256HmacSignature, copy its 
      values.  If value is omitted, the keyLocator is the default with
      unspecified values and the signature is unspecified.
    :param value: Sha256HmacSignature
    """
    def __init__(self, value = None):
        if value == None:
            self._keyLocator = ChangeCounter(KeyLocator())
            self._signature = Blob()
        elif type(value) is Sha256HmacSignature:
            # Copy its values.
            self._keyLocator = ChangeCounter(KeyLocator(value.getKeyLocator()))
            self._signature = value._signature
        else:
            raise RuntimeError(
              "Unrecognized type for Sha256HmacSignature constructor: " +
              repr(type(value)))
            
        self._changeCount = 0
            
    def clone(self):
        """
        Create a new Sha256HmacSignature  which is a copy of this object.

        :return: A new object which is a copy of this object.
        :rtype: Sha256HmacSignature
        """
        return Sha256HmacSignature(self)

    def getKeyLocator(self):
        """
        Get the key locator.
        
        :return: The key locator.
        :rtype: KeyLocator
        """
        return self._keyLocator.get()

    def getSignature(self):
        """
        Get the data packet's signature bytes.
        
        :return: The signature bytes as a Blob, which maybe isNull().
        :rtype: Blob
        """
        return self._signature
    
    def setKeyLocator(self, keyLocator):
        """
        Set the key locator to a copy of the given keyLocator.
        
        :param KeyLocator keyLocator: The KeyLocator to copy.
        """
        self._keyLocator.set(KeyLocator(keyLocator)) 
        self._changeCount += 1

    def setSignature(self, signature):
        """
        Set the signature bytes to the given value.
        
        :param signature: The array with the signature bytes. If signature is 
          not a Blob, then create a new Blob to copy the bytes (otherwise 
          take another pointer to the same Blob).
        :type signature: A Blob or an array type with int elements 
        """
        self._signature = (signature if type(signature) is Blob 
                           else Blob(signature))
        self._changeCount += 1

    def clear(self):
        self._keyLocator.get().clear()
        self._signature = Blob()
        self._changeCount += 1        

    def getChangeCount(self):
        """
        Get the change count, which is incremented each time this object 
        (or a child object) is changed.

        :return: The change count.
        :rtype: int
        """
        # Make sure each of the checkChanged is called.
        changed = self._keyLocator.checkChanged()
        if changed:
            # A child object has changed, so update the change count.
            self._changeCount += 1

        return self._changeCount
