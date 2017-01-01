NDN-IoT Framework
=====================
### Functionalities
The framework libraries in Python, C++, JS and C\# all implement three major functionalities (files in each library are named according to the certain functionality):
* Bootstrap:
  * Identity and KeyChain set up: given a device identity (and optionally a controller identity), build a KeyChain and set up the default (device) certificate name for this instance
  * Consumer bootstrap: keep retrieving [the trust schema](https://named-data.net/wp-content/uploads/2015/11/schematizing_trust_ndn.pdf) of this application from the controller, and use that trust schema for verifying application data
  * Producer bootstrap: request authorization from the controller to publish under a certain application prefix (as of now controller grants the requests automatically if the device is trusted, and if granted, a trust schema rule suggesting this device's certificate can sign that application data is added and distributed to consumers)
* Discovery:
  * ([ChronoSync](http://irl.cs.ucla.edu/~zhenkai/papers/chronosync.pdf) with slight modifications) general name discovery based on multicast interest with name digests appended
* Application-level pub/sub:
  * Timestamp-based namespace (/prefix/[timestamp]) consumer: uses exclusion filter to repeatedly ask for content
  * Sequence-number-based namespace (/prefix/[consecutive sequence numbers]) consumer: pipelines interest for the next few sequence numbers
  * (Producer implementation should be straightforward using NDN CCL's memoryContentCache, or built-in examples that talk to repo-ng, thus not included in the framework implementation)

**Naming**
The framework generally supports arbitrary names given by the application, although the framework is developed with these levels of names (manufacture/bootstrap-level, device-level, and application-level) in mind, as suggested in these application [design slides](https://github.com/remap/ndn-flow/blob/master/design/Flow-design-zhehao-rev4.pptx) for Flow and the IoTDI '16 paper section VI.A.

### How to use
Typically, an application should set up the default identity and keyChain first, then pass these to application components (for example, discovery, or a timestamp-based consumer), and start application components when needed.

**A quick look (sequence number consumer with bootstrap in JavaScript)**
```JavaScript
    // bootstrap
    var bootstrap = new Bootstrap(face);
    bootstrap.setupDefaultIdentityAndRoot(new Name("/home/mobile-device1"), undefined, onSetupComplete, onSetupFailed);
    // in onSetupComplete, request trust schema update
    bootstrap.startTrustSchemaUpdate(new Name("/home/flow1"), onUpdateSuccess, onUpdateFailed);
    // in the first onUpdateSuccess, initialize a SequenceNumber consumer
    var consumer = new AppConsumerSequenceNumber(face, keyChain, true, 5, -1);
    consumer.consume(new Name("/home/flow1/some-data-prefix"), onVerified, onVerifyFailed, onTimeout);
```

More examples can be found in examples folder inside each framework language's subfolder.

And check [here](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_python/interface.md) for a set of library interfaces description (in Python) (being updated)

### Worflow
(what to do after building your application using the framework)

0. Set up [NFD](https://github.com/named-data/nfd) and forwarding on devices
Required devices include a controller (trust anchor of your home environment), and any number of other devices (which runs your application).

1. Add a device to the home network
This step registers a device with the home controller so that they trust each other. 
The device generates a key pair and certificate, and have the certificate signed by home controller (gateway). Message exchanges in this process is secured using a shared secret (a manually keyed in PIN code for now)
More details in the [ndn-pi technical report](https://named-data.net/wp-content/uploads/2015/11/ndn-0035-1-creating_secure_integrated.pdf).

2. Run your application code

### Supported platforms
Each framework library is built on top of a corresponding library in NDN common client libraries, should work with the latest versions of each, and should not introduce further dependencies.

**Controller**: controller code is only available in Python. Please pick a platform that supports Python 2.7. (Controller is tested on OS X 10.11, Raspbian Jessie and Ubuntu 14.04)

**Application devices**
 * **ndn-iot-dot-net** (C\# library): tested with Unity 5's DotNet 2.0, and mono's implementation of DotNet 4.5. Uses: [ndn-dot-net](https://github.com/named-data/ndn-dot-net)
 * **ndn-iot-js** (JavaScript library): tested with Chrome 55 and Firefox 50. Uses: [ndn-js](https://github.com/named-data/ndn-js)
 * **ndn-iot-cpp** (C++ library): uses: [ndn-cpp](https://github.com/named-data/ndn-cpp)
 * **ndn-iot-python** (Python library): uses [PyNDN2](https://github.com/named-data/PyNDN2)



(Updated Dec 31, 2016)