#ifndef __ndn_iot__entity__info__
#define __ndn_iot__entity__info__

#include <ndn-cpp/util/blob.hpp>
#include <ndn-cpp/ndn-cpp-config.h>

namespace ndn_iot
{
namespace discovery
{
  // (TIMEOUTCOUNT + 1) timeouts in a row signifies conference dropped
  const int TIMEOUTCOUNT = 4;
  
  class EntityInfoBase
  {
  public:
    EntityInfoBase()
    {
      timeoutCount_ = 0;
      prefixId_ = -1;
      beingRemoved_ = false;
    }
    virtual ~EntityInfoBase(){}
    
    bool incrementTimeout()
    {
      if (timeoutCount_ ++ > TIMEOUTCOUNT) {
        return true;
      }
      else {
        return false;
      }
    }
    
    void resetTimeout() { timeoutCount_ = 0; }
    int getTimeoutCount() { return timeoutCount_; }
    
    uint64_t getRegisteredPrefixId() { return prefixId_; }
    void setRegisteredPrefixId(uint64_t prefixId) { prefixId_ = prefixId; }
    
    bool getBeingRemoved() { return beingRemoved_; }
    void setBeingRemoved(bool beingRemoved) { beingRemoved_ = beingRemoved; }
  protected:
    int timeoutCount_;
    uint64_t prefixId_;
    bool beingRemoved_;
  };
}
}

#endif