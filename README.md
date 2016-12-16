# ndn-flow

The repository for NDN-IoT framework, and "Flow" interactive NDN game in home environment.

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
    -  **rfduino** // _sample gyro data collector, rfduino publisher and Raspberry Pi helper_
    -  **unity** // _unity visualization component_
    -  **website** // _mobile webpage component_
 - **design** // _deprecated folder for design and interface documents_

Framework functionalities description (deprecated)

https://github.com/zhehaowang/ndnot-abstract