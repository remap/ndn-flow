# IoT Framework interface

class Bootstrap(object):
    """
    Bootstrap class sets up preliminaries for producers and consumers. 
    Its functions cover setting up a KeyChain (producer, consumer), defaultIdentity (producer), policyManager (consumer), 
    as well as requesting publishing permission from trust anchor, and keeping track of trust schema for certain application name.

    Bootstrap class assumes that the configured identity is already signed by the trust anchor and installed (this is handled by ndn_pi), 
    and if not, an error will be thrown
    """
    def __init__(self, face):
        """        
        How to use: 
        Instantiate instance of this class

        :param Face face: The face used by this bootstrap instance
        
        :return: None
        """
        return

    def setupKeyChain(self, confObjOrFileName, requestPermission = True, onSetupComplete = None, onSetupFailed = None):
        """
        Sets up a KeyChain, default identity and certificate to use when publishing.

        :param dict or str confObjOrFileName: the configuration object containing publishing prefix, application name, default identity name, signer name, etc
        :param bool requestPermission: check / request publishing permission with root when this function is called
        :param Function object onSetupComplete: calls onSetupComplete(defaultIdentity, keyChain) when setup is complete
        :param Function object onSetupFailed: calls onSetupFailed(msg) when setup fails

        :return: None
        """
        return

    def startTrustSchemaUpdate(self, namePrefix):
        """
        Keeps outstanding interest for trust schema under a certain prefix, and reloads the schema each time a new one is received

        :param Name namePrefix: the prefix to which timestamp will be appended to ask for the next trust schema.

        :return: None
        """
        return
