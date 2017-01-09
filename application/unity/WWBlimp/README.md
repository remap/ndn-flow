WWBlimp: Unity application for Flow
=========================

This page describes how to install and use the Flow Unity application, and what to expect from it. 

### Using NDN in Unity

The following installation was tested on Unity version 5.3.2.f1 Personal running on Mac OS X version 10.11.5.

### Dependencies

* [ndn-cxx](https://github.com/named-data/ndn-cxx), [NFD](https://github.com/named-data/nfd)
* [PyNDN](https://github.com/named-data/PyNDN2), [IoT framework (Python device bootstrap, C# library)](https://github.com/remap/ndn-flow/tree/master/framework) 

### Installation

1. Install [dependencies](#Dependencies)
2. Add the Unity project in this folder to Unity

### What to expect

There are 4 main components:

 * unity (ndn-flow/application/unity/WWBlimp)
 * openptrack
 * gyros
 * mobiles (running web browser viewing ndn-flow/application/website/wwblimp.html )


The view is in unity is looking down on a terrain from a "blimp."  The terrain deforms and people in the space can drop images using their mobiles.

The terrain in unity is modified by tracks from openptrack.  For things to line up the dimensions of the interactive space from openptrack need to be set in unity property inspector:

 * Terrain - OpenPTrackProvider/MinTrackDims and Terrain/OpenPTrackProvider/MaxTrackDims

The movement of blimp is controlled by the gyros.  The range of the gyro values and how sensitive the blimp should be to them can be set in the property inspector under: Blimp - NDN_GYRO.  There are two of them - one for gyro controlling the orientation of the blimp and one for the gyro controlling the throttle.

Mobiles should start out by viewing ndn-flow/application/website/wwblimp.html.  The mobile is associated with a track by the user moving to a set location in the interaction space and clicking the link on the screen.  The location (and radius) used to match the mobile with a track need to be set in the property inspector:

 * Terrain - TrackDeviceManager/TrackDeviceMatching

If the user clicks on the link and the match works (they are standing in the right spot) then they are given a list of images.  They can click on the image name and it will be dropped onto the terrain from a virtual location corresponding to their tracked location in the installation space.
