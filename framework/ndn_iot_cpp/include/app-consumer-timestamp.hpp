#ifndef IOT_APP_CONSUMER_TIMESTAMP_HPP
#define IOT_APP_CONSUMER_TIMESTAMP_HPP

#include "common.hpp"
#include "app-consumer.hpp"

namespace ndn_iot {

class AppConsumerTimestamp : public AppConsumer {
public:
    AppConsumerTimestamp(ndn::Face& face, ndn::ptr_lib::shared_ptr<ndn::KeyChain> keyChain, ndn::Name certificateName, bool doVerify, uint64_t currentTimestamp = 0);

    ~AppConsumerTimestamp() {};

    void consume(ndn::Name prefix, ndn::OnVerified onVerified, ndn::OnVerifyFailed onVerifyFailed, ndn::OnTimeout onTimeout);
private:
    void onData
    (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, const ndn::ptr_lib::shared_ptr<ndn::Data>& data, ndn::OnVerified onVerified, ndn::OnVerifyFailed onVerifyFailed, ndn::OnTimeout onTimeout);

    void beforeReplyDataVerified
    (const ndn::ptr_lib::shared_ptr<ndn::Data>& data, ndn::OnVerified onVerified, ndn::OnVerifyFailed onVerifyFailed, ndn::OnTimeout onTimeout);

    void beforeReplyVerificationFailed
    (const ndn::ptr_lib::shared_ptr<ndn::Data>& data, const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, ndn::OnVerified onVerified, ndn::OnVerifyFailed onVerifyFailed, ndn::OnTimeout onTimeout);

    void beforeReplyTimeout
    (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, ndn::OnVerified onVerified, ndn::OnVerifyFailed onVerifyFailed, ndn::OnTimeout onTimeout);    

    void retransmitInterest
    (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, ndn::OnVerified onVerified, ndn::OnVerifyFailed onVerifyFailed, ndn::OnTimeout onTimeout);

    void onDummyData
    (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, const ndn::ptr_lib::shared_ptr<ndn::Data>& data);

    uint64_t currentTimestamp_;

    int verifyFailedRetransInterval_;
    int defaultInterestLifetime_;
};

}

#endif