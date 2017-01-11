NDN-Pi: NDN controller for NDN-IoT framework
==========================

This page describes how to install and use the NDN-pi controller and add-device script.

### Dependency

* [Protobuf2](https://pypi.python.org/pypi/protobuf/2.5.0) installed
* [PyNDN2](https://github.com/named-data/PyNDN2) installed

### Supported platforms

* Ubuntu 14.04
* OSX 10.10, 10.11
* Raspbian Jessie (if you want to use this code on a Raspberry Pi, a copy is included in the Pi image here. Follow the instructions here for using that image.)

### How to use

* start nfd
* run controller:

<pre>
git clone https://github.com/remap/ndn-flow
cd ndn-flow/framework/
export PYTHONPATH=$PYTHONPATH:$(pwd)
</pre>

If this is the first time you run controller on this device, do the following
<pre>
cp iot_controller.conf.sample ~/.ndn/iot_controller.conf
</pre>

otherwise, do
<pre>
cd ndn_pi
python iot_controller
</pre>
* run add_device script on the device you wish this controller to authorize, for example, spawn another terminal window and run the cd and export commands from above, then

<pre>
cd add_device
python add_device.py
</pre>

In controller terminal window, paste the serial and pin of the new device (which is printed in add_device terminal window), and give the newly added device a name, such as "/home/flow-csharp".

The controller should send pair command to the device (uses PIN as shared secret to validate), device reply with a self-signed certificate, and controller sign that certificate and give it back to device to finish the bootstrapping process.

After device bootstrapping, you should be able to use device identities which you gave in controller terminal window (in our example, that's "/home/flow-csharp")

(In Ubuntu / OSX, you may need to give 644 permission to python protobuf3 library in site-packages so that running the controller or add\_device does not require su)

(In OSX El Capitan, the permission for users packages path ~/Library/Python/... may be wrong. If so, try 755 for that path)