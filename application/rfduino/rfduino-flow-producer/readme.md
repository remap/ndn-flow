Gyroscope component for Flow application
========================

### Description

In Flow, RFduino with a gyroscope connected does the following

* BLE advertisement, connect to a Raspberry Pi helper
* read gyroscope data, packetizes as NDN data, send data to a Raspberry Pi helper
* (under development) receives and verifies public key secured via a shared secret from Raspberry Pi helper, and use that key to encrypt another secret generateed by this RFduino, and use that secret for HMAC signature of gyroscope data

### Required devices

* RFduino (rfd-22301)
* RFduino USB shield / RFduino battery shield
* MPU6050 gyroscope
* 2 * 10KOhm resistors
* Wires

### Development setup
* First follow [these instructions](https://github.com/RFduino/RFduino/blob/master/README.md) for RFduino development environment setup (tested with Arduino 1.6.9 with RFduino library 2.3.2)
* Then follow [installation guide](https://github.com/remap/ndn-flow/blob/master/application/rfduino/rfduino-flow-producer/INSTALL.md) (Thanks Jeff T)
* Follow instructions [here](http://www.rfduino.com/product/rfduino-6-axis-mpu-6050-accgyro-demo/) for connecting a MPU6050 gyroscope