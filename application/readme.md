Flow application components
============================

### Components and content

* [Unity game engine](https://github.com/remap/ndn-flow/tree/master/application/unity/WWBlimp) / **unity** folder: visualization of a virtual environment which the player navigates via moving around the physical space (while being tracked by OpenPTrack), sending command on their mobile phone, and turning a gyroscope to face different angles
* [OpenPTrack](http://openptrack.org/) (refer to [Flow Unity application](https://github.com/remap/ndn-flow/tree/master/application/unity/WWBlimp) for consumer, and [ndn-opt](https://github.com/openptrack/ndn-opt) for producer): tracks the location of multiple persons in a physical environment, and publish the tracked coordinates as NDN data
* Gyroscope component / **rfduino** folder: controls the visualization by publishing the pitch, yaw and roll of a gyroscope as NDN data
* Mobile website component / **website** folder: controls the visualization by issueing NDN command interests to the Unity component

Refer to the readme of each component for more details.