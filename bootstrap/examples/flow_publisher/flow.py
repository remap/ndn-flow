#!/usr/bin/python

#
# This is the data publisher using MemoryContentCache
#

from pyndn import Name, Face, Interest, Data
from pyndn.util.memory_content_cache import MemoryContentCache
from pyndn.security import KeyChain, SecurityException
from pyndn.security.identity import IdentityManager, FilePrivateKeyStorage, BasicIdentityStorage
from pyndn.security.policy import NoVerifyPolicyManager

import time
import sys
import json

import logging

class FlowPublisher(object):
    def __init__(self, face, keyChain, confFile = "flow.conf"):
        self._keyChain = keyChain
        self._defaultIdentity = None
        self._face = face

        self.processConfiguration(confFile)
        self._defaultCertificateName = self._keyChain.getIdentityManager().getDefaultCertificateNameForIdentity(self._defaultIdentity)

        self._face.setCommandSigningInfo(self._keyChain, self._defaultCertificateName)
        print "Using default certificate name: " + self._defaultCertificateName.toUri()

    def processConfiguration(self, confFile):
        with open(confFile, "r") as f:
            conf = f.read()
            confObj = json.loads(conf)
            if "identity" in confObj:
                if confObj["identity"] == "default":
                    self._defaultIdentity = self._keyChain.getDefaultIdentity()
                else:
                    defaultIdName = Name(confObj["identity"])
                    try:
                        defaultKey = self._keyChain.getIdentityManager().getDefaultKeyNameForIdentity(defaultIdName)
                        self._defaultIdentity = defaultIdName
                    except SecurityException:
                        print "Identity " + defaultIdName.toUri() + " in configuration file " + confFile + " does not exist. Please configure the device with this identity first"
        return
    
    def start(self):
        return

if __name__ == '__main__':
    try:
        import psutil as ps
    except Exception as e:
        print str(e)
    identityManager = IdentityManager(BasicIdentityStorage(), FilePrivateKeyStorage())
    face = Face()
    keyChain = KeyChain()

    n = FlowPublisher(face, keyChain)
    n.start()
