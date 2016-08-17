#include <time.h>

#include "bootstrap.hpp"
#include "boost-info-parser.hpp"
#include "app-request.pb.h"
#include <ndn-cpp/encoding/protobuf-tlv.hpp>

using namespace ndn;
using namespace std;
using namespace ndn::func_lib;

namespace ndn_iot {

Bootstrap::Bootstrap
  (ndn::ThreadsafeFace& face)
 : face_(face)
{
  identityStorage_ = ptr_lib::shared_ptr<BasicIdentityStorage>(new BasicIdentityStorage());
  certificateCache_ = ptr_lib::shared_ptr<CertificateCache>();
  policyManager_ = ptr_lib::shared_ptr<ConfigPolicyManager>(new ConfigPolicyManager("", certificateCache_));
  identityManager_ = ptr_lib::shared_ptr<IdentityManager>(new IdentityManager(identityStorage));
  keyChain_.reset(new KeyChain(identityManager, policyManager));

  //processConfiguration(confFile);
}

AppBootstrap::~AppBootstrap()
{
}

/**
 * Initial keyChain and defaultCertificate setup
 */
Name
Bootstrap::setupDefaultIdentityAndRoot
  (Name defaultIdentity, Name signerName)
{
  if (defaultIdentity.size() == 0) {
    try {
      defaultIdentity = identityManager_.getDefaultIdentity();
    } catch (const SecurityException& e) {
      cout << "Default identity does not exist " << e.what() << endl;
    }
    
  }
  try {
    defaultIdentity_ = Name(defaultIdentity);
    defaultCertificateName_ = identityManager_.getDefaultCertificateNameForIdentity(defaultIdentity_);
    defaultKeyName_ = identityManager_->getDefaultKeyNameForIdentity(defaultIdentity_);
  } catch (const SecurityException& e) {
    cout << "Cannot find keys for configured identity " << defaultIdentityString << endl;
    return Name();
  }
  face_.setCommandSigningInfo(keyChain_, defaultCertificateName_);
  Name actualSignerName = (KeyLocator::getFromSignature(keyChain_->getCertificate(defaultCertificateName_)->getSignature())).getKeyName();

  if (actualSignerName != signerName) {
    cout << "Signer name mismatch" << endl;
    return Name();
  }

  controllerName_ = getIdentityNameFromCertName(signerName);
  try {
    controllerCertificate_ = keyChain_.getCertificate(identityManager_.getDefaultCertificateNameForIdentity(controllerName_))
    policyManager_
  }
}

bool 
AppBootstrap::processConfiguration
  (std::string confFile, bool requestPermission, 
   const OnSetupComplete& onSetupComplete, const OnSetupFailed& onSetupFailed)
{
  BoostInfoParser config;

  try {
    // if the file does not exist, we would run into a runtime_error
    config.read(confFile);

    string defaultIdentityString = "";
    if (config.getRoot()["application/identity"].size() > 0) {
      defaultIdentityString = config.getRoot()["application/identity"][0]->getValue();
      if (defaultIdentityString == "default") {
        defaultIdentity_ = keyChain_->getDefaultIdentity();
      } else {
        try {
          defaultIdentity_ = Name(defaultIdentityString);
          keyChain_->getIdentityManager()->getDefaultKeyNameForIdentity(defaultIdentity_);
        } catch (const SecurityException& e) {
          cout << "Cannot find keys for configured identity " << defaultIdentityString << endl;
          return false;
        }
      }
    } else {
      defaultIdentity_ = keyChain_->getDefaultIdentity();
    }
    cout << "here..." << endl;

    defaultCertificateName_ = keyChain_->getIdentityManager()->getDefaultCertificateNameForIdentity(defaultIdentity_);
    Name signerName = (KeyLocator::getFromSignature(keyChain_->getCertificate(defaultCertificateName_)->getSignature())).getKeyName();

    if (config.getRoot()["application/signer"].size() > 0) {
      string intendedSigner = config.getRoot()["application/signer"][0]->getValue();
      if (intendedSigner == "default") {
        cout << "Using default signer name " << signerName.toUri() << endl;
      } else {
        if (intendedSigner != signerName.toUri()) {
          cout << "Signer name mismatch" << endl;
        }
      }
    }

    controllerName_ = getIdentityNameFromCertName(signerName);

    if (config.getRoot()["application/appName"].size() > 0) {
      applicationName_ = config.getRoot()["application/appName"][0]->getValue();
    } else {
      throw std::runtime_error("Configuration is missing expected appName (application name).\n");
    }

    if (config.getRoot()["application/prefix"].size() > 0) {
      dataPrefix_ = config.getRoot()["application/prefix"][0]->getValue();
    } else {
      throw std::runtime_error("Configuration is missing expected prefix (application prefix).\n");
    }
  } catch (const std::exception& e) {
    cout << e.what() << endl;
    if (onSetupFailed) {
      onSetupFailed(e.what());
    }
    return false;
  }
  if (requestPermission) {
    sendAppRequest();
  } else {
    if (onSetupComplete) {
      onSetupComplete(defaultIdentity_, *keyChain_.get());
    }
  }
  return true;
}
  
void
AppBootstrap::sendAppRequest() {
  AppRequestMessage message;
  int i = 0;

  for (i = 0; i < defaultIdentity_.size(); i++) {
    message.mutable_command()->mutable_idname()->add_components(defaultIdentity_.get(i).toEscapedString());
  }
      
  for (i = 0; i < dataPrefix_.size(); i++) {
    message.mutable_command()->mutable_dataprefix()->add_components(dataPrefix_.get(i).toEscapedString());
  }
  
  message.mutable_command()->set_appname(applicationName_);
  time_t secondsSinceEpoch = time(0);
  
  Name requestInterestName(Name(controllerName_).append("requests").appendVersion((int)secondsSinceEpoch).append(Name::Component(ProtobufTlv::encode(message))));
  Interest requestInterest(requestInterestName);
  // TODO: change this. (for now, make this request long lived (100s), if the controller operator took some time to respond)
  requestInterest.setInterestLifetimeMilliseconds(100000);
  keyChain_->sign(requestInterest, defaultCertificateName_);
  // keyChain.sign vs face.makeCommandInterest(requestInterest) ?

  face_.expressInterest
    (requestInterest, bind(&AppBootstrap::onAppRequestData, this, _1, _2), 
     bind(&AppBootstrap::onAppRequestTimeout, this, _1), bind(&AppBootstrap::onNetworkNack, this, _1, _2));
  cout << "Application publish request sent: " + requestInterest.getName().toUri() << endl;
  
  return ;
}

void
AppBootstrap::onAppRequestData
(const ptr_lib::shared_ptr<const Interest>& interest, const ptr_lib::shared_ptr<Data>& data)
{

}

void 
AppBootstrap::onAppRequestTimeout
(const ptr_lib::shared_ptr<const Interest>& interest)
{

}

void
AppBootstrap::onNetworkNack
(const ptr_lib::shared_ptr<const Interest>& interest, const ptr_lib::shared_ptr<NetworkNack>& networkNack)
{

}

Name
AppBootstrap::getIdentityNameFromCertName(Name certName)
{
  int i = certName.size() - 1;

  string idString = "KEY";
  while (i >= 0) {
    if (certName.get(i).toEscapedString() == idString)
      break;
    i -= 1;
  }
      
  if (i < 0) {
    cout << "Error: unexpected certName " << certName.toUri() << endl;
    return Name();
  }

  return certName.getPrefix(i);
}


}