NDN-IoT framework interface (Python)
=============================
(All languages should hold similar interfaces, and initial /default values definition)

(TODO: OnDataValidationFailed across framework, resolve Face / ThreadsafeFace, Discovery function naming conformance)

**Boostrap**

Constructor
```python
class Bootstrap(object):
    """
    Create a Bootstrap object. Bootstrap object provides interface for setting up KeyChain, default certificate name;
    (as a producer) requesting publishing authorization from controller; and (as a consumer) keeping track of changes

    :param face: the face for communicating with a local / remote forwarder
    :type face: ThreadsafeFace

    TODO: support Face as well as ThreadsafeFace
    """
    def __init__(self, face):
```

Set up default identity and certificate name of this instance, and controller name
```python
def setupDefaultIdentityAndRoot(self, defaultIdentityOrFileName, signerName, onSetupComplete, onSetupFailed):
    """
    Sets up the keyChain, default key name and certificate name according to given 
    configuration. If successful, this KeyChain and default certificate name will be 
    returned to the application, which can be passed to instances like Consumer, Discovery, etc

    :param defaultIdentityOrFileName: if str, the name of the configuration file; if Name, 
      the default identity name of this IoT node. The node will use the default keys and 
      certificate of that identity name.
    :type defaultIdentityOrFileName: Name or str
    :param signerName: (optional) the expected signing identity of the certificate
    :type signerName: Name
    :param onSetupComplete: (optional) onSetupComplete(Name, KeyChain) will be called if 
      set up's successful
    :type onSetupComplete: function object
    :param onSetupFailed: (optional) onSetupFailed(msg) will be called if setup fails
    :type onSetupFailed: function object
    """
```

Consumer functionality bootstrap
```python
def startTrustSchemaUpdate(self, appPrefix, onUpdateSuccess = None, onUpdateFailed = None):
    """
    Starts trust schema update for under an application prefix: initial 
    interest asks for the rightMostChild, and later interests are sent 
    with previous version excluded. Each verified trust schema will trigger
    onUpdateSuccess and update the ConfigPolicyManager for the keyChain
    in this instance, and unverified ones will trigger onUpdateFailed.

    The keyChain and trust anchor should be set up using setupDefaultIdentityAndRoot
    before calling this method. 

    :param appPrefix: the prefix to ask trust schema for. (interest name: /<prefix>/_schema)
    :type appPrefix: Name
    :param onUpdateSuccess: (optional) onUpdateSuccess(trustSchemaStr, isInitial) is 
      called when update succeeds
    :type onUpdateSuccess: function object
    :param onUpdateFailed: (optional) onUpdateFailed(msg) is called when update fails
    :type onUpdateFailed: function object
    """
```

Producer functionality bootstrap
```python
def requestProducerAuthorization(self, dataPrefix, appName, onRequestSuccess = None, onRequestFailed = None):
    Requests producing authorization for a data prefix: commandInterest is sent out 
    to the controller, using /<controller identity>/requests/<encoded-application-parameters>/<signed-interest-suffix>
    where encoded-application-parameters is a ProtobufTlv encoding of 
    {appPrefix, certificateName, appName}
    """
    The keyChain, trust anchor and controller name should be set up using 
    setupDefaultIdentityAndRoot before calling this method.

    :param dataPrefix: the prefix to request publishing for
    :type dataPrefix: Name
    :param appName: the application name to request publishing for
    :type appName: str
    :param onRequestSuccess: (optional) onRequestSuccess() is called when a valid response
      if received for the request
    :type onRequestSuccess: function object
    :param onRequestFailed: (optional) onRequestFailed(msg) is called when request fails
    :type onRequestFailed: function object
    """
```

**Consumer**

Consumer interface class
```python
class AppConsumer():
    """
    The interface for common application consumers (e.g. sequence
    number based and timestamp based)

    :param face: the face to consume data with
    :type face: Face
    :param keyChain: the keyChain to verify received data with
    :type keyChain: KeyChain
    :param certificateName: the certificate name to sign data with
      (not used by default for consumers)
    :type certificateName: Name
    :param doVerify: flag for whether the consumer should skip verification
    :type doVerify: bool
    """
    def __init__(self, face, keyChain, certificateName, doVerify):
```

Interface class consume call
```python
    def consume(self, name, onData, onVerifyFailed, onTimeout):
        """
        Consume one piece of data, or consume continuously, depending on
        child class's implementation

        :param name: name / prefix to consume data under
        :type name: Name
        :param onData: onData(data) gets called after received data's onVerifyFailed
        :type onData: function object
        :param onVerifyFailed: onVerifyFailed(data) gets called if received data 
          cannot be verified
        :type onVerifyFailed: function object
        :param onTimeout: onTimeout(interest) gets called if a consumer interest times out
        :type onTimeout: function object
        """
        return
```

Sequence number consumer constructor
```python
class AppConsumerSequenceNumber(AppConsumer):
    """
    Sequence number based consumer with interest pipelining

    :param face: the face to consume data with
    :type face: Face
    :param keyChain: the keyChain to verify received data with
    :type keyChain: KeyChain
    :param doVerify: flag for whether the consumer should skip verification
    :type doVerify: bool
    :param defaultPipelineSize: interest pipeline size
    :type defaultPipelineSize: int
    :param startingSeqNumber: the starting sequence number to ask for
    :type startingSeqNumber: int
    """
    def __init__(self, face, keyChain, doVerify, defaultPipelineSize = 5, startingSeqNumber = 0):
```

Timestamp consumer constructor
```python
class AppConsumerTimestamp(AppConsumer):
    """
    Timestamp based consumer with exclusion filters

    :param face: the face to consume data with
    :type face: Face
    :param keyChain: the keyChain to verify received data with
    :type keyChain: KeyChain
    :param doVerify: flag for whether the consumer should skip verification
    :type doVerify: bool
    :param currentTimestamp: the timestamp to start excluding with
    :type currentTimestamp: int
    """
    def __init__(self, face, keyChain, doVerify, currentTimestamp = None):
```

**Discovery**

Discovery constructor and functionalities
```python
class SyncBasedDiscovery(object):
    """
    Sync (digest exchange) based name discovery.
    Discovery maintains a list of discovered, and a list of hosted objects.
    Calls observer.onStateChanged(name, msgType, msg) when an entity is discovered or removed.
    Uses serializer.serialize(entityObject) to serialize a hosted entity's entityInfo into string.

    :param face:
    :type face: Face
    :param keyChain: 
    :type keyChain: KeyChain
    :param certificateName:
    :type certificateName: Name
    :param syncPrefix:
    :type syncPrefix: Name
    :param observer:
    :type observer: ExternalObserver
    :param serializer:
    :type serializer: EntitySerializer
    """
    def __init__(self, face, keyChain, certificateName, syncPrefix, observer, serializer)
```

```python
    def start(self):
        """
        Starts the discovery
        """
```

```python
    def stop(self):
        """
        Stops the discovery
        """
```

```python
    def addHostedObject(self, name, entityInfo):
        """
        Adds another object and registers prefix for that object's name

        :param name: the object's name string
        :type name: str
        :param entityInfo: the application given entity info to describe this object name with
        :type entityInfo: EntityInfo
        """
```

```python
    def removeHostedObject(self, name):
        """
        Removes a locally hosted object

        :param name: the object's name string
        :type name: str
        :return: whether removal's successful or not
        :rtype: bool
        """
```