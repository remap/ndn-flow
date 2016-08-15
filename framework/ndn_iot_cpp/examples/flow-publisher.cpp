#include "bootstrap.hpp"
#include "common.hpp"
#include "boost/asio.hpp"

using namespace ndn;
using namespace std;

namespace ndn_iot {

namespace examples {

class ExamplePublisher
{
public:
  ExamplePublisher() {
    face_ = ptr_lib::shared_ptr<ThreadsafeFace>(new ThreadsafeFace(ioService_));
  }

  void run() {
    cout << "Running test" << endl;
    AppBootstrap bootstrap(*(face_.get()));

    // Keep ioService running until the Counter calls stop().
    boost::asio::io_service::work work(ioService_);
    ioService_.run();
  }
private:
  ptr_lib::shared_ptr<ThreadsafeFace> face_;
  boost::asio::io_service ioService_;
};

}

}

int main() {
  ndn_iot::examples::ExamplePublisher publisher;
  publisher.run();

  return 0;
}