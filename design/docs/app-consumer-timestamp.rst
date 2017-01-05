.. _AppConsumerTimestamp:

AppConsumerTimestamp Class
==========

AppConsumerTimestamp class derives from the Consumer interface, and consumes data named in a timestamp namespace using exclusion filters.

:[C++]:
    | ``#include <ndn_iot_cpp/app-consumer-timestamp.hpp>``
    | Namespace: ``ndn_iot``

:[Python]:
    Module: ``ndn_iot_python.consumer``

:[C#]:
    Package: ``ndn_iot.consumer``

AppConsumerTimestamp Constructors
-----------------

AppConsumerTimestamp Constructor (Face, KeyChain, DoVerify, CurrentTimestamp)
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Create a AppConsumerTimestamp instance using the given Face, KeyChain, DoVerify, and CurrentTimestamp. While it's not necessary to use this class with a KeyChain set up by a Bootstrap instance, it's recommended to do so, so that data verification is handled by that KeyChain automatically.

:[C++]:

    .. code-block:: c++
    
        AppConsumerTimestamp(
            ndn::Face& face, 
            ndn::ptr_lib::shared_ptr<ndn::KeyChain> keyChain, 
            bool doVerify, 
            int64_t currentTimestamp = -1
        );

:[JavaScript]:

    .. code-block:: javascript
    
        var AppConsumerTimestamp = function AppConsumerTimestamp(
            face,                // Face
            keyChain,            // KeyChain
            doVerify,            // bool
            currentTimestamp     // Number or undefined
        )

:[Python]:

    .. code-block:: python
    
        def __init__(self, 
            face,                    # Face
            keyChain,                # KeyChain
            doVerify,                # bool
            currentTimestamp = None  # Int or None
        )

:[C#]:

    .. code-block:: c#
    
        public Bootstrap(
            Face face, 
            KeyChain keyChain, 
            bool doVerify, 
            long currentTimestamp = -1
        )
    
:Parameters:

    - `face`
        The face that AppConsumerSequenceNumber instance uses to issue interest.

    - `keyChain`
        The KeyChain that AppConsumerSequenceNumber instance uses to verify received data. It is recommended to use the KeyChain set up by Bootstrap, which is tracking updates in the trust schema.

    - `doVerify`
        Flag that controls whether receives data should be verified. It is recommended to use true but this can be disabled for testing purposes by setting to false.

    - `currentTimestamp`
        (Optional) Current timestamp this instance uses to append to interest exclusions. Non-negative numbers means starting to issue interest with <Any, that number> excluded, while -1, undefined or None means start with the first timestamp this instance can receive (with rightMostChild) from the network. Defaults to -1, undefined or None if not present.

AppConsumerTimestamp.consume Methods
-------------------

AppConsumerTimestamp.consume
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Starts consuming data in the given namespace.

:[C++]:

    .. code-block:: c++
    
        void consume(
            ndn::Name prefix, 
            ndn::OnVerified onVerified, 
            ndn::OnVerifyFailed onVerifyFailed, 
            ndn::OnTimeout onTimeout
        );

:[JavaScript]:

    .. code-block:: javascript
    
        // Returns null
        AppConsumerSequenceNumber.prototype.consume = function(
            prefix,         // Name
            onVerified,     // Function Object, onVerified(Data)
            onVerifyFailed, // Function Object, onVerifyFailed(Data)
            onTimeout       // Function Object, onTimeout(Interest)
        )

:[Python]:

    .. code-block:: python
    
        # Returns None
        def consume(self, 
            prefix,         # Name
            onVerified,     # Function Object, onVerified(Data)
            onVerifyFailed, # Function Object, onVerifyFailed(Data)
            onTimeout       # Function Object, onTimeout(Interest)
        )

:[C#]:

    .. code-block:: c#
    
        void consume(
            Name prefix, 
            OnVerified onVerified, 
            OnDataValidationFailed onVerifyFailed, 
            OnTimeout onTimeout
        )

:Parameters:

    - `prefix`
        The data prefix to consume data under: the expected full data name is prefix + versioned timestamp.

    - `onVerified`
        If the data is successfully verified by KeyChain.verifyData call or doVerify flag is disabled, then this is called.

    - `onVerifyFailed`
        If doVerify flag is enabled and received data fails to validate, this is called with the data and reason (TODO: update to OnDataValidationFailed!)

    - `onTimeout`
        If an interest times out, this is called.

:Returns:

    Null
