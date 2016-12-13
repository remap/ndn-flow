#ifndef __ndn_iot__entity__serializer__
#define __ndn_iot__entity__serializer__

#include <ndn-cpp/ndn-cpp-config.h>
#include <ndn-cpp/util/blob.hpp>
#include <exception>

#include "entity-info.hpp"

namespace ndn_iot
{
namespace discovery
{
  class EntitySerializer
  {
  public:
    virtual ndn::Blob 
    serialize(const ndn::ptr_lib::shared_ptr<EntityInfoBase> &entityInfo) = 0;
    
    virtual ndn::ptr_lib::shared_ptr<EntityInfoBase> 
    deserialize(ndn::Blob srcBlob) = 0;
  };
}
}

#endif