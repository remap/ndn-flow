// interface for external observer class

#ifndef __ndn_iot__external__observer__
#define __ndn_iot__external__observer__

namespace ndn_iot
{
namespace discovery
{
  // enum class won't compile with C++03
  enum class MessageTypes
  {
    ADD,        // Received when discovering conference
    REMOVE,        // Received when discovering conference end
    SET,        // Not being used
    START,        // Received when starting to host a conference
    STOP        // Received when hosted conference stops
  };
  
  class ExternalObserver
  {
  public:
    /**
     * The timestamp is considered as a double, which is the base type for ndn_Milliseconds in common.h
     */
    virtual void onStateChanged(MessageTypes type, const char *msg, double timestamp) = 0;
  };
}
}

#endif