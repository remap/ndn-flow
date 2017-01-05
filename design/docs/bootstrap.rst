.. _Bootstrap:

Bootstrap Class
==========

Bootstrap class sets up a keyChain, default certificate name, (as a producer) requesting publishing authorization from controller, and (as a consumer) keeping track of trust schema changes.

:[C++]:
    | ``#include <ndn_iot_cpp/bootstrap.hpp>``
    | Namespace: ``ndn_iot``

:[Python]:
    Module: ``ndn_iot_python.bootstrap``

:[C#]:
    Package: ``ndn_iot.bootstrap``

Bootstrap Constructors
-----------------

Bootstrap Constructor (Face)
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Create a Bootstrap instance using the given Face. Bootstrap uses BasicIdentityStorage and FilePrivateKeyStorage by default, and IndexedDbIdentityStorage and IndexedDbPrivateKeyStorage in browser JavaScript.

:[C++]:

    .. code-block:: c++
    
        Bootstrap(
            ndn::Face& face
        );

:[JavaScript]:

    .. code-block:: javascript
    
        var Bootstrap = function Bootstrap(
            face  // Face
        )

:[Python]:

    .. code-block:: python
    
        def __init__(self, 
            face  # Face
        )

:[C#]:

    .. code-block:: c#
    
        public Bootstrap(
            Face face
        )
    
:Parameters:

    - `face`
        The face that Bootstrap instance uses to interact with the home controller or other devices

Bootstrap.setupDefaultIdentityAndRoot Methods
-------------------

Bootstrap.setupDefaultIdentityAndRoot
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Sets up a keyChain using the given identity name and controller name. Bootstrap uses default certificate names for given identities as default certificate names in the instance.

:[C++]:

    .. code-block:: c++
    
        ndn::ptr_lib::shared_ptr<ndn::KeyChain> setupDefaultIdentityAndRoot(
            ndn::Name defaultIdentity, 
            ndn::Name signerName
        );

:[JavaScript]:

    .. code-block:: javascript
    
        // Returns null
        Bootstrap.prototype.setupDefaultIdentityAndRoot = function(
            identityName,    // Name
            signerName,      // Name
            onSetupComplete, // Function object, onSetupComplete(CertName, KeyChain)
            onSetupFailed    // Function object, onSetupFailed(string)
        )

:[Python]:

    .. code-block:: python
    
        # Returns KeyChain
        def setupDefaultIdentityAndRoot(self, 
            defaultIdentityOrFileName,    # Name or string
            signerName,                   # Name
            onSetupComplete,              # Function object, onSetupComplete(CertName, KeyChain)
            onSetupFailed                 # Function object, onSetupFailed(string)
        )

:[C#]:

    .. code-block:: c#
    
        KeyChain setupDefaultIdentityAndRoot(
            Name defaultIdentityName,
            Name signerName
        )

:Parameters:

    - `defaultIdentityName`
        If identity name is given as empty then the default identity in the identityManager is used. If no default identities are present then an exception is thrown or onSetupFailed is called, and it's recommended to set an identity up using the ndn_pi add_device process.

    - `signerName`
        If signer name is given as empty then the signing identity for the default certificate (inferred from defaultIdentityName) is used. This name is also used as controller name in later communications. If a signerName is present and differs from the signer of the default certificate, then an exception is thrown or onSetupFailed is called. If you set up the identity using the ndn_pi add_device proess, it's recommended to give an empty name for this parameter. By the time this is function called, the controller certificate should be present in the local IdentityManager. Setting up the device identity using the ndn_pi add_device process should install the controller certificate.

    - `onSetupComplete`
        If present, it gets called with (Name defaultCertificateName, KeyChain keyChain) when setup finishes. (TODO: update Python for conformance!)

    - `onSetupFailed`
        If present, it gets called with (string reason) when setup fails.

:Returns:

    If onSetupComplete and onSetupFailed are not defined, returns the KeyChain set up by this method; otherwise return null.

Bootstrap.requestProducerAuthorization Methods
-------------------

Bootstrap.requestProducerAuthorization
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Send a command interest to the home controller (set up by the :ref:`Bootstrap.setupDefaultIdentityAndRoot` call) that requests to publish data for a given prefix in a given application using the identity of this instance. This call should be present in a producer module, and called before publishing data. If the request is granted, the controller will add a rule in the trust schema of the given applicationName that suggests data falling under the given dataPrefix should be signed by this instance's given certificate.

:[C++]:

    .. code-block:: c++
    
        void requestProducerAuthorization(
            ndn::Name dataPrefix, 
            std::string appName, 
            OnRequestSuccess onRequestSuccess, 
            OnRequestFailed onRequestFailed
        );

:[JavaScript]:

    .. code-block:: javascript
    
        // Returns null
        Bootstrap.prototype.requestProducerAuthorization = function(
            dataPrefix,       // Name
            appName,          // string
            onRequestSuccess, // Function object, onRequestSuccess()
            onRequestFailed   // Function object, onSetupFailed(string)
        )

:[Python]:

    .. code-block:: python
    
        # Returns None
        def requestProducerAuthorization(self, 
            dataPrefix,       # Name
            appName,          # string
            onRequestSuccess, # Function object, onRequestSuccess()
            onRequestFailed   # Function object, onSetupFailed(string)
        )

:[C#]:

    .. code-block:: c#
    
        void requestProducerAuthorization(
            Name dataPrefix, 
            string applicationName, 
            OnRequestSuccess onRequestSuccess, 
            OnRequestFailed onRequestFailed
        )

:Parameters:

    - `dataPrefix`
        The dataPrefix to request publishing for. This field, along with applicationName and this instance's identity is encoded in the command interest.

    - `applicationName`
        The application name to request publishing for. The controller organizes application trust schema by application names. This field, along with dataPrefix and this instance's identity is encoded in the command interest.

    - `onRequestSuccess`
        If the controller authorizes the request, then onRequestSuccess() is called. In C++ / C#, this is a std::function (or boost::function) / delegate. 

    - `onRequestFailed`
        If the controller fails to validate the request, doesn't authorize the request, or the response fails to validate, then onRequestFailed(reason) is called. (TODO: update for timeout handling!)

:Returns:

    Null

Bootstrap.startTrustSchemaUpdate Methods
-------------------

Bootstrap.startTrustSchemaUpdate
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Request the trust schema of an application from the controller. This call should be present in a consumer module, and called before consuming data. This call expects a timestamp namespace for updated trust schemas, and starts issuing interests with exclusion filters to request updates in the trust schemas.

:[C++]:

    .. code-block:: c++
    
        void startTrustSchemaUpdate(
            ndn::Name appPrefix, 
            OnUpdateSuccess onUpdateSuccess, 
            OnUpdateFailed onUpdateFailed
        );

:[JavaScript]:

    .. code-block:: javascript
    
        // Returns null
        Bootstrap.prototype.startTrustSchemaUpdate = function(
            appPrefix,       // Name
            onUpdateSuccess, // Function object, onUpdateSuccess(string, bool)
            onUpdateFailed   // Function object, onUpdateFailed(string)
        )

:[Python]:

    .. code-block:: python
    
        # Returns None
        def startTrustSchemaUpdate(self, 
            appPrefix,        # Name
            onUpdateSuccess,  # Function object, onUpdateSuccess(string, bool)
            onUpdateFailed    # Function object, onUpdateFailed(string)
        )

:[C#]:

    .. code-block:: c#
    
        void startTrustSchemaUpdate(
            Name appPrefix, 
            OnUpdateSuccess onUpdateSuccess, 
            OnUpdateFailed onUpdateFailed
        )

:Parameters:

    - `appPrefix`
        The application trust schema's prefix, usually controller name appended by application name, as used in :ref:`Bootstrap.requestProducerAuthorization` call. Each trust schema data is expected to be named as appPrefix + timestamp and signed by the controller.

    - `onUpdateSuccess`
        If an update in the trust schema is received and validated, then onUpdateSuccess(string, bool) is called with the schema string and if it's the first time an update succeeds. It is recommended to call consuming functionalities only after the first successful trust schema update. (TODO: handle segmented trust schema) 

    - `onUpdateFailed`
        If the response fails to validate, then onUpdateFailed(reason) is called.

:Returns:

    Null

Bootstrap.getDefaultCertificateName Methods
-------------------

Bootstrap.getDefaultCertificateName
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Returns the default certificate name set up by this Bootstrap instance. Should only be called after :ref:`Bootstrap.setupDefaultIdentityAndRoot`.

:[C++]:

    .. code-block:: c++
    
        Name getDefaultCertificateName(
        );

:[JavaScript]:

    .. code-block:: javascript
    
        // Returns Name
        Bootstrap.prototype.getDefaultCertificateName = function(
        )

:[Python]:

    .. code-block:: python
    
        # Returns Name
        def getDefaultCertificateName(self
        )

:[C#]:

    .. code-block:: c#
    
        Name getDefaultCertificateName(
        )

:Returns:

    The default certificate name set up by this Bootstrap instance.