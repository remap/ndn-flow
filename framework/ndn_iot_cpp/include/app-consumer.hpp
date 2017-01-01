#ifndef IOT_APP_CONSUMER_HPP
#define IOT_APP_CONSUMER_HPP

#include "common.hpp"

namespace ndn_iot {

class AppConsumer {
public:
    AppConsumer(ndn::Face& face, ndn::ptr_lib::shared_ptr<ndn::KeyChain> keyChain, bool doVerify);

    ~AppConsumer();

    virtual void consume(ndn::Name name, ndn::OnVerified onVerified, ndn::OnVerifyFailed onVerifyFailed, ndn::OnTimeout onTimeout)
    const = 0;
protected:
    ndn::Face& face_;
    ndn::ptr_lib::shared_ptr<ndn::KeyChain> keyChain_;
    bool doVerify_;    
};

}

#endif