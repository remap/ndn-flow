#ifndef IOT_BASE_NODE_HPP
#define IOT_BASE_NODE_HPP

#include <ndn-cpp/name.hpp>
#include <ndn-cpp/interest.hpp>
#include <ndn-cpp/data.hpp>
#include <ndn-cpp/threadsafe-face.hpp>

#include <ndn-cpp/security/key-chain.hpp>
#include <ndn-cpp/security/identity/identity-manager.hpp>
#include <ndn-cpp/security/identity/basic-identity-storage.hpp>
#include <ndn-cpp/security/identity/file-private-key-storage.hpp>
#include <ndn-cpp/security/policy/config-policy-manager.hpp>

#include "common.hpp"
#include <boost/asio.hpp>

namespace ndn_iot {

class BaseNode {
public:
  BaseNode();
  ~BaseNode();

private:
  ndn::BasicIdentityStorage identityStorage;
  ndn::ConfigPolicyManager policyManager;
  ndn::IdentityManager identityManager;
  
  boost::asio::io_service ioService;
  ndn::ptr_lib::shared_ptr<ndn::ThreadsafeFace> face;

  bool setupComplete;
};

}

#endif