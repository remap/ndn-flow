// Authored by Zhehao on Aug 19, 2014
// This sync based discovery works similarly as ChronoSync, 
// however, it does not have a built-in sequence number or digest tree
// And the user does not have to be part of a digest tree to synchronize the root digest with others

#ifndef __ndnrtc__addon__entity__discovery__
#define __ndnrtc__addon__entity__discovery__

#include <ndn-cpp/ndn-cpp-config.h>

#include <ndn-cpp/util/memory-content-cache.hpp>

#include <ndn-cpp/security/identity/memory-identity-storage.hpp>
#include <ndn-cpp/security/identity/memory-private-key-storage.hpp>
#include <ndn-cpp/security/policy/no-verify-policy-manager.hpp>
#include <ndn-cpp/transport/tcp-transport.hpp>

#include <sys/time.h>
#include <iostream>

#include "sync-based-discovery.hpp"
#include "external-observer.hpp"
#include "entity-serializer.hpp"

// TODO: Constantly getting 'add entity' message during a test.
// TODO: Different lifetimes could cause interest towards stopped self hosted entity to get reissued;
//   explicit entity over still being verified.
// TODO: sync interest does not seem to timeout; for an add/remove the same entry; freshness period problem?
// TODO: Moving timeout count and other variables out of EntityInfoBase, so that passing nil entityInfo(from deserialize) to this library
//   won't cause problems.

// Updates 2016
// TODO: digest interest length check; the unexpected prefix chrono-chat0.3 of ndncon's and flooding of local nfd issue

namespace entity_discovery
{
  class EntityDiscovery : public ndn::ptr_lib::enable_shared_from_this<EntityDiscovery>
  {
  public:
    /**
     * Constructor
     * @param broadcastPrefix broadcastPrefix The name prefix for broadcast. Hashes of discovered 
     * and published objects will be appended directly after broadcast prefix
     * @param observer The observer class for receiving and displaying discovery messages.
     * @param face The face for broadcast sync and multicast fetch interest.
     * @param keyChain The keychain to sign things with.
     * @param certificateName The certificate name for locating the certificate.
     */
    EntityDiscovery
      (std::string broadcastPrefix, IDiscoveryObserver *observer, 
       ndn::ptr_lib::shared_ptr<IEntitySerializer> serializer, ndn::Face& face, ndn::KeyChain& keyChain, 
       ndn::Name certificateName)
    :  defaultDataFreshnessPeriod_(2000), defaultKeepPeriod_(3000), 
       defaultHeartbeatInterval_(2000), defaultTimeoutReexpressInterval_(300), 
       broadcastPrefix_(broadcastPrefix), observer_(observer), serializer_(serializer), 
       faceProcessor_(face), keyChain_(keyChain), 
       certificateName_(certificateName), hostedEntitiesNum_(0), enabled_(true)
    {
    };
  
    void
    start();
  
    /**
     * Publish entity publishes entity to be discovered by the broadcastPrefix in 
     * the constructor.
     * It registers prefix for the intended entity name, 
     * if local peer's not publishing before
     * @param entityName string name of the entity.
     * @param localPrefix name prefix of the entity. (localPrefix + entityName) is the full name of the entity
     * @param entityInfo the info of this entity.
     * @return true, if entity name is not already published by this instance; false if otherwise.
     */
    bool 
    publishEntity(std::string entityName, ndn::Name localPrefix, ndn::ptr_lib::shared_ptr<EntityInfoBase> entityInfo);
  
    /**
     * Stop publishing the entity of this instance. 
     * Also removes the registered prefix by its ID.
     * Note that interest with matching name could still arrive, but will not trigger
     * onInterest.
     * @param entityName string name of the entity to be stopped
     * @param prefix name prefix of the entity to be stopped
     * @return true, if entity with that name is published by this instance; false if otherwise.
     */
    bool
    stopPublishingEntity(std::string entityName, ndn::Name prefix);
    
    /**
     * getDiscoveredEntityList returns the copy of list of discovered entities
     */
    std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>
    getDiscoveredEntityList() { return discoveredEntityList_; };
    
    /**
     * getHostedEntityList returns the copy of list of hosted entities
     */
    std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>
    getHostedEntityList() { return hostedEntityList_; };
    
    /**
     * getEntity gets the entity info from list of entitys discovered or hosted
     * The first entity with matching name will get returned.
     */
    ndn::ptr_lib::shared_ptr<EntityInfoBase>
    getEntity(std::string entityName); 
    
    ~EntityDiscovery() 
    {
    };
    
    /**
     * When calling shutdown, destroy all pending interests and remove all
     * registered prefixes.
     *
     * This accesses face, and should be called in the thread where face is accessed
     */
    void 
    shutdown();
    
  private:
    /**
     * onReceivedSyncData is passed into syncBasedDiscovery, and called whenever 
     * syncData is received in syncBasedDiscovery.
     */
    void 
    onReceivedSyncData(const std::vector<std::string>& syncData);

    /**
     * When receiving interest about the entity hosted locally, 
     * respond with a string that tells the interest issuer that this entity is ongoing
     */
    void 
    onInterestCallback
      (const ndn::ptr_lib::shared_ptr<const ndn::Name>& prefix,
       const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, ndn::Face& face,
       uint64_t registeredPrefixId, const ndn::ptr_lib::shared_ptr<const ndn::InterestFilter>& filter);
    
    /**
     * Handles the ondata event for entity querying interest
     * For now, whenever data is received means the entity in question is ongoing.
     * The content should be entity description for later uses.
     */
    void 
    onData
      (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest,
       const ndn::ptr_lib::shared_ptr<ndn::Data>& data);
  
    /**
     * expressHeartbeatInterest expresses the interest for certain entity again,
     * to learn if the entity is still going on. This is done like heartbeat with a 2 seconds
     * default interval. Should switch to more efficient mechanism.
     */
    void
    expressHeartbeatInterest
      (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest,
       const ndn::ptr_lib::shared_ptr<const ndn::Interest>& entityInterest);
    
    /**
     * Remove registered prefix happens after a few seconds after stop hosting entity;
     * So that other peers may fetch "entity over" with heartbeat interest.
     */
    void
    removeRegisteredPrefix
      (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest,
       ndn::Name entityName);
    
    /**
     * This works as expressHeartbeatInterest's onData callback.
     * Should switch to more efficient mechanism.
     */
    void
    onHeartbeatData
      (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest,
       const ndn::ptr_lib::shared_ptr<ndn::Data>& data);
  
    void 
    dummyOnData
      (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest,
       const ndn::ptr_lib::shared_ptr<ndn::Data>& data)
    {
      std::cout << "Dummy onData called by EntityDiscovery." << std::endl;
    };
  
    /**
     * Handles the timeout event for unicast entity querying interest:
     * For now, receiving one timeout means the entity being queried is over.
     * This strategy is immature and should be replaced.
     */
    void
    onTimeout
      (const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest);
  
    void
    onRegisterFailed
      (const ndn::ptr_lib::shared_ptr<const ndn::Name>& prefix)
    {
      std::cout << "Prefix " << prefix->toUri() << " registration failed." << std::endl;
    };
    
    std::string entitiesToString();
    
    void 
    notifyObserver(MessageTypes type, const char *msg, double timestamp);
    
    ndn::Face& faceProcessor_;
    ndn::KeyChain& keyChain_;
    
    ndn::Name certificateName_;
    std::string broadcastPrefix_;
    
    int hostedEntitiesNum_;
    bool enabled_;
    
    const ndn::Milliseconds defaultDataFreshnessPeriod_;
    const ndn::Milliseconds defaultKeepPeriod_;
    const ndn::Milliseconds defaultHeartbeatInterval_;
    const ndn::Milliseconds defaultTimeoutReexpressInterval_;
  
    // List of discovered entities.
    std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>> discoveredEntityList_;
    // List of entities that are being expressed interest towards,
    // this includes discoveredEntitiesList_ and unverified entities.
    std::vector<std::string> queriedEntityList_;
    
    // List of hosted entities.
    std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>> hostedEntityList_;
    
    ndn::ptr_lib::shared_ptr<SyncBasedDiscovery> syncBasedDiscovery_;
    IDiscoveryObserver *observer_;
    
    ndn::ptr_lib::shared_ptr<IEntitySerializer> serializer_;
  };
}

#endif