#include "app-bootstrap.hpp"
#include "boost-info-parser.hpp"

using namespace ndn;
using namespace std;
using namespace ndn::func_lib;

namespace ndn_iot {

class AppBootstrap;

AppBootstrap::AppBootstrap
  (ndn::ThreadsafeFace& face, std::string confFile)
 : face_(face)
{
  ptr_lib::shared_ptr<BasicIdentityStorage> identityStorage = ptr_lib::shared_ptr<BasicIdentityStorage>(new BasicIdentityStorage());
  ptr_lib::shared_ptr<ConfigPolicyManager> policyManager = ptr_lib::shared_ptr<ConfigPolicyManager>(new ConfigPolicyManager());
  ptr_lib::shared_ptr<FilePrivateKeyStorage> filePrivateKeyStorage = ptr_lib::shared_ptr<FilePrivateKeyStorage>(new FilePrivateKeyStorage());
  ptr_lib::shared_ptr<IdentityManager> identityManager = ptr_lib::shared_ptr<IdentityManager>(new IdentityManager(identityStorage, filePrivateKeyStorage));
  keyChain_.reset(new KeyChain(identityManager, policyManager));

  processConfiguration(confFile);
}

AppBootstrap::~AppBootstrap()
{
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
    }
  } catch (const std::exception& e) {
    cout << e.what();
  }
  return true;
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

void 
AppBootstrap::sendApplicationRequest()
{
  return;
}

}