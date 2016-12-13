#include <exception>
#include <stdio.h>

#include "boost/asio.hpp"

#include "bootstrap.hpp"
#include "sync-based-discovery.hpp"
#include "external-observer.hpp"
#include "entity-discovery.hpp"

using namespace std;
using namespace ndn;
using namespace ndn::func_lib;
using namespace ndn_iot;
using namespace ndn_iot::discovery;

ptr_lib::shared_ptr<EntityDiscovery> discovery;

string getRandomString(const int len) {
  static const char alphanum[] =
    "0123456789"
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    "abcdefghijklmnopqrstuvwxyz";
  
  string s = "";
  for (int i = 0; i < len; ++i) {
    s += alphanum[rand() % (sizeof(alphanum) - 1)];
  }

  return s;
}

namespace ndn_iot {

namespace examples {

class EntityInfo : public EntityInfoBase {
public:
  string getDescription() {
    return description_;
  }

  void setDescription(string desc) {
    description_ = desc;
  }

private:
  string description_;
};

class Observer : public ExternalObserver {
public:
  void onStateChanged(MessageTypes type, const char *msg, double timestamp) {
    cout << "Observer: " << timestamp << " : " << msg << endl;
  }
};

class Serializer : public EntitySerializer 
{
public:
  virtual Blob
  serialize(const ptr_lib::shared_ptr<EntityInfoBase> &description) {
    string content = (ptr_lib::dynamic_pointer_cast<EntityInfo>(description))->getDescription();
    return Blob((const uint8_t *)(&content[0]), content.size());
  }
  
  virtual ptr_lib::shared_ptr<EntityInfoBase>
  deserialize(Blob srcBlob) {
    string content = "";
    for (size_t i = 0; i < srcBlob.size(); ++i) {
      content += (*srcBlob)[i];
    }
    
    EntityInfo cd;
    cd.setDescription(content);
    return ptr_lib::make_shared<EntityInfo>(cd);
  }
};

class DiscoveryTest
{
public:
  DiscoveryTest() {
    face_ = ptr_lib::shared_ptr<ThreadsafeFace>(new ThreadsafeFace(ioService_));
  }

  void run() {
    Bootstrap bootstrap(*(face_.get()));
    Name objectPrefix("/home/my-cpp-device");
    keyChain_ = bootstrap.setupDefaultIdentityAndRoot(objectPrefix);
    defaultCertificateName_ = bootstrap.getDefaultCertificateName();

    Name syncPrefix("/home/discovery");

    Observer observer;
    Serializer serializer;
    
    ptr_lib::shared_ptr<EntityDiscovery> discovery = ptr_lib::make_shared<EntityDiscovery>(EntityDiscovery(*(face_.get()), *(keyChain_.get()), defaultCertificateName_, syncPrefix, &observer, ptr_lib::make_shared<Serializer>(serializer)));
    cout << "default cert name" << defaultCertificateName_.toUri() << endl;
    discovery->start();
    cout << "good so far" << endl;

    Name entityName(objectPrefix);
    entityName.append(getRandomString(3));

    EntityInfo ei;
    ei.setDescription(entityName.toUri());

    discovery->publishEntity(entityName.toUri(), ptr_lib::make_shared<EntityInfo>(ei));
    boost::asio::io_service::work work(ioService_);
    ioService_.run();
  }
private:
  void onVerified(const ptr_lib::shared_ptr<const Data>& data) {
    cout << "data verified: " << data->getName().toUri() << endl;
  }

  void onVerifyFailed(const ptr_lib::shared_ptr<const Data>& data) {
    cout << "data verification failed: " << data->getName().toUri() << endl;
  }

  void onRegisterFailed(const ptr_lib::shared_ptr<const Name>& prefix) {
    cout << "Prefix registration failed: " << prefix->toUri() << endl;
  }

  ptr_lib::shared_ptr<ThreadsafeFace> face_;
  boost::asio::io_service ioService_;
  ptr_lib::shared_ptr<KeyChain> keyChain_;
  ptr_lib::shared_ptr<MemoryContentCache> memoryContentCache_;
  Name defaultCertificateName_;
};

}

}

int main()
{
  ndn_iot::examples::DiscoveryTest discoveryTest;
  discoveryTest.run();
  return 1;
}