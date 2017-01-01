Mobile website component for Flow application
========================

### Description

In Flow, user-held mobile website controls dropping images to the Unity application, which should appear at a location corresponding to the user's location in the room as tracked by OpenPTrack.

Mobile website mainly does the following

 * send a command interest to Unity to match this mobile's identity with an observed track in OpenPTrack (this requires the user to be standing within the range of a predefined matching area)
 * send command interests to Unity afterwards, containing which image the user chose to drop

### Content

* **index.html**: sample code for Unity mobile component
* **wwblimp.html**: test Unity mobile website component