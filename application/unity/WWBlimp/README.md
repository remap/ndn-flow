WWBlimp: Unity application for Flow
=========================

### Using NDN in Unity

The following installation was tested on Unity version 5.3.2.f1 Personal running on Mac OS X version 10.11.5.

### Installation

1. [Install NFD][1]
2. Create a Unity project
3. Add [`ndn-dot-net.dll`][2] to the project’s assets.
4. Add [SimpleJSON][3] to the project's assets.

[1]:	https://github.com/named-data/NFD/blob/master/docs/INSTALL.rst
[2]:	https://github.com/named-data/ndn-dot-net/blob/master/bin/ndn-dot-net.dll
[3]:	http://wiki.unity3d.com/index.php/SimpleJSON#Download

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
