#ifndef IOT_APP_CONSUMER_SEQUENCE_NUMBER_HPP
#define IOT_APP_CONSUMER_SEQUENCE_NUMBER_HPP

#include "common.hpp"
#include "app-consumer.hpp"

namespace ndn_iot {

class AppConsumerSequenceNumber : public AppConsumer {
public:
    AppConsumerSequenceNumber(ndn::Face& face, ndn::ptr_lib::shared_ptr<ndn::KeyChain> keyChain, ndn::Name certificateName, bool doVerify, int defaultPipelineSize = 5, int startingSequenceNumber = 0);

    ~AppConsumerSequenceNumber() {};

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

    int pipelineSize_;
    int currentSeqNumber_;
    int emptySlot_;

    int verifyFailedRetransInterval_;
    int defaultInterestLifetime_;
};

}

#endif