Flow application components
============================

### Application components and content

1. Macbook running [Unity game engine](https://unity3d.com) / **unity** folder
  * What it does: visualizes a virtual environment which the player navigates via moving around the physical space (while being tracked by OpenPTrack), sending command on their mobile phone, and turning a gyroscope to face different angles
  * [Source code](https://github.com/remap/ndn-flow/tree/master/application/unity/WWBlimp)
2. Ubuntu machine running [OpenPTrack](http://openptrack.org/)
  * What it does: tracks the location of multiple persons in a physical environment, and publish the tracked coordinates as NDN data
  * Source code
    * [Flow Unity application](https://github.com/remap/ndn-flow/tree/master/application/unity/WWBlimp) for consumer
    * [ndn-opt](https://github.com/openptrack/ndn-opt) for producer)
3. RFduino/Gyroscope component / **rfduino** folder
  * What it does: controls the visualization by publishing the pitch, yaw and roll of a gyroscope as NDN data
  * [Source code](https://github.com/remap/ndn-flow/blob/master/rfduino/rfduino-flow-producer/INSTALL.md)
4. Mobile website component / **website** folder
  * What it does: controls the visualization by issueing NDN command interests to the Unity component
  * [Source code](https://github.com/remap/ndn-flow/blob/master/application/website/wwblimp.html)

Refer to the readme of each component for more details.