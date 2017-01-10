**Application Example: opt standalone**

To compile: make (please compile ndn-iot-dot-net first)

TODO: test with opt instance in office

What to expect:

To integrate the code, 
 * fill in interactions with Track object and track area definition [here](https://github.com/remap/ndn-flow/blob/master/application/unity/matcher-standalone/matcher.cs#L54-L58)
 * add the modified script as another class referenced by [the receiver class](https://github.com/remap/ndn-flow/blob/master/application/unity/structure/NDN.cs), like gyro script in the same folder