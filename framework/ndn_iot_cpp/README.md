NDN-IoT in C++
====================

### Compile and install
```
cd ndn_iot_cpp
./waf configure
./waf
sudo ./waf install
```

### Dependency
* Boost (>= 1.54) ASIO

* [ndn-cpp](https://github.com/named-data/ndn-cpp/blob/master/INSTALL.md) (soon to be updated to >= 0.11, for OnDataValidationFailed support; compiled with standard / boost shared pointers and functions; with **boost asio**)

* [rapidjson](https://github.com/miloyip/rapidjson) (please use latest version or clang may complain)

* (may need to regenerate protobuf files, for the specific version of protobuf installed)

* (On Ubuntu 14.04, may need to do)
```
sudo ln -s /usr/lib/x86_64-linux-gnu/ /usr/lib64
```

### Examples
* [Bootstrap - basic consumer](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_cpp/examples/test-consuming.cpp)

* [Bootstrap - basic producer](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_cpp/examples/test-producing.cpp)

* [Discovery](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_cpp/examples/test-discovery.cpp)

* Consumer - timestamp consumer (coming soon)

* Consumer - sequence number consumer (coming soon)

### Using in your code
Link against installed .dylib/.so, link and include flag include installed path