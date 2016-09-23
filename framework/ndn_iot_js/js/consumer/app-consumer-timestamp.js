var AppConsumerTimestamp = function AppConsumerTimestamp
  (face, keyChain, certificateName, doVerify, currentTimestamp)
{
    AppConsumer.call(this, face, keyChain, certificateName, doVerify);

    this.currentTimestamp = currentTimestamp;

    this.verifyFailedRetransInterval = 4000;
    this.defaultInterestLifetime = 4000;
}

// public interface
AppConsumerTimestamp.prototype.consume = function
  (prefix, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;

    var name = (new Name(prefix)).append(this.currentSeqNumber.toString());
    var interest = new Interest(name);
    interest.setInterestLifetimeMilliseconds(this.defaultInterestLifetime);

    if (this.currentTimestamp) {
        var exclude = new Exclude();
        exclude.appendAny();
        exclude.appendComponent(Name.Component.fromVersion(this.currentTimestamp));
        interest.setExclude();
    }

    this.face.expressInterest(interest, function (i, d) {
        self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
    }, function (i) {
        self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
    });
    return;
}

// internal functions
AppConsumerTimestamp.prototype.onData = function
  (interest, data, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    if (this.doVerify) {
        this.keyChain.verifyData(data, function (d) {
            self.beforeReplyDataVerified(d, onVerified, onVerifyFailed, onTimeout);
        }, function (d) {
            self.beforeReplyVerificationFailed(d, interest, onVerified, onVerifyFailed, onTimeout);
        });
    } else {
        this.beforeReplyDataVerified(data, onVerified, onVerifyFailed, onTimeout);
    }
    return;
}

AppConsumerTimestamp.prototype.beforeReplyDataVerified = function
  (data, onVerified, onVerifyFailed, onTimeout)
{
    this.currentTimestamp = data.getName().get(-1).toVersion();
    this.consume(data.getName().getPrefix(-1), onVerified, onVerifyFailed, onTimeout);
    this.onVerified(data);
    return;
}

AppConsumerTimestamp.prototype.beforeReplyVerificationFailed = function
  (data, interest, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    // for now internal to the library: verification failed cause the library to retransmit the interest after some time
    var newInterest = new Interest(interest);
    newInterest.refreshNonce();

    var dummyInterest = new Interest(Name("/local/timeout"));
    dummyInterest.setInterestLifetimeMilliseconds(this._verifyFailedRetransInterval)
    this.face.expressInterest(dummyInterest, this.onDummyData, function (i) {
        self.retransmitInterest(newInterest, onVerified, onVerifyFailed, onTimeout);
    };
    this.onVerifyFailed(data);
    return;
}

AppConsumerTimestamp.prototype.beforeReplyTimeout = function 
  (interest, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    var newInterest = new Interest(interest);
    newInterest.refreshNonce();
    
    this.face.expressInterest(newInterest, function (i, d) {
        self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
    }, function (i) {
        self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
    });
    this.onTimeout(interest);
    return;
}

AppConsumerTimestamp.prototype.retransmitInterest = function
  (interest, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    this.face.expressInterest(interest, function (i, d) {
        self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
    }, function (i) {
        self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
    });
}
        
AppConsumerTimestamp.prototype.onDummyData = function
  (interest, data)
{
    console.log("Got unexpected dummy data");
    return;
}
        
