import sys
import logging

from pyndn import Name, Data, Interest, Face
from pyndn.security import KeyChain

from ndn_iot_python.consumer.app_consumer import AppConsumer

# TODO: test case and example for this code

class AppConsumerTimestamp(AppConsumer):
    """
    Timestamp based consumer with exclusion filters

    :param face: the face to consume data with
    :type face: Face
    :param keyChain: the keyChain to verify received data with
    :type keyChain: KeyChain
    :param certificateName: the certificate name to sign data with
      (not used by default for consumers)
    :type certificateName: Name
    :param doVerify: flag for whether the consumer should skip verification
    :type doVerify: bool
    :param currentTimestamp: the timestamp to start excluding with
    :type currentTimestamp: int
    """
    def __init__(self, face, keyChain, certificateName, doVerify, currentTimestamp = None):
        super(AppConsumerSequenceNumber, self).__init__(face, keyChain, certificateName, doVerify)

        self._pipelineSize = defaultPipelineSize
        self._emptySlot = defaultPipelineSize
        self._currentTimestamp = currentTimestamp
        
        self._verifyFailedRetransInterval = 4000
        self._defaultInterestLifetime = 4000
        return

    """
    public interface
    """
    def consume(self, prefix, onVerified, onVerifyFailed, onTimeout):
        """
        Consume data continuously under a given prefix, each time sending interest with
        the last timestamp excluded

        :param name: prefix to consume data under
        :type name: Name
        :param onData: onData(data) gets called after received data's onVerifyFailed
        :type onData: function object
        :param onVerifyFailed: onVerifyFailed(data) gets called if received data 
          cannot be verified
        :type onVerifyFailed: function object
        :param onTimeout: onTimeout(interest) gets called if a consumer interest times out
        :type onTimeout: function object
        """
        name = Name(prefix)
        interest = Interest(name)
        interest.setInterestLifetimeMilliseconds(self._defaultInterestLifetime)

        if self._currentTimestamp:
            exclude = Exclude()
            exclude.appendAny()
            exclude.appendComponent(Name.Component.fromVersion(self._currentTimestamp))
            interest.setExclude(exclude)

        self._face.expressInterest(interest, 
          lambda i, d : self.onData(i, d, onVerified, onVerifyFailed, onTimeout), 
          lambda i: self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout))
        return

    """
    internal functions
    """
    def onData(self, interest, data, onVerified, onVerifyFailed, onTimeout):
        if self._doVerify:
            self._keyChain.verifyData(data, 
              lambda d : self.beforeReplyDataVerified(d, onVerified, onVerifyFailed, onTimeout), 
              lambda d : self.beforeReplyVerificationFailed(d, interest, onVerified, onVerifyFailed, onTimeout))
        else:
            self.beforeReplyDataVerified(data, onVerified, onVerifyFailed, onTimeout)
        return

    def beforeReplyDataVerified(self, data, onVerified, onVerifyFailed, onTimeout):
        # express next interest
        self._currentTimestamp = data.getName().get(-1).toVersion()
        self.consume(data.getName().getPrefix(-1), onVerified, onVerifyFailed, onTimeout)
        onVerified(data)
        return

    def beforeReplyVerificationFailed(self, data, interest, onVerified, onVerifyFailed, onTimeout):
        # for now internal to the library: verification failed cause the library to retransmit the interest after some time
        newInterest = Interest(interest)
        newInterest.refreshNonce()

        dummyInterest = Interest(Name("/local/timeout"))
        dummyInterest.setInterestLifetimeMilliseconds(self._verifyFailedRetransInterval)
        self._face.expressInterest(dummyInterest, 
          self.onDummyData, 
          lambda i: self.retransmitInterest(newInterest, onVerified, onVerifyFailed, onTimeout))
        onVerifyFailed(data)
        return

    def beforeReplyTimeout(self, interest, onVerified, onVerifyFailed, onTimeout):
        newInterest = Interest(interest)
        newInterest.refreshNonce()
        self._face.expressInterest(newInterest, 
          lambda i, d : self.onData(i, d, onVerified, onVerifyFailed, onTimeout), 
          lambda i: self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout))
        onTimeout(interest)
        return

    def retransmitInterest(self, interest, onVerified, onVerifyFailed, onTimeout):
        self._face.expressInterest(interest, 
          lambda i, d : self.onData(i, d, onVerified, onVerifyFailed, onTimeout), 
          lambda i: self.beforeReplyTimeout(i, onVerified, onVerifyFailed, onTimeout))

    def onDummyData(self, interest, data):
        print "Unexpected: got dummy data!"
        return