#ifndef IOT_APP_BOOTSTRAP_HPP
#define IOT_APP_BOOTSTRAP_HPP

#include "common.hpp"
#include <boost/asio.hpp>

namespace ndn_iot {

// Setup functions are now made synchronous, the following aren't used
typedef ndn::func_lib::function<void
  (const std::string)> OnSetupFailed;

typedef ndn::func_lib::function<void
  (const ndn::Name&, const ndn::KeyChain&)> OnSetupComplete;

// Application publishing request granted, no param
typedef ndn::func_lib::function<void
  (void)> OnRequestSuccess;

// Application publishing request failed, msg param
typedef ndn::func_lib::function<void
  (const std::string)> OnRequestFailed;

// Trust schema update success, (schema string, is-first-update) param
typedef ndn::func_lib::function<void
  (const std::string, bool)> OnUpdateSuccess;

// Trust schema update failed, msg param
typedef ndn::func_lib::function<void
  (const std::string)> OnUpdateFailed;

class AppTrustSchema {
public:
  AppTrustSchema(bool following, std::string schema, uint64_t version, bool isInitial)
   : following_(following), schema_(schema), version_(version), isInitial_(isInitial)
  {};

  ~AppTrustSchema()
  {};

  bool getFollowing() {
    return following_;
  }

  std::string getSchema() {
    return schema_;
  }

  void setFollowing(bool following) {
    following_ = following;
    return;
  }

  void setSchema(std::string schema) {
    schema_ = schema;
    return;
  }

  bool getIsInitial() {
    return isInitial_;
  }

  void setIsInitial(bool isInitial) {
    isInitial_ = isInitial;
    return;
  }

  uint64_t getVersion() {
    return version_;
  }

  void setVersion(int version) {
    version_ = version;
    return;
  }
private:
  bool following_;
  std::string schema_;
  uint64_t version_;
  bool isInitial_;
};

class Bootstrap {

public:
  Bootstrap
    (ndn::ThreadsafeFace& face, std::string confFile = "app.conf");
  
  ~Bootstrap();

  bool 
  processConfiguration
    (std::string confFile, bool requestPermission = true, 
     const OnSetupComplete& onSetupComplete = NULL, 
     const OnSetupFailed& onSetupFailed = NULL);
  
  ndn::Name
  getIdentityNameFromCertName(ndn::Name certName);

  void
  requestProducerAuthorization(ndn::Name dataPrefix, std::string appName, OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed);

  void
  sendAppRequest(ndn::Name certificateName, ndn::Name dataPrefix, std::string appName, OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed);

  ndn::ptr_lib::shared_ptr<ndn::KeyChain>
  setupDefaultIdentityAndRoot(ndn::Name defaultIdentity, ndn::Name signerName);

  ndn::Name
  getDefaultIdentity();

  void
  stopTrustSchemaUpdate();

  void
  startTrustSchemaUpdate(ndn::Name appPrefix, OnUpdateSuccess onUpdateSuccess, OnUpdateFailed onUpdateFailed);

private:

  void 
  onAppRequestDataVerified(const ndn::ptr_lib::shared_ptr<ndn::Data>& data, OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed);

  void 
  onAppRequestDataVerifyFailed(const ndn::ptr_lib::shared_ptr<ndn::Data>& data, OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed);

  void
  onNetworkNack(const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, const ndn::ptr_lib::shared_ptr<ndn::NetworkNack>& networkNack,  OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed);

  void
  onAppRequestData(const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, const ndn::ptr_lib::shared_ptr<ndn::Data>& data, OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed);

  void 
  onAppRequestTimeout(const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, OnRequestSuccess onRequestSuccess, OnRequestFailed onRequestFailed);

  void 
  onRegisterFailed(const ndn::ptr_lib::shared_ptr<const ndn::Name>& prefix);
  
  void
  onSchemaVerificationFailed(const ndn::ptr_lib::shared_ptr<const ndn::Data>& data, OnUpdateSuccess onUpdateSuccess, OnUpdateFailed onUpdateFailed);

  void
  reexpressSchemaInterest(ndn::Interest newInterest, OnUpdateSuccess onUpdateSuccess, OnUpdateFailed onUpdateFailed);
  
  void
  onSchemaVerified(const ndn::ptr_lib::shared_ptr<const ndn::Data>& data, OnUpdateSuccess onUpdateSuccess, OnUpdateFailed onUpdateFailed);
  
  void
  onTrustSchemaTimeout(const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, OnUpdateSuccess onUpdateSuccess, OnUpdateFailed onUpdateFailed);
  
  void
  onTrustSchemaData(const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, const ndn::ptr_lib::shared_ptr<ndn::Data>& data, OnUpdateSuccess onUpdateSuccess, OnUpdateFailed onUpdateFailed);

  void
  onNetworkNackSchema(const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, const ndn::ptr_lib::shared_ptr<ndn::NetworkNack>& networkNack, OnUpdateSuccess onUpdateSuccess, OnUpdateFailed onUpdateFailed);

  ndn::ThreadsafeFace& face_;
  ndn::Name defaultIdentity_;
  ndn::Name defaultCertificateName_;
  ndn::Name defaultKeyName_;
  ndn::Name controllerName_;
  ndn::ptr_lib::shared_ptr<ndn::IdentityCertificate> controllerCertificate_;
  ndn::Name dataPrefix_;

  std::string applicationName_;
  ndn::MemoryContentCache certificateContentCache_;

  ndn::ptr_lib::shared_ptr<ndn::BasicIdentityStorage> identityStorage_;
  ndn::ptr_lib::shared_ptr<ndn::ConfigPolicyManager> policyManager_;
  ndn::ptr_lib::shared_ptr<ndn::IdentityManager> identityManager_;
  ndn::ptr_lib::shared_ptr<ndn::CertificateCache> certificateCache_;
  ndn::ptr_lib::shared_ptr<ndn::KeyChain> keyChain_;

  std::map<std::string, ndn::ptr_lib::shared_ptr<AppTrustSchema>> trustSchemas_;

  bool setupComplete;
};

}

#endif