Flow Design Diagrams
=======================

This page describes design diagrams / flow charts which explain the bootstrapping steps and data namespace of the Flow application.

### Application bootstrap flow charts
* [Add device process](add-device-sequence.graffle): this diagram shows the messages exchanged between controller and added device in the first step of bootstrap: adding the device to the home network.

* [Request producing authorization / request application trust schema](authorize-producer-consumer.graffle): this diagram shows the messages exchanged in the second step of bootstrap: requesting producing authorization as a producer, or requesting application trust schema as a consumer.

### Name diagrams in a running Flow application
* [Application components with NDN names](flow-components-ndn-names.graffle): this diagram shows after the bootstrap (refer to [workflow](https://github.com/remap/ndn-flow/tree/master/framework#worflow) for more details) is done, what interest(I) and data(D) names Flow application uses by default. Arrows denote the direction of an interest. Each component has a name in the device namespace, as well as application prefixes if they serve as producers in the Flow application.
  * OpenPTrack subsystem (1a, 1b): 1a shows the interest and data names when Unity asks for a track_hint. 1b shows the names when asking for a track (See more details in [ndn-opt readme](https://github.com/OpenPTrack/ndn-opt/tree/master/publisher#ndn-namespace))
  * Mobile phone subsystem (2a, 2b): 2a shows the command interest that the phone sends, when initiating a "track matching" (the process in which a phone's ID is matched with a Track ID provided by opt). 2b shows the command interest that the phone sends to control image dropping in Unity.
  * RFduinos/Gyroscope subsystem (3a): 3a shows the interest name that Unity uses to ask for gyroscope data, produced by RaspberryPi helper on behalf of RFduinos connected to it via Bluetooth LE.

* [Overall application namespace](flow-namespace.graffle): this diagram shows the overall namespace tree of the Flow application.

