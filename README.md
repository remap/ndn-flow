NDN-IoT framework and Flow application
=========================

This repository contains two major components:

**Named Data Networking of Things (NDN-IoT) framework**: a set of libraries in JavaScript, Python, C\# and C++ that implement the naming, trust and bootstrap, discovery and application-level pub/sub functionalities in the NDN team's [IoTDI '16 paper](https://named-data.net/wp-content/uploads/2015/01/ndn-IOTDI-2016.pdf). 

The libraries are built on top of [NDN Common Client Libraries](http://named-data.net/doc/ndn-ccl-api/), and provides further abstractions to facilitate application development in a home IoT environment.

See [framework](framework) folder for more details.

**"Flow" application**: a home IoT game application built on top of the IoT framework. 

A player interacts with the game by walking around in an area tracked by [OpenPTrack](http://openptrack.org/), and see his physical tracks affect the terrain in a virtual space rendered by [Unity3D](https://unity3d.com/) game engine. The player can also drop images to the virtual space using a mobile webpage, or control the angles the virtual cameras are facing by rotating a gyroscope.

See [application](application) folder for more details.

### Contact

Zhehao <zhehao@remap.ucla.edu> (framework and application sample code)

Eitan <eitanm@gmail.com> (Unity application)