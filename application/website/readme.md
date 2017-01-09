Mobile website component for Flow application
========================

This page describes the functionalities and installation of mobile-website-related components of Flow. 

### Description

In Flow, user-held mobile website controls dropping images to the Unity application, which should appear at a location corresponding to the user's location in the room as tracked by OpenPTrack.

Mobile website does the following

 * send a command interest to Unity to match this mobile's identity with an observed track in OpenPTrack (this requires the user to be standing within the range of a predefined matching area)
 * send command interests to Unity afterwards, containing which image the user chose to drop

### Content

* **index.html**: sample code for Unity mobile component
* **wwblimp.html**: test Unity mobile website component

### Installation

* Copy the contents of this folder, and put them into a running web server 

### How to use

* Open wwblimp.html via the latest Firefox on an Android browser
* Choose which NFD to connect to, and hit connect. Please make sure NFD's running on that machine, routes are set up correctly, and localhop registration on that NFD is enabled
* Run OpenPTrack and the Unity application