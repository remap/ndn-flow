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

