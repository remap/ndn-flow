// ConferenceDiscovery works with SyncBasedDiscovery, handling traffic under user prefix, 
// Similar with the  relationship between chrono-sync and chrono-chat

#ifndef __ndnrtc__addon__object__discovery__
#define __ndnrtc__addon__object__discovery__

#include <ndn-cpp/ndn-cpp-config.h>

#include <ndn-cpp/util/memory-content-cache.hpp>

#include <ndn-cpp/security/identity/memory-identity-storage.hpp>
#include <ndn-cpp/security/identity/memory-private-key-storage.hpp>
#include <ndn-cpp/security/policy/no-verify-policy-manager.hpp>
#include <ndn-cpp/transport/tcp-transport.hpp>

// remove dependency on boost header for this.
#include <boost/algorithm/string.hpp>

#include <sys/time.h>
#include <iostream>

#include "external-observer.hpp"

namespace entity_discovery
{
  typedef ndn::func_lib::function<void
    (const std::vector<std::string>& syncData)>
      OnReceivedSyncData;

  static ndn::MillisecondsSince1970 
  ndn_getNowMilliseconds()
  {
    struct timeval t;
    // Note: configure.ac requires gettimeofday.
    gettimeofday(&t, 0);
    return t.tv_sec * 1000.0 + t.tv_usec / 1000.0;
  }
  
  /**
   * This class, upon instantiation, will register a broadcast prefix and 
   * handle all that's going on in the broadcast namespace,
   * Namely periodical broadcast of sync digest, maintenance of outstanding interest, etc
   *
   * Its idea and implementations are pretty similar with ChronoSync, but does not have
   * a dependency upon a not-shrinking digest tree (whose nodes are names and sequence 
   * numbers) and log.
   */
  class SyncBasedDiscovery : public ndn::ptr_lib::enable_shared_from_this<SyncBasedDiscovery>
  {
  public:
  
    // Double check: does "const int& something" make sense?
    // Does initiation part in constructor implementation do a copy, or merely a reference?
    // Question: does this initiation part call the constructor, or do merely an equal: 
    // seems to be the former, which means, it copies, unless the variable being initialized is a reference member
    // Double check: the thing with cpp initialization sequence in constructor
    
    /**
     * Constructor.
     * @param broadcastPrefix The name prefix for broadcast. Hashes of discovered 
     * and published objects will be appended directly after broadcast prefix
     * @param onReceivedSyncData The callback for the action after receiving sync data.
     * @param face The broadcast face.
     * @param keyChain The keychain to sign things with.
     * @param certificateName The certificate name for locating the certificate.
     */
    SyncBasedDiscovery
      (ndn::Name broadcastPrefix, const OnReceivedSyncData& onReceivedSyncData, 
       ndn::Face& face, ndn::KeyChain& keyChain, ndn::Name certificateName)
     : broadcastPrefix_(broadcastPrefix), onReceivedSyncData_(onReceivedSyncData), 
       face_(face), keyChain_(keyChain), certificateName_(certificateName), 
       contentCache_(&face), newComerDigest_("00"), currentDigest_(newComerDigest_),
       defaultDataFreshnessPeriod_(2000), defaultInterestLifetime_(2000), enabled_(true)
    {
    }
    
    void start();
    
    /**
     * Destructor does not call shutdown by default, avoid crashing caused by deletion 
     * happening in a different thread.
     */
    ~SyncBasedDiscovery()
    {
    }
    
    /**
     * When calling shutdown, destroy all pending interests and remove all
     * registered prefixes.
     *
     * This should happen in the thread where face is accessed.
     */
    void
    shutdown()
    {
      contentCache_.unregisterAll(); 
      enabled_ = false;
    }
    
    /**
     * onData sorts both the object(string) array received, 
     * and the string array belonging to this object.
     * A lock for objects_ member?
     */
    void onData
      (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest,
       const ndn::ptr_lib::shared_ptr<ndn::Data>& data);
      
    void onTimeout
      (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest);
      
    void dummyOnData
      (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest,
       const ndn::ptr_lib::shared_ptr<ndn::Data>& data);
       
    void expressBroadcastInterest
      (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest);
    
    void onInterestCallback
      (const ndn::ptr_lib::shared_ptr<const ndn::Name>& prefix,
       const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, ndn::Face& face,
       uint64_t registeredPrefixId, const ndn::ptr_lib::shared_ptr<const ndn::InterestFilter>& filter);
    
    void onRegisterFailed
      (const ndn::ptr_lib::shared_ptr<const ndn::Name>& prefix);
    
    /**
     * contentCacheAdd copied from ChronoSync2013 implementation
     * Double check its logic
     *
     * Add the data packet to the contentCache_. Remove timed-out entries
     * from pendingInterestTable_. If the data packet satisfies any pending
     * interest, then send the data packet to the pending interest's transport
     * and remove from the pendingInterestTable_.
     * @param data
     */
    void contentCacheAdd(const ndn::Data& data);
    
    /**
     * Called when startPublishing, note that this function itself does 
     * add the published name to objects_. The caller should not call addObject to do it agin.
     * @ {string} name: the full (prefix+conferenceName).toUri()
     */
    void publishObject(std::string name);
    
    /**
     * Called when stopConferencePublishing
     */
    int stopPublishingObject(std::string name);
    
    /**
     * Updates the currentDigest_ according to the list of objects
     */
    void stringHash();
    void recomputeDigest();
    
    const std::string newComerDigest_;
    const ndn::Milliseconds defaultDataFreshnessPeriod_;
    const ndn::Milliseconds defaultInterestLifetime_;
    
    /**
     * These functions should be replaced, once we replace objects with something more
     * generic
     */
    std::vector<std::string> getObjects() { return objects_; };
    
    // addObject does not necessarily call updateHash
    int addObject(std::string object, bool updateDigest);
    
    // removeObject does not necessarily call updateHash
    int removeObject(std::string object, bool updateDigest);
    
    // To and from string method using \n as splitter
    std::string 
    objectsToString();
    
    static std::vector<std::string> stringToObjects(std::string str);
    
    /**
     * Should-be-replaced methods end
     */
    
    /**
     * PendingInterest class copied from ChronoSync2013 cpp implementation
     *
     * A PendingInterest holds an interest which onInterest received but could
     * not satisfy. When we add a new data packet to the contentCache_, we will
     * also check if it satisfies a pending interest.
     */
    class PendingInterest {
    public:
      /**
       * Create a new PendingInterest and set the timeoutTime_ based on the current time and the interest lifetime.
       * @param interest A shared_ptr for the interest.
       * @param transport The transport from the onInterest callback. If the
       * interest is satisfied later by a new data packet, we will send the data
       * packet to the transport.
       */
      PendingInterest
        (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest,
         ndn::Face& face);

      /**
       * Return the interest given to the constructor.
       */
      const ndn::ptr_lib::shared_ptr<const ndn::Interest>&
      getInterest() { return interest_; }

      /**
       * Return the transport given to the constructor.
       */
      ndn::Face&
      getFace() { return face_; }

      /**
       * Check if this interest is timed out.
       * @param nowMilliseconds The current time in milliseconds from ndn_getNowMilliseconds.
       * @return true if this interest timed out, otherwise false.
       */
      bool
      isTimedOut(ndn::MillisecondsSince1970 nowMilliseconds)
      {
        return timeoutTimeMilliseconds_ >= 0.0 && nowMilliseconds >= timeoutTimeMilliseconds_;
      }

    private:
      ndn::ptr_lib::shared_ptr<const ndn::Interest> interest_;
      ndn::Face& face_;
      ndn::MillisecondsSince1970 timeoutTimeMilliseconds_; /**< The time when the
        * interest times out in milliseconds according to ndn_getNowMilliseconds,
        * or -1 for no timeout. */
    };
    
  private:
    ndn::Name broadcastPrefix_;
    ndn::Name certificateName_;
    
    OnReceivedSyncData onReceivedSyncData_;
    
    ndn::Face& face_;
    ndn::MemoryContentCache contentCache_;
    
    ndn::KeyChain& keyChain_;
    
    // This serves as the rootDigest in ChronoSync.
    std::string currentDigest_;
    bool enabled_;
    
    // This serves as the list of objects to be synchronized. 
    // For now, it's the list of full conference names (prefix + conferenceName)
    // Could be replaced with a Protobuf class or a class later.
    std::vector<std::string> objects_;
    
    // PendingInterestTable for holding outstanding interests.
    std::vector<ndn::ptr_lib::shared_ptr<PendingInterest> > pendingInterestTable_;
  };
}

#endif