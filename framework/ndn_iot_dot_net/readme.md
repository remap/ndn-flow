NDN-IoT in C\#
=====================

This page describes how to compile and install the C\# NDN-IoT framework, and examples to use it in your code.

### Compile
```
cd ndn_iot_dot_net
./waf configure
./waf
```

### Dependency
* [Mono framework](http://www.mono-project.com/download/), or Unity

* [ndn-dot-net](https://github.com/named-data/ndn-dot-net) (a copy is already included in this repository)

### Examples
* [Bootstrap - basic consumer](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_dot_net/examples/test-consuming.cs)

* [Bootstrap - basic producer](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_dot_net/examples/test-producing.cs)

* [Discovery](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_dot_net/examples/test-discovery.cs)

* [Consumer - timestamp consumer](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_dot_net/examples/test-timestamp-consumer.cs)

* [Consumer - sequence number consumer](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_dot_net/examples/test-sequential-consumer.cs)

* [Flow-specific application examples](https://github.com/remap/ndn-flow/tree/master/application/unity)

### To run an example:
```
cd build
mono [example-name.exe]
```

### Using in your code
Reference ndn-iot-dot-net.dll, ndn-dot-net.dll, and Mono.Data.Sqlite.dll when building; add dll to the same folder as compiled exe