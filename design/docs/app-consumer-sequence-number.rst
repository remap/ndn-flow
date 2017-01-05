.. _AppConsumerSequenceNumber:

AppConsumerSequenceNumber Class
==========

AppConsumerSequenceNumber class derives from the Consumer interface, and consumes data named in a sequence number namespace using interest pipelining.

:[C++]:
    | ``#include <ndn_iot_cpp/app-consumer-sequence-number.hpp>``
    | Namespace: ``ndn_iot``

:[Python]:
    Module: ``ndn_iot_python.consumer``

:[C#]:
    Package: ``ndn_iot.consumer``

AppConsumerSequenceNumber Constructors
-----------------

AppConsumerSequenceNumber Constructor (Face, KeyChain, DoVerify, PipelineSize, startingSequenceNumber)
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Create a AppConsumerSequenceNumber instance using the given Face, KeyChain, doVerify, pipelineSize and startingSequenceNumber. While it's not necessary to use this class with a KeyChain set up by a Bootstrap instance, it's recommended to do so, so that data verification is handled by that KeyChain automatically.

:[C++]:

    .. code-block:: c++
    
        AppConsumerSequenceNumber(
            ndn::Face& face, 
            ndn::ptr_lib::shared_ptr<ndn::KeyChain> keyChain, 
            bool doVerify, 
            int defaultPipelineSize = 5, 
            int startingSequenceNumber = 0   // TODO: update initial value!
        );

:[JavaScript]:

    .. code-block:: javascript
    
        var AppConsumerSequenceNumber = function AppConsumerSequenceNumber(
            face,                // Face
            keyChain,            // KeyChain
            doVerify,            // bool
            defaultPipelineSize, // Number
            startingSeqNumber    // Number
        )

:[Python]:

    .. code-block:: python
    
        def __init__(self, 
            face,                    # Face
            keyChain,                # KeyChain
            doVerify,                # bool
            defaultPipelineSize = 5, # int
            startingSeqNumber = 0    # int
        )

:[C#]:

    .. code-block:: c#
    
        public Bootstrap(
            Face face, 
            KeyChain keyChain, 
            bool doVerify, 
            int defaultPipelineSize = 5, 
            int startingSeqNumber = -1
        )
    
:Parameters:

    - `face`
        The face that AppConsumerSequenceNumber instance uses to issue interest.

    - `keyChain`
        The KeyChain that AppConsumerSequenceNumber instance uses to verify received data. It is recommended to use the KeyChain set up by Bootstrap, which is tracking updates in the trust schema.

    - `doVerify`
        Flag that controls whether receives data should be verified. It is recommended to use true but this can be disabled for testing purposes by setting to false.

    - `defaultPipelineSize`
        (Optional) Pipeline size for the interest pipeline this instance uses. Defaults to 5 if not present.
    
    - `startingSeqNumber`
        (Optional) Starting sequence number this instance uses to append to interest names. Non-negative numbers means starting to issue interest with that sequence number, while -1 means start with the first sequence number this instance can receive (with rightMostChild) from the network. Defaults to -1 if not present.

AppConsumerSequenceNumber.consume Methods
-------------------

AppConsumerSequenceNumber.consume
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
        The data prefix to consume data under: the expected full data name is prefix + sequence number.

    - `onVerified`
        If the data is successfully verified by KeyChain.verifyData call or doVerify flag is disabled, then this is called.

    - `onVerifyFailed`
        If doVerify flag is enabled and received data fails to validate, this is called with the data and reason (TODO: update to OnDataValidationFailed!)

    - `onTimeout`
        If an interest times out, this is called.

:Returns:

    Null
