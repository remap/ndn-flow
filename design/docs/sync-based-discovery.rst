.. _SyncBasedDiscovery:

SyncBasedDiscovery Class
==========

SyncBasedDiscovery class creates an instance that handles name discovery using mechanism similar with ChronoSync (digest exchange in multicast namespace, separation of discovery and actual data retrieval). Internally the digest is calculated as the sha256 hash of an ordered list of Name.toUri(). Published EntityInfo is not versioned, objects whose EntityInfo continuously times out are considered removed.

:[C++]:
    | ``#include <ndn_iot_cpp/entity-discovery.hpp>``
    | Namespace: ``ndn_iot::discovery``

:[Python]:
    Module: ``ndn_iot_python.discovery``

:[C#]:
    Package: ``ndn_iot.discovery``

SyncBasedDiscovery Constructors
-----------------

SyncBasedDiscovery Constructor 
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Instantiate a discovery instance for given prefix, using given face for communication, KeyChain for verification and certificate name for signing, and ExternalObserver for Name callbacks, EntitySerializer for object description serialization.

:[C++]:

    .. code-block:: c++
    
        EntityDiscovery(  // TODO: update names to conform with other libraries!
            ndn::Face& face, 
            ndn::KeyChain& keyChain, 
            ndn::Name certificateName, 
            ndn::Name broadcastPrefix, 
            ExternalObserver *observer, 
            ndn::ptr_lib::shared_ptr<EntitySerializer> serializer
        );

:[JavaScript]:

    .. code-block:: javascript
    
        var SyncBasedDiscovery = function SyncBasedDiscovery(
            face,             // Face
            keyChain,         // KeyChain
            certificateName,  // Name
            syncPrefix,       // Name
            observer,         // ExternalObserver
            serializer        // EntitySerializer
        )

:[Python]:

    .. code-block:: python
    
        def __init__(self, 
            face,             # Face
            keyChain,         # KeyChain
            certificateName,  # Name
            syncPrefix,       # Name
            observer,         # ExternalObserver
            serializer,       # EntitySerializer
        )

:[C#]:

    .. code-block:: c#
    
        public SyncBasedDiscovery(
            Face face, 
            KeyChain keyChain, 
            Name certificateName, 
            Name syncPrefix, 
            ExternalObserver observer, 
            EntitySerializer serializer
        )
    
:Parameters:

    - `face`
        The face that SyncBasedDiscovery instance uses to issue interest and receive data.

    - `keyChain`
        The KeyChain that SyncBasedDiscovery instance uses to verify received data and sign produced data. While it's not necessary to use this class with a KeyChain set up by a Bootstrap instance, it is recommended to do so since it's tracking updates in the trust schema.

    - `certificateName`
        Name used to sign discovery data produced by this instance

    - `syncPrefix`
        Multicast name prefix that sync interest (with digest appended) exchange uses.

    - `observer`
        ExternalObserver instance whose onStateChanged(name, msgType, msg) gets called when a new object is discovered or removed. It is recommended to derive your own Observer from the given ExternalObserver interface.

    - `serializer`
        EntitySerializer instance whose serialize() and deserialize(Entity) method gets called when serializing or deserializing a discovery object (putting the serialized string in the corresponding data packet for discovery, or deserializing the received data content to report to given Observer). 

SyncBasedDiscovery.start
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Starts discovery with parameters given in constructor.

:[C++]:

    .. code-block:: c++
    
        void start(
        );

:[JavaScript]:

    .. code-block:: javascript
    
        // Returns null
        SyncBasedDiscovery.prototype.start = function(
        )

:[Python]:

    .. code-block:: python
    
        # Returns None
        def start(self 
        )

:[C#]:

    .. code-block:: c#
    
        void start(
        )

:Returns:

    Null

SyncBasedDiscovery.stop
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Stops discovery.

:[C++]:

    .. code-block:: c++
    
        void stop(
        );

:[JavaScript]:

    .. code-block:: javascript
    
        // Returns null
        SyncBasedDiscovery.prototype.stop = function(
        )

:[Python]:

    .. code-block:: python
    
        # Returns None
        def stop(self 
        )

:[C#]:

    .. code-block:: c#
    
        void stop(
        )

:Returns:

    Null

SyncBasedDiscovery.publishObject Methods
-----------------

SyncBasedDiscovery.publishObject
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Adds an object locally for other peers to discover.

:[C++]:

    .. code-block:: c++
    
        bool publishObject(
            ndn::Name entityName, 
            ndn::ptr_lib::shared_ptr<EntityInfoBase> entityInfo
        );

:[JavaScript]:

    .. code-block:: javascript
    
        // Returns bool
        SyncBasedDiscovery.prototype.publishObject = function(
            name,       // string
            entityInfo  // EntityInfo (implements the EntityInfo interface)
        )

:[Python]:

    .. code-block:: python
    
        # Returns bool
        def addHostedObject(self,   # TODO: conform method name, "object vs entity" and function signature
            name,       # string
            entityInfo  # EntityInfo (implements the EntityInfo interface)
        )

:[C#]:

    .. code-block:: c#
    
        void publishObject(
            string name, 
            EntityInfoBase entityInfo
        )

:Parameters:

    - `name`
        The string / Name of the object to be added to the list of objects hosted by this instance.

    - `entityInfo`
        The description for the added entity. Please implement EntityInfo with serialize method.

:Returns:

    bool, whether the given object is added or not. If not, an object with the same name exists: either discovered or already added by this instance.


SyncBasedDiscovery.stopPublishingObject Methods
-----------------

SyncBasedDiscovery.stopPublishingObject
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Removes a locally hosted object so that it won't be discovered by others.

:[C++]:

    .. code-block:: c++
    
        bool stopPublishingEntity( // TODO: update this interface
            std::string entityName,
            ndn::Name prefix
        );

:[JavaScript]:

    .. code-block:: javascript
    
        // Returns bool
        SyncBasedDiscovery.prototype.removeHostedObject = function(
            name       // string
        )

:[Python]:

    .. code-block:: python
    
        # Returns bool
        def removeHostedObject(self,   # TODO: conform method name, "object vs entity" and function signature
            name       # string
        )

:[C#]:

    .. code-block:: c#
    
        void publishObject(
            string name
        )

:Parameters:

    - `name`
        The full name of the object to be removed.

:Returns:

    bool, whether the given object is removed or not. If not, an object with this name is not hosted locally.