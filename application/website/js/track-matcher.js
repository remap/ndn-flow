var TRACK_MATCH_COMMAND_VERB = "match";

/**
 * TrackMatcher takes a face configured by ndn-iot-js, with keychain and default certificate name
 * set up.
 */
var TrackMatcher = function TrackMatcher(commandInterestPrefix, mobileId, face) {
    this.commandInterestPrefix = commandInterestPrefix;
    this.mobileId = mobileId;
    this.face = face;
}

TrackMatcher.prototype.sendTrackMatchCommand = function(onMatch, onMatchFailed) {
    var commandInterest = new Interest(new Name(this.commandInterestPrefix).append(TRACK_MATCH_COMMAND_VERB).append(new Name(this.mobileId).toUri()));
    var self = this;
    this.face.makeCommandInterest(commandInterest, function() {
        console.log("made match command interest: " + commandInterest.getName().toUri());
        self.face.expressInterest(commandInterest, function(interest, data) {
            console.log("got match command interest data!");
            // TODO: insert in verification process: prerequisite library update with BasicIdentityStorage
            var content = JSON.parse(data.getContent().buf());
            if (content["status"] !== undefined) {
                if (content["status"] == "200") {
                    console.log("match established: trackId " + content["trackId"] + " with mobileId " + content["mobileId"]);
                    if (onMatch) {
                        onMatch(content["trackId"], content["mobileId"]);
                    }
                } else if (content["status"] == "404") {
                    console.log("cannot establish match: no tracks currently in \"matching zone\"");
                    if (onMatchFailed) {
                        onMatchFailed("cannot establish match: no tracks currently in \"matching zone\"");
                    }
                } else if (content["status"] == "409") {
                    console.log("cannot establish match: multiple tracks currently in \"matching zone\"");
                    if (onMatchFailed) {
                        onMatchFailed("cannot establish match: multiple tracks currently in \"matching zone\"");
                    }
                }
            }
        }, function(interest) {
            console.log("match command interest times out!");
            if (onMatchFailed) {
                onMatchFailed("match command interest times out!");
            }
        });
    });
}

TrackMatcher.prototype.queryStatus = function() {

}

