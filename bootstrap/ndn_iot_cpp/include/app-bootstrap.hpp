#ifndef IOT_APP_BOOTSTRAP_HPP
#define IOT_APP_BOOTSTRAP_HPP

#include "common.hpp"
#include <boost/asio.hpp>

namespace ndn_iot {

class AppBootstrap {

typedef ndn::func_lib::function<void
  (const std::string)> OnSetupFailed;

typedef ndn::func_lib::function<void
  (const ndn::Name&, const ndn::KeyChain&)> OnSetupComplete;

public:
  AppBootstrap
    (ndn::ThreadsafeFace& face, std::string confFile = "app.conf");
  
  ~AppBootstrap();

  bool 
  processConfiguration
    (std::string confFile, bool requestPermission = true, 
     const OnSetupComplete& onSetupComplete = NULL, 
     const OnSetupFailed& onSetupFailed = NULL);
  
  ndn::Name
  getIdentityNameFromCertName(ndn::Name certName);

  void
  sendAppRequest();
private:
  ndn::ptr_lib::shared_ptr<ndn::KeyChain> keyChain_;
  ndn::ThreadsafeFace& face_;
  ndn::Name defaultIdentity_;
  ndn::Name defaultCertificateName_;
  ndn::Name controllerName_;
  ndn::Name dataPrefix_;

  std::string applicationName_;

  bool setupComplete;
};

}

#endif