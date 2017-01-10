RFduino-related component for Flow application
========================

This page describes the RFduino-related functionalities for the Flow application, including the RFduino gyroscope publisher, and RaspberryPi helper. 

### Functionality

The RFduino produces the facing (pitch, yaw, roll) of the gyroscopes and send them as NDN data to the Raspberry Pi helper via Bluetooth. The Raspberry Pi helper repackizes the data, sign them using keys it stored on behalf of the RFduinos, and publish the resulting NDN data packet for Unity application to consume.

A helper is introduced since constrained devices like RFduino is not powerful enough to do data signing using asymmetric keys, yet the system wants to secure the data they generate. In our case, the less constrained Raspberry Pi keeps the key pair for the RFduino it connects to and does the signing.

### Content

* **rfduino-flow-producer**
  * RFduino gyroscope data producer
  * Refer to its [readme](rfduino-flow-producer) for details and installation guide
* **rpi_helper**
  * Helper Raspberry Pi Python script
  * Refer to its [readme](rpi_helper) for details and installation guide 