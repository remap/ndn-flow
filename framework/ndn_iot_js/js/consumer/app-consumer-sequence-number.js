var AppConsumerSequenceNumber = function AppConsumerSequenceNumber
  (face, keyChain, certificateName, doVerify, defaultPipelineSize, startingSeqNumber)
{
    if (defaultPipelineSize === undefined) {
        defaultPipelineSize = 5;
    }
    if (startingSeqNumber === undefined) {
        startingSeqNumber = 0;
    }
    AppConsumer.call(this, face, keyChain, certificateName, doVerify);

    this.pipelineSize = defaultPipelineSize;
    this.emptySlot = defaultPipelineSize;
    this.currentSeqNumber = startingSeqNumber;

    this.verifyFailedRetransInterval = 4000;
    this.defaultInterestLifetime = 4000;
}

// public interface
AppConsumerSequenceNumber.prototype.consume = function
  (prefix, onVerified, onVerifyFailed, onTimeout)
{
    var num = this.emptySlot;
    var self = this;
    for (var i = 0; i < emptySlot; i++) {
        var name = (new Name(prefix)).append(this.currentSeqNumber.toString());
        var interest = new Interest(name);
        interest.setInterestLifetimeMilliseconds(this.defaultInterestLifetime);
        this.face.expressInterest(interest, function (i, d) {
            self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
        }, function (i) {
            self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
        });
        this.currentSeqNumber -= 1;
        this.emptySlot += 1;
    }
    return;
}

// internal functions
AppConsumerSequenceNumber.prototype.onData = function
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

AppConsumerSequenceNumber.prototype.beforeReplyDataVerified = function
  (data, onVerified, onVerifyFailed, onTimeout)
{
    // fill the pipeline
    this.currentSeqNumber += 1;
    this.emptySlot += 1;
    this.consume(data.getName().getPrefix(-1), onVerified, onVerifyFailed, onTimeout);
    this.onVerified(data);
    return;
}

AppConsumerSequenceNumber.prototype.beforeReplyVerificationFailed = function
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
    });
    this.onVerifyFailed(data);
    return;
}

AppConsumerSequenceNumber.prototype.beforeReplyTimeout = function 
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

AppConsumerSequenceNumber.prototype.retransmitInterest = function
  (interest, onVerified, onVerifyFailed, onTimeout)
{
    var self = this;
    this.face.expressInterest(interest, function (i, d) {
        self.onData(i, d, onVerified, onVerifyFailed, onTimeout);
    }, function (i) {
        self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout);
    });
}
        
AppConsumerSequenceNumber.prototype.onDummyData = function
  (interest, data)
{
    console.log("Got unexpected dummy data");
    return;
}
        
