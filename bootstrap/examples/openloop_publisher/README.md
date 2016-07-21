
Openloop Publisher
============================


This example demos the openloop publisher per Qualcomm's request.

Node Types
----------

### Controller

The controller in this network has no special function: it listens for certificate requests and device listing requests.

### Publisher Node
    
This node reads from a PIR attached to a GPIO pin (by default, 25) at 1Hz rate.
The publisher chirps data to a face created with a UDP multicast address that NFD listens on, without necessarily receiving interest

### Consumer

The consumer issues interest for published content, and inserts it into a local repo.
Repo watched insertion can also serve as a consumer.

### In-browser visualizer

An in-browser visualizer of repo's data, using NDN-JS, is also included.

Setup
-------

No special hardware setup is needed for this example.

### Network Setup      
See the README.md in (ndn-pi path?) for NDN setup steps.    

Running the Example
-------------------

The controller node should be started, using:

        ./ndn-iot-controller

The publisher can be started using

        sudo python publisher/publisher.py

Finally, we run a consumer using:

        python consumer/test_udp_multicast_consumer_outstanding.py

The in-browser visualizer, browser-consumer/index.html can be launched in browser.
A copy of the index.html page is left at /var/www/browser-consumer/index.html by default on this image.

More to come on the subject
