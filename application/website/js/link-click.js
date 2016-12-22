var LINK_CLICK_COMMAND_VERB = "link";

/**
 * TrackMatcher takes a face configured by ndn-iot-js, with keychain and default certificate name
 * set up.
 */
var LinkClick = function LinkClick(commandInterestPrefix, mobileId, face) {
    this.commandInterestPrefix = commandInterestPrefix;
    this.mobileId = mobileId;
    this.face = face;
}

LinkClick.prototype.sendLinkClick = function(content, onResponse, onFailure) {
    var commandInterest = new Interest(new Name(this.commandInterestPrefix).append(LINK_CLICK_COMMAND_VERB).append(new Name(this.mobileId).toUri()).append(content));
    var self = this;
    this.face.makeCommandInterest(commandInterest, function() {
        console.log("made link click interest: " + commandInterest.getName().toUri());
        self.face.expressInterest(commandInterest, function(interest, data) {
            console.log("got link click data!");
            if (onResponse) {
                onResponse(data);
            }
        }, function(interest) {
            console.log("link response times out!");
            if (onFailure) {
                onFailure("link response times out!");
            }
        });
    });
}
