# IoT Framework interface

# Library: Discovery (sync-based discovery)

class Discovery(object):
    
    def __init__(self, face, keyChain, certificateName, syncPrefix, onReceivedSyncData, 
      syncDataFreshnessPeriod = 4000, initialDigest = "00", syncInterestLifetime = 4000, syncInterestMinInterval = 500,
      timeoutCntThreshold = 3, maxResponseWaitPeriod = 2000, minResponseWaitPeriod = 400):
        """
        Initialize a discovery instance using given Face, KeyChain, CertificateName, and exchanges under the given SyncPrefix namespace.
        Upon starting, discovery sends out the first interest with name "syncPrefix/00" (00 is the initial digest).
        For each object hosted by this instance, discovery adds its name to the alphabetically sorted list of discovered objects. 
        A sha256 of the list is calculated to be the current digest.
        Each discovery instance replies to sync interest with its local sorted list of discovered names, when its local hash is different from the received one.
        And upon receiving a valid sync data reply, discovery calls an application function onReceivedSyncData(reply). 
        The application could, for example, express interest with those names, and for each response received, calls discovery's addObject method to add the new name to the list of discovered objects.
        
        Discovery keeps a dictionary of <object name uri, status>, where status contains current timeout count. An object is removed when discovery's incrementTimeoutCnt is called and the count exceeds a given threshold.
        Discovery also keeps an ordered list of discovered object names, and an ordered list of hosted object names. The latter should be a subset of the former.

        :param Face face: The face discovery uses to issue interest and receive data
        :param KeyChain keyChain: The keyChain discovery uses to sign sync data, and verify received sync data
        :param Name certificateName: The certificate name to look up in the keyChain for signing sync data
        :param onReceivedSyncData: This calls onReceivedSyncData(data) once a verified sync response is received
        :type onReceivedSyncData: function object

        :return: None
        """
        return

    def start(self):
        """
        Start discovery by sending out initial interest with name "syncPrefix/00" (00 is the initial digest)
        
        :return: None
        """
        return

    def stop(self):
        """
        Stop discovery: unregister syncPrefix, no longer send discovery interests, or heartbeat interests for discovered objects
        
        :return: None
        """
        return

    def addHostedObject(self, name):
        """
        Add a name to the list of objects hosted by this instance, and the list of objects currently discovered by this instance. 
        A new hash of the set of names will be generated. 

        :param Name name: the name of the added object
        :return: True if this name's added, False if this name's already in the discovered list
        :rtype: Bool
        """
        return

    def removeHostedObject(self, name):
        """
        Remove a name from the list of objects hosted by this instance, and the list of objects currently discovered by this instance. 
        A new hash of the set of names will be generated. 

        :param Name name: the name of the removed object
        :return: True if this name's removed, False if this name's not in the hosted list
        :rtype: Bool
        """
        # TODO: consider leaving a "removed" token when removing so that others can know the removal from the next heartbeat message?
        return

    def addObject(self, name):
        """
        Add a name to the list of objects currently discovered by this instance. 
        A new hash of the set of names will be generated. 

        :param Name name: the name of the added object
        :return: True if this name's added, False if this name's already in the discovered list
        :rtype: Bool
        """
        return

    def removeObject(self, name):
        """
        Remove a name from the list of objects currently discovered by this instance. 
        A new hash of the set of names will be generated. 

        :param Name name: the name of the removed object
        :return: True if this name's removed, False if this name's not in the discovered list
        :rtype: Bool
        """
        return

    def incrementTimeoutCnt(self, name):
        """
        Increment the timeout count for a discovered object. 
        If the incremented timeout count hits the threshold, remove the object from the discovered list.

        :param Name name: the name of the removed object
        :return: True if this name's discovered and not hosted, False if this name's not in the discovered list
        :rtype: Bool
        """
        return

    def resetTimeoutCnt(self, name):
        """
        Reset the timeout count for a discovered object. 
        
        :param Name name: the name of the removed object
        :return: True if this name's discovered and not hosted, False if this name's not in the discovered list
        :rtype: Bool
        """        
        return

# Library: Bootstrap trust


# Library: Naive Pub/Sub


