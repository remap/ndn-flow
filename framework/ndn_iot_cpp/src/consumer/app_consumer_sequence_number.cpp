#include "app-consumer-sequence-number.hpp"

using namespace ndn;
using namespace std;
using namespace ndn::func_lib;

namespace ndn_iot {

AppConsumerSequenceNumber::AppConsumerSequenceNumber
  (Face& face, ndn::ptr_lib::shared_ptr<KeyChain> keyChain, bool doVerify, int defaultPipelineSize, int defaultSeqNumber) 
 : AppConsumer(face, keyChain, doVerify)
{
    currentSeqNumber_ = defaultSeqNumber;
    pipelineSize_ = defaultPipelineSize;
    emptySlot_ = defaultPipelineSize;
    
    verifyFailedRetransInterval_ = 4000;
    defaultInterestLifetime_ = 4000;
}

void AppConsumerSequenceNumber::consume
  (Name prefix, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout) 
{
    int num = emptySlot_;
    
    for (int i = 0; i < num; i++) {
        Name name(prefix);
        name.append(std::to_string(currentSeqNumber_));
        Interest interest(name);
        
        // interest configuration / template?
        interest.setInterestLifetimeMilliseconds(defaultInterestLifetime_);
        face_.expressInterest(interest, 
          bind(&AppConsumerSequenceNumber::onData, this, _1, _2, onVerified, onVerifyFailed, onTimeout), 
          bind(&AppConsumerSequenceNumber::beforeReplyTimeout, this, _1, onVerified, onVerifyFailed, onTimeout));
        currentSeqNumber_ += 1;
        emptySlot_ += 1;
    }
    return;
}

void AppConsumerSequenceNumber::onData
  (const ptr_lib::shared_ptr<const Interest>& interest, const ptr_lib::shared_ptr<Data>& data, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout)
{
    if (doVerify_) {
        keyChain_->verifyData(data, 
          bind(&AppConsumerSequenceNumber::beforeReplyDataVerified, this, _1, onVerified, onVerifyFailed, onTimeout),
          (const OnVerifyFailed)bind(&AppConsumerSequenceNumber::beforeReplyVerificationFailed, this, _1, interest, onVerified, onVerifyFailed, onTimeout));
    } else {
        beforeReplyDataVerified(data, onVerified, onVerifyFailed, onTimeout);
    }
    return;
}

void AppConsumerSequenceNumber::beforeReplyDataVerified
  (const ptr_lib::shared_ptr<Data>& data, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout)
{
    // fill the pipeline
    currentSeqNumber_ += 1;
    emptySlot_ += 1;
    consume(data->getName().getPrefix(-1), onVerified, onVerifyFailed, onTimeout);
    onVerified(data);
    return;
}

void AppConsumerSequenceNumber::beforeReplyVerificationFailed
  (const ptr_lib::shared_ptr<Data>& data, const ptr_lib::shared_ptr<const Interest>& interest, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout)
{
    Interest newInterest(*interest);
    newInterest.refreshNonce();
    Interest dummyInterest(Name("/local/timeout"));

    dummyInterest.setInterestLifetimeMilliseconds(verifyFailedRetransInterval_);
    face_.expressInterest(dummyInterest, 
      bind(&AppConsumerSequenceNumber::onDummyData, this, _1, _2),
      bind(&AppConsumerSequenceNumber::retransmitInterest, this, ptr_lib::make_shared<const Interest>(newInterest), onVerified, onVerifyFailed, onTimeout));
    return;
}

void AppConsumerSequenceNumber::beforeReplyTimeout
  (const ptr_lib::shared_ptr<const Interest>& interest, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout)    
{
    Interest newInterest(*interest);
    newInterest.refreshNonce();

    face_.expressInterest(newInterest, 
      bind(&AppConsumerSequenceNumber::onData, this, _1, _2, onVerified, onVerifyFailed, onTimeout), 
      bind(&AppConsumerSequenceNumber::beforeReplyTimeout, this, _1, onVerified, onVerifyFailed, onTimeout));
    onTimeout(interest);
    return;
}

void AppConsumerSequenceNumber::retransmitInterest
  (const ptr_lib::shared_ptr<const Interest>& interest, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout)
{
    face_.expressInterest(*interest, 
      bind(&AppConsumerSequenceNumber::onData, this, _1, _2, onVerified, onVerifyFailed, onTimeout), 
      bind(&AppConsumerSequenceNumber::beforeReplyTimeout, this, _1, onVerified, onVerifyFailed, onTimeout));
    return;
}

void AppConsumerSequenceNumber::onDummyData
  (const ptr_lib::shared_ptr<const Interest>& interest, const ptr_lib::shared_ptr<Data>& data)
{
    cout << "Unexpected: got dummy data!" << endl;
    return;
}

}

