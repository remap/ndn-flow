import sys
import logging

from pyndn import Name, Data, Interest, Face
from pyndn.security import KeyChain

from ndn_iot_python.consumer.app_consumer import AppConsumer

# TODO: test case and example for this code

class AppConsumerTimestamp(AppConsumer):
    def __init__(self, face, keyChain, certificateName, doVerify, currentTimestamp = None):
        super(AppConsumerSequenceNumber, self).__init__(face, keyChain, certificateName, doVerify)

        self._pipelineSize = defaultPipelineSize
        self._emptySlot = defaultPipelineSize
        self._currentTimestamp = currentTimestamp

        self._defaultInterestLifetime = 4000
        return

    """
    public interface
    """
    def consume(self, prefix, onVerified, onVerificationFailed, onTimeout):
        name = Name(prefix)
        interest = Interest(name)
        interest.setInterestLifetimeMilliseconds(self._defaultInterestLifetime)

        if self._currentTimestamp:
            exclude = Exclude()
            exclude.appendAny()
            exclude.appendComponent(Name.Component.fromVersion(self._currentTimestamp))
            interest.setExclude(exclude)

        self._face.expressInterest(interest, 
          lambda i, d : self.onData(i, d, onVerified, onVerificationFailed, onTimeout), 
          lambda i: self.beforeReplyTimeout(i, onVerified, onVerificationFailed, onTimeout)))
        return

    """
    internal functions
    """
    def onData(self, interest, data, onVerified, onVerificationFailed, onTimeout):
        if self._doVerify:
            self._keyChain.verifyData(data, 
              lambda d : self.beforeReplyDataVerified(d, onVerified, onVerificationFailed, onTimeout), 
              lambda d : self.beforeReplyVerificationFailed(d, interest, onVerified, onVerificationFailed, onTimeout))
        else:
            self.beforeReplyDataVerified(data, onVerified, onVerificationFailed, onTimeout)
        return

    def beforeReplyDataVerified(self, data, onVerified, onVerificationFailed, onTimeout):
        # express next interest
        self._currentTimestamp = data.getName().get(-1).toVersion()
        self.consume(data.getName().getPrefix(-1), onVerified, onVerificationFailed, onTimeout)
        onVerified(data)
        return

    def beforeReplyVerificationFailed(self, data, interest, onVerified, onVerificationFailed, onTimeout):
        # for now internal to the library: verification failed cause the library to retransmit the interest after some time
        newInterest = Interest(interest)
        newInterest.refreshNonce()

        dummyInterest = Interest(Name("/local/timeout"))
        dummyInterest.setInterestLifetimeMilliseconds(self._verifyFailedRetransInterval)
        self.expressInterest(dummyInterest, 
          self.onDummyData, 
          lambda i: self.retransmitInterest(newInterest, onVerified, onVerificationFailed, onTimeout))
        onVerificationFailed(data)
        return

    def beforeReplyTimeout(self, interest, onVerified, onVerificationFailed, onTimeout):
        newInterest = Interest(interest)
        newInterest.refreshNonce()
        self.expressInterest(newInterest, 
          lambda i, d : self.onData(i, d, onVerified, onVerificationFailed, onTimeout), 
          lambda i: self.beforeReplyTimeout(i, onVerified, onVerificationFailed, onTimeout))
        onTimeout(interest)
        return

    def retransmitInterest(self, interest, onVerified, onVerificationFailed, onTimeout):
        self._face.expressInterest(interest, 
          lambda i, d : self.onData(i, d, onVerified, onVerificationFailed, onTimeout), 
          lambda i: self.beforeReplyTimeout(i, onVerified, onVerificationFailed, onTimeout))

    def onDummyData(self, interest, data):
        print "Unexpected: got dummy data!"
        return