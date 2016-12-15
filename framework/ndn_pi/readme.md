NDN-Pi: NDN controller for Flow application
==========================

** Dependency **
[PyNDN2](https://github.com/named-data/PyNDN2) installed

** How to use **

* start nfd
* run controller:

<pre>
git clone https://github.com/remap/ndn-flow
cd ndn-flow/framework/
export PYTHONPATH=$PYTHONPATH:$(pwd)
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