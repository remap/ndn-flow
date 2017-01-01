Raspberry Pi helper component for Flow application
========================

### Description

In Flow, Raspberry Pi helper works with RFduinos to request publishing authorization in their name, store RFduino's key pairs, receive their data, verify and repacketize (using the identity authorized by controller) to publish for the rest of the system.

A helper is introduced since constrained devices like RFduino is not powerful enough to do data signing using asymmetric keys, yet the system wants to secure the data they generate. In our case, the less constrained Raspberry Pi keeps the key pair for the RFduino it connects to and does the signing.

### Required devices

* Raspberry Pi 2 with (coming soon) image flashed
* BLE dongle
* Wifi dongle

### Development setup

* PyNDN2
* ndn-iot-python