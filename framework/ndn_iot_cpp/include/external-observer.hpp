// interface for external observer class

#ifndef __ndnrtc__addon__external__observer__
#define __ndnrtc__addon__external__observer__

namespace chrono_chat
{
  enum class MessageTypes
  {
    JOIN,
    LEAVE,
    CHAT
  };
  
  class ChatObserver
  {
  public:
    /**
     * The timestamp is considered as a double, which is the base type for ndn_Milliseconds in common.h
     */
    virtual void onStateChanged(MessageTypes type, const char *prefix, const char *userName, const char *msg, double timestamp) = 0;
  };
}

namespace entity_discovery
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
  
  class IDiscoveryObserver
  {
  public:
    /**
     * The timestamp is considered as a double, which is the base type for ndn_Milliseconds in common.h
     */
    virtual void onStateChanged(MessageTypes type, const char *msg, double timestamp) = 0;
  };
}

#endif