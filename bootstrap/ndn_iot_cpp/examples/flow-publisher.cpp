#include "app-bootstrap.hpp"
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
    boost::asio::io_service ioService;
    face_ = ptr_lib::shared_ptr<ThreadsafeFace>(new ThreadsafeFace(ioService));
  }

  void run() {
    cout << "Running test" << endl;
    AppBootstrap bootstrap(*(face_.get()));
  }
private:
  ptr_lib::shared_ptr<ThreadsafeFace> face_;

};

}

}

int main() {
  ndn_iot::examples::ExamplePublisher publisher;
  publisher.run();
  return 0;
}