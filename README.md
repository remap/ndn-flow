# ndn-flow

The repository for:

**NDN-IoT framework**: a set of libraries in JavaScript, Python, C\# and C++ that implement the naming, trust and bootstrap, discovery and application-level pub/sub mentioned in the NDN team's [IoTDI '16 paper](https://named-data.net/wp-content/uploads/2015/01/ndn-IOTDI-2016.pdf), built on top of [NDN Common Client Libraries](http://named-data.net/doc/ndn-ccl-api/).

**"Flow" application**: a home entertainment application built on top of the IoT framework. The game runs in Unity, and features [OpenPTrack](http://openptrack.org/), mobile website, Raspberry Pis and RFduinos subsystems.

### Folder structure
 - **framework** // _code for ndn-iot libraries_
    -  **commands** // _source protobuf files for command exchanges in the framework_
    -  **ndn\_iot\_cpp** // _library in C++_
    -  **ndn\_iot\_dot\_net** // _library in C\#_
    -  **ndn\_iot\_js** // _library and device bootstrapping in JS_
    -  **ndn\_iot\_python** // _library in Python_
    -  **ndn\_pi** // _device bootstrapping in Python (mostly inherited from [ndn-pi](https://github.com/remap/ndn-pi))_
 - **application** // _"Flow" application code_
    -  **gyro-simulator** // _browser publisher that publishes similar data as gyros_
    -  **rfduino** // _gyroscope data collector, rfduino publisher and Raspberry Pi helper_
    -  **unity** // _unity game engine visualization component_
    -  **website** // _mobile webpage component_
 - **design** // _folder for design slides and interface documents_

### Contact
Zhehao <zhehao@remap.ucla.edu>