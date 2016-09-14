
# -*- Mode:python; c-file-style:"gnu"; indent-tabs-mode:nil -*- */
#
# Copyright (C) 2016 Regents of the University of California.
# Author: Adeola Bannis <thecodemaiden@gmail.com> Zhehao Wang <zhehao@cs.ucla.edu>
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

import logging
import time
import sys

from pyndn import Name, Face, Interest, Data
from pyndn.security import KeyChain

from pyndn.security.policy import ConfigPolicyManager
from pyndn.security.identity import IdentityManager, BasicIdentityStorage, FilePrivateKeyStorage
from pyndn.security.security_exception import SecurityException

from collections import namedtuple

from security.iot_policy_manager import IotPolicyManager

try:
    import asyncio
except ImportError:
    import trollius as asyncio

from pyndn.threadsafe_face import ThreadsafeFace

Command = namedtuple('Command', ['suffix', 'function', 'keywords', 'isSigned'])

class BaseNode(object):
    """
    This class contains methods/attributes common to both node and controller.
    """
    def __init__(self, transport = None, conn = None):
        """
        Initialize the network and security classes for the node
        """
        super(BaseNode, self).__init__()
        self.faceTransport = transport
        self.faceConn = conn
        
        self._identityStorage = BasicIdentityStorage()

        self._identityManager = IdentityManager(self._identityStorage, FilePrivateKeyStorage())
        self._policyManager = IotPolicyManager(self._identityStorage)

        # hopefully there is some private/public key pair available
        self._keyChain = KeyChain(self._identityManager, self._policyManager)

        self._registrationFailures = 0
        self._prepareLogging()

        self._setupComplete = False


##
# Logging
##
    def _prepareLogging(self):
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

###
# Startup and shutdown
###
    def beforeLoopStart(self):
        """
        Called before the event loop starts.
        """
        pass

    def getDefaultCertificateName(self):
        try:
            certName = self._identityStorage.getDefaultCertificateNameForIdentity( 
                self._policyManager.getDeviceIdentity())
        except SecurityException as e:
            # zhehao: in the case of producer's /localhop prefixes, the default key is not defined in ndnsec-public-info.db
            certName = self._keyChain.createIdentityAndCertificate(self._policyManager.getDeviceIdentity())
            #certName = self._keyChain.getDefaultCertificateName()
            #print(certName.toUri())
        return certName

    def start(self):
        """
        Begins the event loop. After this, the node's Face is set up and it can
        send/receive interests+data
        """
        self.log.info("Starting up")
        self.loop = asyncio.get_event_loop()
        
        if (self.faceTransport == None or self.faceTransport == ''):
            self.face = ThreadsafeFace(self.loop)
        else:
            self.face = ThreadsafeFace(self.loop, self.faceTransport, self.faceConn)
        
        self.face.setCommandSigningInfo(self._keyChain, self.getDefaultCertificateName())
        
        self._keyChain.setFace(self.face)

        self._isStopped = False
        self.beforeLoopStart()
        
        try:
            self.loop.run_forever()
        except Exception as e:
            self.log.exception(exc_info=True)
        finally:
            self.stop()

    def stop(self):
        """
        Stops the node, taking it off the network
        """
        self.log.info("Shutting down")
        self._isStopped = True 
        self.loop.stop()
        
###
# Data handling
###
    def signData(self, data):
        """
        Sign the data with our network certificate
        :param pyndn.Data data: The data to sign
        """
        self._keyChain.sign(data, self.getDefaultCertificateName())

    def sendData(self, data, sign=True):
        """
        Reply to an interest with a data packet, optionally signing it.
        :param pyndn.Data data: The response data packet
        :param boolean sign: (optional, default=True) Whether the response must be signed. 
        """
        if sign:
            self.signData(data)
        self.face.putData(data)

###
# 
# 
##
    def onRegisterFailed(self, prefix):
        """
        Called when the node cannot register its name with the forwarder
        :param pyndn.Name prefix: The network name that failed registration
        """
        if self.faceTransport != None and self.faceConn != None:
            self.log.warn("Explicit face transport and connectionInfo: Could not register {}; expect a manual or autoreg on the other side.".format(prefix.toUri()))
        elif self._registrationFailures < 5:
            self._registrationFailures += 1
            self.log.warn("Could not register {}, retry: {}/{}".format(prefix.toUri(), self._registrationFailures, 5)) 
            self.face.registerPrefix(self.prefix, self._onCommandReceived, self.onRegisterFailed)
        else:
            self.log.info("Prefix registration failed")
            self.stop()

    def verificationFailed(self, dataOrInterest):
        """
        Called when verification of a data packet or command interest fails.
        :param pyndn.Data or pyndn.Interest: The packet that could not be verified
        """
        self.log.info("Received invalid" + dataOrInterest.getName().toUri())

    @staticmethod
    def getSerial():
        """
        Find and return the serial number of the Raspberry Pi. Provided in case
        you wish to distinguish data from nodes with the same name by serial.
        :return: The serial number extracted from device information in /proc/cpuinfo
        :rtype: str
        """
        try:
            if PLATFORM == "raspberry pi":
                with open('/proc/cpuinfo') as f:
                    for line in f:
                        if line.startswith('Serial'):
                            return line.split(':')[1].strip()
            else:
                return "todo"
        except NameError:
            return "todo"
