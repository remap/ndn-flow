Raspberry Pi helper component for Flow application
========================

This page describes the required devices and installation of the RaspberryPi helper for Flow application.

### Description

In Flow, Raspberry Pi helper works with RFduinos to request publishing authorization in their name, store RFduino's key pairs, receive their data, verify and repacketize (using the identity authorized by controller) to publish for the rest of the system.

### Required devices

* Raspberry Pi 2 with one at least 8GB microSD card, one BLE dongle and one Wifi dongle.

### Installation

* Flash the Raspberry Pi 2 following instructions [here](https://www.raspberrypi.org/documentation/installation/installing-images/)
* Set up Raspberry Pi wifi following instructions [here](https://www.raspberrypi.org/documentation/configuration/wireless/wireless-cli.md)
* (Optional) reset the keyboard layout in /etc/default/keyboard if not "uk/gb" ([related thread](http://raspberrypi.stackexchange.com/questions/1042/why-is-my-symbol-not-working)) 
* Update Flow application code to the latest. On the Pi, do
```
cd ~/ndn/ndn-flow
git pull
```