**Application Example: matcher standalone**

To compile: make (please compile ndn-iot-dot-net first)

What to expect:
 - Matcher registers a prefix and waits for command interest which looks like "/home/flow-csharp/match/[_phone identity name_]"
 - Upon command interest arrival, matcher should check if anyone's within matching area, if so, match them and send back a response of who's matched with which track
Future data published by the phone should carry the same _phone identity name_ (either in payload or via signature name), so that matcher knows who published the data

To test the phone end, do
 * make sure accepted identities are set up in phone and local file system
 * mono matcher.exe
 * launch [this](https://github.com/remap/ndn-flow/blob/master/application/website/index.html), open console, and click "match"

To integrate the code, 
 * fill in interactions with Track object and track area definition [here](https://github.com/remap/ndn-flow/blob/master/application/unity/matcher-standalone/matcher.cs#L54-L58)
 * add the modified script as another class referenced by [the receiver class](https://github.com/remap/ndn-flow/blob/master/application/unity/structure/NDN.cs), like gyro script in the same folder