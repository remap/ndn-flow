NDNoT Framework implementation
=====================

* **ndn-pi**: controller and add_device, based on the old ndn-pi (installation: setup.py)
* **ndn-iot-python**: bootstrap, discovery and consumer in Python (installation: setup.py)
* **ndn-iot-cpp**: bootstrap, discovery and consumer in cpp (compile and installation: waf)
* **ndn-iot-js**: add_device, bootstrap, discovery and consumer in JavaScript (browser only, include assembled js)
* **ndn-iot-dot-net**: add_device, bootstrap, discovery and consumer in C# (compile: custom script)

***** Worflow

1. Add a device to the network (ndn-pi)

Generate a key pair and certificate on added device, and have the certificate signed by home controller (gateway). Exchange of add device command and response is secured using a shared secret (manually key'ed in PIN for now)

2. Run application code (built based on bootstrap, consumer and discovery functionalities the framework provides)