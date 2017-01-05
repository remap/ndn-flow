.. _Bootstrap:

Bootstrap Class
==========

Bootstrap class sets up a keyChain, default certificate name, (as a producer) requesting publishing authorization from controller, and (as a consumer) keeping track of trust schema changes.

:[C++]:
    | ``#include <ndn_iot_cpp/bootstrap.hpp>``
    | Namespace: ``ndn_iot``

:[Python]:
    Module: ``ndn_iot_python``

:[C#]:
    Package: ``net.named_data.jndn``

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
        If present, it gets called with (Name defaultCertificateName, KeyChain keyChain) when setup finishes.

    - `onSetupFailed`
        If present, it gets called with (string reason) when setup fails.

:Returns:

    If onSetupComplete and onSetupFailed are not defined, returns the KeyChain set up by this method; otherwise return null.


