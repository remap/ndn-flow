Gyroscope data simulator for Flow Unity application
=========================================

**How to use**

* nfd-start (installation guide: https://github.com/named-data/nfd)
* launch page, click "start simulating"
* open browser console, report if any errors 
* launch sample client that works with the simulator: [source](https://github.com/remap/ndn-flow/blob/master/framework/ndn_iot_dot_net/examples/test-sequential-consumer.cs), refer to [readme](https://github.com/remap/ndn-flow/tree/master/framework/ndn_iot_dot_net) for testing guide 

**Page Control** 

* _up arrow_, _down arrow_ for changing the roll
* _left arrow_, _right arrow_ for changing the yaw
* _w_, _s_ for changing the pitch

Initial values: https://github.com/remap/ndn-flow/blob/master/application/gyro-simulator/simulator.html#L28-L30

Step for each keypress: https://github.com/remap/ndn-flow/blob/master/application/gyro-simulator/simulator.html#L32