# Using OpenTrack in Unity via NDN

These instructions assume that you have a working installation of OpenPTrack using a NDN producer.

## Unity Installation
1. [Install the NDN C# libraries][1]
2. Download and install [SimpleJSON][2] into your Unity project.  (See the *Usage* section for instructions and the *Download* section for download link.)
2. Add `OpenPTrackConsumer.cs`, `OpenPTrackListener.cs`, and `Track.cs` to your project’s assets.
3. Attach `OpenPTrackConsumer.cs` to an object.

## Test Demo
You may attached `MoveFromTrack.cs` to an object in unity.  It will cause the object’s position to mirror the first track detected after running the project.  (Make sure OpenPTrack and NFD are running.)

[1]:	../READMEN.md
[2]:	http://wiki.unity3d.com/index.php/SimpleJSON