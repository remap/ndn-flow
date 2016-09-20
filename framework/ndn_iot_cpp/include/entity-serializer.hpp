#ifndef __ndnrtc__addon__conference__info__factory__
#define __ndnrtc__addon__conference__info__factory__

#include <ndn-cpp/ndn-cpp-config.h>
#include <ndn-cpp/util/blob.hpp>
#include <exception>

#include "entity-info.hpp"

namespace entity_discovery
{
  class IEntitySerializer
  {
  public:
    virtual ndn::Blob 
    serialize(const ndn::ptr_lib::shared_ptr<EntityInfoBase> &entityInfo) = 0;
    
    virtual ndn::ptr_lib::shared_ptr<EntityInfoBase> 
    deserialize(ndn::Blob srcBlob) = 0;
  };
}

#endif