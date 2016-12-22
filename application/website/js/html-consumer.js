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
}

HtmlConsumer.prototype.consume = function(onComplete) {
    console.log(this.baseInterest.toUri());
    SegmentFetcher.fetch
      (this.face, this.baseInterest, null,
        function(content) {
            onComplete(content);
        },
        function(errorCode, message) {
            console.log(message);
        });
}