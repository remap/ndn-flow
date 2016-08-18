#include "bootstrap.hpp"
#include "common.hpp"
#include "boost/asio.hpp"

using namespace ndn;
using namespace std;

namespace ndn_iot {

namespace examples {

class AppConsumer
{
public:
  AppConsumer() {
    face_ = ptr_lib::shared_ptr<ThreadsafeFace>(new ThreadsafeFace(ioService_));
  }

  void run() {
    Bootstrap bootstrap(*(face_.get()));
    keyChain_ = bootstrap.setupDefaultIdentityAndRoot(Name("/home/my-cpp-device"));
    defaultCertificateName_ = bootstrap.getDefaultCertificateName();
    cout << "default certificate name is: " << defaultCertificateName_.toUri() << endl;
    memoryContentCache_.reset(new MemoryContentCache(face_.get()));
    memoryContentCache_->registerPrefix(Name("/home/flow/cpp-publisher-1"), 
      bind(&AppConsumer::onRegisterFailed, this, _1));

    bootstrap.requestProducerAuthorization(Name("/home/flow/cpp-publisher-1"), "flow",
      bind(&AppConsumer::onRequestSuccess, this), 
      bind(&AppConsumer::onRequestFailed, this, _1));
    boost::asio::io_service::work work(ioService_);
    ioService_.run();
  }
private:
  void onRequestSuccess() {
    cout << "request granted" << endl;
    Data data(Name("/home/flow/cpp-publisher-1/0"));
    string content = "good good";
    data.setContent((const uint8_t *)&content[0], content.size());
    keyChain_->sign(data, defaultCertificateName_);

    memoryContentCache_->add(data);
  }

  void onRequestFailed(string msg) {
    cout << "request not granted: " << msg << "; in this test, publish anyway" << endl;

    Data data(Name("/home/flow/cpp-publisher-1/0"));
    string content = "good good";
    data.setContent((const uint8_t *)&content[0], content.size());
    keyChain_->sign(data, defaultCertificateName_);

    memoryContentCache_->add(data);
  }

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

int main() {
  ndn_iot::examples::AppConsumer consumer;
  consumer.run();

  return 0;
}