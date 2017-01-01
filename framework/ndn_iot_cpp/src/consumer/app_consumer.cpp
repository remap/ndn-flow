#include "app-consumer.hpp"

using namespace ndn;
using namespace std;
using namespace ndn::func_lib;

namespace ndn_iot {

AppConsumer::AppConsumer
  (Face& face, ndn::ptr_lib::shared_ptr<KeyChain> keyChain, bool doVerify) 
 : face_(face), keyChain_(keyChain), doVerify_(doVerify)
{
}

}