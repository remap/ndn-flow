### [About Flow](http://github.com/remap/ndn-flow/wiki)

### Components in the installation:
0. Hardware and network setup (topology picture link)
1. Macbook running Unity
  * [ndn-cxx](https://github.com/named-data/ndn-cxx), [NFD](https://github.com/named-data/nfd)
  * [Unity application](https://github.com/remap/ndn-flow/blob/master/unity/README.md)
  * [PyNDN](https://github.com/named-data/PyNDN2), [IoT framework (Python device bootstrap, C# library)](https://github.com/remap/ndn-flow/tree/master/framework)
2. Ubuntu machine running OpenPTrack
  * [ndn-cxx](https://github.com/named-data/ndn-cxx), [NFD](https://github.com/named-data/nfd)
  * [ndn-cpp](https://github.com/named-data/ndn-cpp), [ndn-opt](https://github.com/OpenPTrack/ndn-opt/tree/master/publisher#how-to-use)
  * [PyNDN](https://github.com/named-data/PyNDN2), [IoT framework (Python device bootstrap, C++ library)](https://github.com/remap/ndn-flow/tree/master/framework)
  * [OpenPTrack installation and configuration](https://github.com/OpenPTrack/open_ptrack/wiki)
3. RFduino publishers
  * [Flow application code](https://github.com/remap/ndn-flow/blob/master/rfduino/rfduino-flow-producer/INSTALL.md)
4. Raspberry Pi gateway and RFduino helper
  * Download image
  * [Flash image](https://www.raspberrypi.org/documentation/installation/installing-images/)
5. User phones
  * Launch browser and visit webpage hosted at some static IP address

### Adding devices to the network and publishing authorization
How to run:
1. run script "ndn-iot-start", 
 - on controller run "ndn-iot-controller"
 - on opt producer, first run "ndn-iot-node", then run "opt-producer" (opt producer needs to be updated to read namespace from a configuration and require certain configured identity, if identity is not present, exit; if ndn-iot-node already gets back an identity and puts into identity storage, then opt producer uses it); ndn-iot-node runs discovery as well, for now, it can publish a configured name (and assume that the actual producer is running)
 - on phone, need to write the webpage for discovery and bootstrapping trust; Upon finding intended identity, the UI would allow user to publish
 - on RFduino, need to 

2. to add any components (except phone or rfduino), connect it to local wifi, run script “add”, which 
* reads in a conf file of prefix to use, and thing-level capability to request trust schema for
* starts nfd if not already started, registers "/prefix/devices/serial" prefix to the controller's IP
* generates device serial and pin

