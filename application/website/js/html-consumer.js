var HTML_FETCH_COMMAND_VERB = "fetch";

/**
 * HtmlConsumer takes a face configured by ndn-iot-js, with keychain and default certificate name
 * set up.
 */
var HtmlConsumer = function HtmlConsumer(prefix, mobileId, face, keyChain) {
    this.prefix = prefix;
    this.mobileId = mobileId;
    this.face = face;
    this.keyChain = keyChain;
    this.baseInterest = new Interest(new Name(prefix).append(HTML_FETCH_COMMAND_VERB).append(new Name(this.mobileId).toUri()));
    this.baseInterest.setInterestLifetimeMilliseconds(4000);
}

HtmlConsumer.prototype.consume = function(onComplete, repeatWithExclusion) {
    var self = this;
    var repeat = repeatWithExclusion;
    if (repeat === undefined) {
        repeat = true;
    }
    console.log("fetch: " + this.baseInterest.getName().toUri());
    console.log("exclude: " + this.baseInterest.getExclude().toUri());
    SegmentFetcherDataName.fetch
      (this.face, this.baseInterest, null,
        function (dataName, content) {
            if (repeat) {
                self.completeAndExpress(onComplete, dataName, content);
            } else {
                onComplete(content);
            }
        },
        function (errorCode, message) {
            console.log(errorCode + " : " + message);
            if (repeat) {
                console.log("retrying");
                self.timeoutAndRetry(onComplete);
            }
        });
}

HtmlConsumer.prototype.completeAndExpress = function (onComplete, dataName, content) {
    onComplete(content);
    var versionComponent = dataName.get(this.baseInterest.getName().size());
    var exclude = new Exclude();
    exclude.appendAny();
    exclude.appendComponent(versionComponent);
    this.baseInterest.setExclude(exclude);
    this.consume(onComplete, true);
}

HtmlConsumer.prototype.timeoutAndRetry = function (onComplete) {
    this.consume(onComplete, true);
}