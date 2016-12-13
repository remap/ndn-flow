#include "bootstrap.hpp"
#include "common.hpp"
#include "boost/asio.hpp"

using namespace ndn;
using namespace std;
using namespace ndn::func_lib;

namespace ndn_iot {

namespace examples {

class AppConsumer
{
public:
  AppConsumer() {
    face_ = ptr_lib::shared_ptr<ThreadsafeFace>(new ThreadsafeFace(ioService_));
  }

  void run() {
    cout << "Running test consumer" << endl;
    Bootstrap bootstrap(*(face_.get()));
    keyChain_ = bootstrap.setupDefaultIdentityAndRoot(Name("/home/mygooddevice"));
    Name defaultCertificate = bootstrap.getDefaultCertificateName();
    bootstrap.startTrustSchemaUpdate(Name("/home/gateway/flow"), 
      bind(&AppConsumer::onUpdateSuccess, this, _1, _2), 
      bind(&AppConsumer::onUpdateFailed, this, _1));

    boost::asio::io_service::work work(ioService_);
    ioService_.run();
  }
private:
  void onUpdateSuccess(string schema, bool isInitial) {
    cout << "trust schema update success" << endl;
    if (isInitial) {
      face_->expressInterest(Name("/home/flow/cpp-publisher-1"), 
        bind(&AppConsumer::onData, this, _1, _2),
        bind(&AppConsumer::onTimeout, this, _1));
    }
  }

  void onUpdateFailed(string msg) {
    cout << "Trust schema update failed: " << msg << endl;
  }

  void onVerified(const ptr_lib::shared_ptr<const Data>& data) {
    cout << "data verified: " << data->getName().toUri() << endl;
  }

  void onVerifyFailed(const ptr_lib::shared_ptr<const Data>& data) {
    cout << "data verification failed: " << data->getName().toUri() << endl;
  }

  void onData(const ptr_lib::shared_ptr<const Interest>& interest, const ptr_lib::shared_ptr<Data>& data) {
    keyChain_->verifyData(data, 
      bind(&AppConsumer::onVerified, this, _1),
      (const OnVerifyFailed)bind(&AppConsumer::onVerifyFailed, this, _1));
  }

  void onTimeout(const ptr_lib::shared_ptr<const Interest>& interest) {
    cout << "interest times out: " << interest->getName().toUri() << endl;
  }

  ptr_lib::shared_ptr<ThreadsafeFace> face_;
  boost::asio::io_service ioService_;
  ptr_lib::shared_ptr<KeyChain> keyChain_;
};

}

}

int main() {
  ndn_iot::examples::AppConsumer consumer;
  consumer.run();

  return 0;
}