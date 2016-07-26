# IoT Framework interface

# Library: General IoTNode (devices that runs the client library as well as the forwarder, not, for example, RFduinos and their Raspberry Pi helpers)
#  - bootstrap
#  - basic data production (insertion into MemoryContentCache)

class IoTNode(object):
    # TODO: derive controller node, and RFduino "helper" nodes from this? 
    def __init__(self, face, keyChain, certificateName):
        """
        
        How to use: 
        Instantiate instance of this class

        :param Face face: The face used by this IoTNode
        :param KeyChain keyChain: The keyChain this IoTNode uses to sign data, and verify received data
        :param Name certificateName: The certificate name to look up in the keyChain for signing data (this is the device-level name, which is used to sign application data)
        
        :return: None
        """
        return

    @staticmethod
    def getSerial(self):
        """
        Find the serial for this type of device. Could read from a serial file by default, and overloaded by different children classes representing different types of devices.
        
        :return: the serial number string
        :rtype: String
        """
        return

    def start(self):
        """
        Initialize face, register prefix for this device (name infered from the the given ceritificateName)
        
        :return: None
        """
        return

    def stop(self):
        """
        Stop face, unregister prefix

        :return: None
        """
        return

    def produce(self, data):
        """
        Produce data by adding it to this node's memoryContentCache (could support more types of producers e.g. repo-ng, may have a Producer parameter in the constructor, depending on need)
        (If the producer's a MemoryContentCache producer, its register prefix should be exposed via Producer interface? Similar for others, like repo-ng producer?)

        :parameter Data data: the data to be produced
        :return: None
        """
        return

    def addCommand(self, cmdName, callback):
        """
        Register prefix for cmdName

        :param Name cmdName: The prefix to register 
        :param callback: This calls callback(prefix, interest, face, filterId, filter) (as an OnInterestCallback) when an interest is received
        :type: function object
        :return: None
        """
        return

    def removeCommand(self, cmdName):
        """
        Unregisters the prefix as given by cmdName

        :param Name cmdName: The prefix to unregister
        :return: None
        """
        return

    # Internal functions for handling the bootstrap process
    # onAddCommandReceived
    # sendCertificate
    # onParentCertificateReceived
    # sendCapabilities

    # TODO: should also allow querying the bootstraping process? To provide information to other producers, for example, discovery instance?
    # TODO: internal configuration functions? Like updating trust schema? And exposed configuration function: load a configuration file, what would be its content?