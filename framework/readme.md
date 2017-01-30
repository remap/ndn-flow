NDN-IoT Framework
=====================

This page describes the overall functionalities, how to use, and the workflow of the NDN-IoT framework, which we used to build [the Flow application](https://github.com/remap/ndn-flow/tree/master/application).

This page frequently refers to specific sections of the NDN team's IoTDI '16 paper, available [here](https://named-data.net/wp-content/uploads/2015/01/ndn-IOTDI-2016.pdf).

### Functionalities

The framework libraries in Python, C++, JS and C\# all implement three major functionalities (files in each library are named according to the certain functionality):
* **Bootstrap** (suggested in section VI.B and VI.C of the IoTDI '16 paper), main abstractions include:
  * Identity and KeyChain set up: given a device identity (and optionally a controller identity), build a KeyChain and set up the default (device) certificate name for this instance
  * Consumer bootstrap: keep retrieving [the trust schema](https://named-data.net/wp-content/uploads/2015/11/schematizing_trust_ndn.pdf) of this application from the controller, and use that trust schema for verifying application data
  * Producer bootstrap: request authorization from the controller to publish under a certain application prefix (as of now controller grants the requests automatically if the device is trusted, and if granted, a trust schema rule suggesting this device's certificate can sign that application data is added and distributed to consumers)
* **Discovery** (a sync-based discovery different from suggested in section VI.B of the IoTDI '16 paper):
  * ([ChronoSync](http://irl.cs.ucla.edu/~zhenkai/papers/chronosync.pdf) with slight modifications) general name discovery based on multicast interest with name digests appended
* **Application-level pub/sub** (suggested in section VI.F of the IoTDI '16 paper), major abstractions include:
  * Timestamp-based namespace (/prefix/[timestamp]) consumer: uses exclusion filter to repeatedly ask for content
  * Sequence-number-based namespace (/prefix/[consecutive sequence numbers]) consumer: pipelines interest for the next few sequence numbers
  * Producer implementation should be straightforward using NDN CCL's memoryContentCache, or built-in examples that communicate with repo-ng, thus not included in the framework implementation

For more details on the functionalities of the framework, check [here](https://github.com/remap/ndn-flow/blob/master/design/docs) for a set of library interface descriptions, and [here](https://github.com/remap/ndn-flow/blob/master/design/Flow-design-zhehao-rev4.pptx) for the design slides.

#### Naming

While the framework could support arbitrary names given by the application, it is developed with three levels of names (manufacture/bootstrap-level, device-level, and application-level) in mind, the latter two as suggested in the IoTDI '16 paper section VI.A.

More specifically, we picture the different levels of names to have the following functionalities

* **Application/thing level**: name the thing
  * An application-specific “label” given by the user
  * Used for the exchange of application data
* **Device level**: name the device in a space
  * Associated with some physical property of the device to identify the device in a space
  * Used for device control and status feedback
* **Manufacturer level**: name given by the manufacturer
  * The name of device given by the manufacturer when produced
  * Used for initial device verification, and manufacturer-based querying

The framework also doesn't restrain the name prefix: one can use a globally unique network-related ID, e.g. "/ucla/melnitz-hall/room-1469", or a local one for local environment e.g. "/home/living-room". To bridge with global Internet using a local name, mechanisms such as forwarding hints or encapsulation would be required.

### How to use

Typically, an application should instantiate a Bootstrap object first, and use it to set up the default identity and keyChain, then pass the Face, Identity, and KeyChain to each application component (for example, discovery, or a timestamp-based consumer), and start those components when needed.

#### A quick look

The following code sets up a sequence number consumer in JavaScript.

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

#### More examples and interface description

More examples can be found in examples folder inside each framework language's subfolder.

Check [here](https://github.com/remap/ndn-flow/blob/master/design/docs) for a set of library interface descriptions.

### Workflow

(what to do after building your application using the framework)

0. Set up [NFD](https://github.com/named-data/nfd) and forwarding on devices
Required devices include a controller (trust anchor of your home environment), and any number of other devices (which runs your application).

1. Add a device to the home network
This step registers a device with the home controller so that they trust each other. 
The device generates a key pair and certificate, and have the certificate signed by home controller (gateway). Message exchanges in this process is secured using a shared secret (a manually keyed in PIN code for now)
More details in the [ndn-pi technical report](https://named-data.net/wp-content/uploads/2015/11/ndn-0035-1-creating_secure_integrated.pdf).

2. Run your application code

### Folder structure
  -  **commands** // _source protobuf files for command exchanges in the framework_
  -  **ndn\_iot\_cpp** // _library in C++_
  -  **ndn\_iot\_dot\_net** // _library in C\#_
  -  **ndn\_iot\_js** // _library and device bootstrapping in JS_
  -  **ndn\_iot\_python** // _library in Python_
  -  **ndn\_pi** // _device bootstrapping and controller in Python (mostly inherited from [ndn-pi](https://github.com/remap/ndn-pi)

### Supported platforms
Each framework library is built on top of a corresponding library in NDN common client libraries, should work with the latest versions of each, and should not introduce further dependencies.

**Controller**: controller code is only available in Python. Please pick a platform that supports Python 2.7. (Controller is tested on OS X 10.11, Raspbian Jessie and Ubuntu 14.04)

**Application devices**
 * **ndn-iot-dot-net** (C\# library): tested with Unity 5's DotNet 2.0, and mono's implementation of DotNet 4.5. Uses: [ndn-dot-net](https://github.com/named-data/ndn-dot-net)
 * **ndn-iot-js** (JavaScript library): tested with Chrome 55 and Firefox 50. Uses: [ndn-js](https://github.com/named-data/ndn-js)
 * **ndn-iot-cpp** (C++ library): uses: [ndn-cpp](https://github.com/named-data/ndn-cpp)
 * **ndn-iot-python** (Python library): uses [PyNDN2](https://github.com/named-data/PyNDN2)

### License
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
A copy of the GNU Lesser General Public License is in the file COPYING.


(Updated Jan 30, 2016)