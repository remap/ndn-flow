#include "app-consumer-timestamp.hpp"
#include <ndn-cpp/exclude.hpp>

using namespace ndn;
using namespace std;
using namespace ndn::func_lib;

namespace ndn_iot {

AppConsumerTimestamp::AppConsumerTimestamp
  (Face& face, ndn::ptr_lib::shared_ptr<KeyChain> keyChain, Name certificateName, bool doVerify, int64_t currentTimestamp) 
 : AppConsumer(face, keyChain, certificateName, doVerify)
{
    currentTimestamp_ = currentTimestamp;
    
    verifyFailedRetransInterval_ = 4000;
    defaultInterestLifetime_ = 4000;
}

void AppConsumerTimestamp::consume
  (Name prefix, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout) 
{
    Name name(prefix);
    Interest interest(name);
    interest.setInterestLifetimeMilliseconds(defaultInterestLifetime_);
    
    if (currentTimestamp_ >= 0) {
        Exclude exclude;
        exclude.appendAny();
        exclude.appendComponent(Name::Component::fromVersion(currentTimestamp_));
        interest.setExclude(exclude);
    }
    
    face_.expressInterest(interest, 
      bind(&AppConsumerTimestamp::onData, this, _1, _2, onVerified, onVerifyFailed, onTimeout), 
      bind(&AppConsumerTimestamp::beforeReplyTimeout, this, _1, onVerified, onVerifyFailed, onTimeout));

    return;
}

void AppConsumerTimestamp::onData
  (const ptr_lib::shared_ptr<const Interest>& interest, const ptr_lib::shared_ptr<Data>& data, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout)
{
    if (doVerify_) {
        keyChain_->verifyData(data, 
          bind(&AppConsumerTimestamp::beforeReplyDataVerified, this, _1, onVerified, onVerifyFailed, onTimeout),
          (const OnVerifyFailed)bind(&AppConsumerTimestamp::beforeReplyVerificationFailed, this, _1, interest, onVerified, onVerifyFailed, onTimeout));
    } else {
        beforeReplyDataVerified(data, onVerified, onVerifyFailed, onTimeout);
    }
    return;
}

void AppConsumerTimestamp::beforeReplyDataVerified
  (const ptr_lib::shared_ptr<Data>& data, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout)
{
    uint64_t version = data->getName().get(-1).toVersion();
    currentTimestamp_ = version;

    consume(data->getName().getPrefix(-1), onVerified, onVerifyFailed, onTimeout);
    onVerified(data);
    return;
}

void AppConsumerTimestamp::beforeReplyVerificationFailed
  (const ptr_lib::shared_ptr<Data>& data, const ptr_lib::shared_ptr<const Interest>& interest, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout)
{
    Interest newInterest(*interest);
    newInterest.refreshNonce();
    Interest dummyInterest(Name("/local/timeout"));

    dummyInterest.setInterestLifetimeMilliseconds(verifyFailedRetransInterval_);
    face_.expressInterest(dummyInterest, 
      bind(&AppConsumerTimestamp::onDummyData, this, _1, _2),
      bind(&AppConsumerTimestamp::retransmitInterest, this, ptr_lib::make_shared<const Interest>(newInterest), onVerified, onVerifyFailed, onTimeout));
    return;
}

void AppConsumerTimestamp::beforeReplyTimeout
  (const ptr_lib::shared_ptr<const Interest>& interest, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout)    
{
    Interest newInterest(*interest);
    newInterest.refreshNonce();

    face_.expressInterest(newInterest, 
      bind(&AppConsumerTimestamp::onData, this, _1, _2, onVerified, onVerifyFailed, onTimeout), 
      bind(&AppConsumerTimestamp::beforeReplyTimeout, this, _1, onVerified, onVerifyFailed, onTimeout));
    onTimeout(interest);
    return;
}

void AppConsumerTimestamp::retransmitInterest
  (const ptr_lib::shared_ptr<const Interest>& interest, OnVerified onVerified, OnVerifyFailed onVerifyFailed, OnTimeout onTimeout)
{
    face_.expressInterest(*interest, 
      bind(&AppConsumerTimestamp::onData, this, _1, _2, onVerified, onVerifyFailed, onTimeout), 
      bind(&AppConsumerTimestamp::beforeReplyTimeout, this, _1, onVerified, onVerifyFailed, onTimeout));
    return;
}

void AppConsumerTimestamp::onDummyData
  (const ptr_lib::shared_ptr<const Interest>& interest, const ptr_lib::shared_ptr<Data>& data)
{
    cout << "Unexpected: got dummy data!" << endl;
    return;
}

}

