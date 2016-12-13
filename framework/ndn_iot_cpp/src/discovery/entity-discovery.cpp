#include "entity-discovery.hpp"
#include "sync-based-discovery.hpp"
#include <sys/time.h>
#include <iostream>

#include <algorithm>

using namespace std;
using namespace ndn;
using namespace ndn::func_lib;
using namespace ndn_iot::discovery;

#if NDN_CPP_HAVE_STD_FUNCTION && NDN_CPP_WITH_STD_FUNCTION
  using namespace func_lib::placeholders;
#endif

void
EntityDiscovery::shutdown()
{
  syncBasedDiscovery_->shutdown();
  enabled_ = false;
  
  for (std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>::iterator it = hostedEntityList_.begin(); it != hostedEntityList_.end(); it++) {
    faceProcessor_.removeRegisteredPrefix(it->second->getRegisteredPrefixId());
  }
}

void
EntityDiscovery::start()
{
  enabled_ = true;
  
  try {  
    syncBasedDiscovery_.reset(new SyncBasedDiscovery
    (broadcastPrefix_, bind(&EntityDiscovery::onReceivedSyncData, shared_from_this(), _1), 
     faceProcessor_, keyChain_, certificateName_));
  } catch(const std::bad_weak_ptr& e) {
    // no shared_ptr reference to EntityDiscovery yet, so shared_from_this() will complain about bad weak ptr
    cout << e.what() << '\n';
    syncBasedDiscovery_.reset(new SyncBasedDiscovery
    (broadcastPrefix_, bind(&EntityDiscovery::onReceivedSyncData, this, _1), 
     faceProcessor_, keyChain_, certificateName_));
  }
  syncBasedDiscovery_->start();
}

ndn::ptr_lib::shared_ptr<EntityInfoBase>
EntityDiscovery::getEntity(std::string entityName)
{
  std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>::iterator item = discoveredEntityList_.find
    (entityName);
  if (item != discoveredEntityList_.end()) {
    return item->second;
  }
  else {
    std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>::iterator hostedItem = hostedEntityList_.find
      (entityName);
    if (hostedItem != hostedEntityList_.end()) {
      return hostedItem->second;
    }
    else {
      return ndn::ptr_lib::shared_ptr<EntityInfoBase>();
    }
  }
}

bool 
EntityDiscovery::publishEntity
  (Name entityName, ptr_lib::shared_ptr<EntityInfoBase> entityInfo) 
{
  Name entityFullName(entityName);
    
  std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>::iterator item = hostedEntityList_.find(entityFullName.toUri());
  if (item == hostedEntityList_.end()) {

    uint64_t registeredPrefixId = faceProcessor_.registerPrefix
      (entityFullName, 
       (const ndn::OnInterestCallback&)bind(&EntityDiscovery::onInterestCallback, this, _1, _2, _3, _4, _5), 
       bind(&EntityDiscovery::onRegisterFailed, this, _1));
  
    syncBasedDiscovery_->publishObject(entityFullName.toUri());
  
    // this destroys the parent class object.
    ptr_lib::shared_ptr<EntityInfoBase> info = entityInfo;
    info->setRegisteredPrefixId(registeredPrefixId);
  
    hostedEntityList_.insert
      (std::pair<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>(entityFullName.toUri(), info));
  
    notifyObserver(MessageTypes::START, entityFullName.toUri().c_str(), 0);
    hostedEntitiesNum_ ++;
    return true;
  }
  else {
    // For the same entity name published again, we update its EntityInfoBase object
    ptr_lib::shared_ptr<EntityInfoBase> info = entityInfo;
    info->setRegisteredPrefixId(item->second->getRegisteredPrefixId());
    item->second = info;
    
    // SET is called for notifyObserver
    notifyObserver(MessageTypes::SET, entityFullName.toUri().c_str(), 0);
    return true;
  }
}

void
EntityDiscovery::removeRegisteredPrefix
  (const ptr_lib::shared_ptr<const Interest>& interest,
   Name entityName)
{ 
  std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>::iterator item = hostedEntityList_.find(entityName.toUri());
  if (item != hostedEntityList_.end()) {
    faceProcessor_.removeRegisteredPrefix(item->second->getRegisteredPrefixId());
    hostedEntityList_.erase(item);
    hostedEntitiesNum_ --;
  }
  else {
    cerr << "No such entity exists." << endl;
  }
}

bool
EntityDiscovery::stopPublishingEntity
  (std::string entityName, ndn::Name prefix)
{
  if (hostedEntitiesNum_ > 0) {
    Name entityBeingStopped = Name(prefix).append(entityName);
    
    std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>::iterator item = hostedEntityList_.find(entityBeingStopped.toUri());
  
    if (item != hostedEntityList_.end()) {
      item->second->setBeingRemoved(true);
      syncBasedDiscovery_->removeObject(entityBeingStopped.toUri(), true);
      
      Interest timeout("/local/timeout");
      timeout.setInterestLifetimeMilliseconds(defaultKeepPeriod_);

      faceProcessor_.expressInterest
        (timeout, bind(&EntityDiscovery::dummyOnData, this, _1, _2),
         bind(&EntityDiscovery::removeRegisteredPrefix, this, _1, entityBeingStopped));
    
      notifyObserver(MessageTypes::STOP, entityBeingStopped.toUri().c_str(), 0);
      
      return true;
    }
    else {
      cerr << "No such entity exists." << endl;
      return false;
    }
  }
  else {
    cerr << "Not hosting any entities." << endl;
    return false;
  }
}

void 
EntityDiscovery::onReceivedSyncData
  (const std::vector<std::string>& syncData)
{
  if (!enabled_)
    return ;
    
  for (size_t j = 0; j < syncData.size(); ++j) {
    std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>::iterator hostedItem = hostedEntityList_.find(syncData[j]);
    std::vector<std::string>::iterator queriedItem = std::find(queriedEntityList_.begin(), queriedEntityList_.end(), syncData[j]);
    
    if (hostedItem == hostedEntityList_.end() && queriedItem == queriedEntityList_.end()) {
      queriedEntityList_.push_back(syncData[j]);
    
      Name name(syncData[j]);
      Interest interest(name);
      
      interest.setInterestLifetimeMilliseconds(defaultHeartbeatInterval_);
      interest.setMustBeFresh(true);
      
      faceProcessor_.expressInterest
        (interest, bind(&EntityDiscovery::onData, this, _1, _2),
         bind(&EntityDiscovery::onTimeout, this, _1));
    }
  }
}

void
EntityDiscovery::onInterestCallback
  (const ndn::ptr_lib::shared_ptr<const ndn::Name>& prefix,
   const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, ndn::Face& face,
   uint64_t registeredPrefixId, const ndn::ptr_lib::shared_ptr<const ndn::InterestFilter>& filter)
{
  if (!enabled_)
    return ;
    
  std::map<std::string, ndn::ptr_lib::shared_ptr<EntityInfoBase>>::iterator item = hostedEntityList_.find(interest->getName().toUri());
  
  if (item != hostedEntityList_.end()) {
  
    Data data(interest->getName());
  
    if (item->second->getBeingRemoved() == false) {
      data.setContent(serializer_->serialize(item->second));
    } else {
      string content("over");
      data.setContent((const uint8_t *)&content[0], content.size());
    }
    
    data.getMetaInfo().setFreshnessPeriod(defaultDataFreshnessPeriod_);

    keyChain_.sign(data, certificateName_);

    face.putData(data);
  }
  else {
    cerr << "Received interest about entity not hosted by this instance." << endl;
  }
}

void 
EntityDiscovery::onData
  (const ptr_lib::shared_ptr<const Interest>& interest,
   const ptr_lib::shared_ptr<Data>& data)
{
  if (!enabled_)
    return ;
    
  std::string entityName = interest->getName().toUri();
  
  std::map<string, ptr_lib::shared_ptr<EntityInfoBase>>::iterator item = discoveredEntityList_.find
    (entityName);
  
  string content = "";
  for (size_t i = 0; i < data->getContent().size(); ++i) {
    content += (*data->getContent())[i];
  }
  
  // if it's not an already discovered entity
  if (item == discoveredEntityList_.end()) {
    // if it's still going on
    if (content != "over") {
      ptr_lib::shared_ptr<EntityInfoBase> entityInfo = serializer_->deserialize(data->getContent());
      
      if (entityInfo) {
        discoveredEntityList_.insert
          (std::pair<string, ptr_lib::shared_ptr<EntityInfoBase>>
            (entityName, entityInfo));
  
        // std::map should be sorted by default
        //std::sort(discoveredEntityList_.begin(), discoveredEntityList_.end());

        // Probably need lock for adding/removing objects in SyncBasedDiscovery class.
        // Here we update hash as well as adding object; The next interest will carry the new digest

        // Expect this to be equal with 0 several times. 
        // Because new digest does not get updated immediately
        if (syncBasedDiscovery_->addObject(entityName, true) == 0) {
          cerr << "Did not add to the discoveredEntityList_ in syncBasedDiscovery_" << endl;
        }

        notifyObserver(MessageTypes::ADD, entityName.c_str(), 0);

        Interest timeout("/local/timeout");
        timeout.setInterestLifetimeMilliseconds(defaultHeartbeatInterval_);

        // express heartbeat interest after 2 seconds of sleep
        faceProcessor_.expressInterest
          (timeout, bind(&EntityDiscovery::dummyOnData, this, _1, _2),
           bind(&EntityDiscovery::expressHeartbeatInterest, this, _1, interest));
      }
      else {
        // If received entityInfo is malformed, 
        // re-express interest after a timeout.
        Interest timeout("/local/timeout");
        timeout.setInterestLifetimeMilliseconds(defaultHeartbeatInterval_);

        // express heartbeat interest after 2 seconds of sleep
        faceProcessor_.expressInterest
          (timeout, bind(&EntityDiscovery::dummyOnData, this, _1, _2),
           bind(&EntityDiscovery::expressHeartbeatInterest, this, _1, interest));
      }
    }
    // if the not already discovered entity is already over.
    else {
      std::vector<string>::iterator queriedItem = std::find
        (queriedEntityList_.begin(), queriedEntityList_.end(), entityName);
      if (queriedItem != queriedEntityList_.end()) {
        queriedEntityList_.erase(queriedItem);
      }
    }
  }
  // if it's an already discovered entity
  else {
    if (content != "over") {
      item->second->resetTimeout();
      
      // Using set messages for updated entitys
      ptr_lib::shared_ptr<EntityInfoBase> entityInfo = serializer_->deserialize(data->getContent());
      
      if (!serializer_->serialize(entityInfo).equals(serializer_->serialize(item->second))) {
        item->second = entityInfo;
        
        notifyObserver(MessageTypes::SET, entityName.c_str(), 0);
      }
      
      Interest timeout("/local/timeout");
      timeout.setInterestLifetimeMilliseconds(defaultHeartbeatInterval_);

      // express heartbeat interest after 2 seconds of sleep
      faceProcessor_.expressInterest
        (timeout, bind(&EntityDiscovery::dummyOnData, this, _1, _2),
         bind(&EntityDiscovery::expressHeartbeatInterest, this, _1, interest));
    }
    // If the discovered entity marks itself as "over"; This is updated to use "STOP" instead of "REMOVE", so that the latter's easier to differentiate, and is only caused by a series of interest timeouts
    else {
      notifyObserver(MessageTypes::STOP, entityName.c_str(), 0);
      
      if (syncBasedDiscovery_->removeObject(item->first, true) == 0) {
        cerr << "Did not remove from the discoveredEntityList_ in syncBasedDiscovery_" << endl;
      }
      std::vector<string>::iterator queriedItem = std::find
        (queriedEntityList_.begin(), queriedEntityList_.end(), entityName);
      if (queriedItem != queriedEntityList_.end()) {
        queriedEntityList_.erase(queriedItem);
      }
      discoveredEntityList_.erase(item);
    }
  }
}

/**
 * When interest times out, increment the timeout count for that entity;
 * If timeout count reaches maximum, delete entity;
 * If not, express interest again.
 */
void
EntityDiscovery::onTimeout
  (const ptr_lib::shared_ptr<const Interest>& interest)
{
  if (!enabled_)
    return ;
    
  // entityName is the full name of the entity, with the last component being the entity name string.
  std::string entityName = interest->getName().toUri();
  
  std::map<string, ptr_lib::shared_ptr<EntityInfoBase>>::iterator item = discoveredEntityList_.find
    (entityName);
  if (item != discoveredEntityList_.end()) {
    // TODO: This seems to be the only way of getting REMOVE (manual stop gets you STOP, instead of REMOVE); see how this's called
    cerr << "** Entity heartbeat interest timeout called, interest: " << interest->getName().toUri() << "; Current timeout count: " << item->second->getTimeoutCount() << endl;
    if (item->second && item->second->incrementTimeout()) {
      notifyObserver(MessageTypes::REMOVE, entityName.c_str(), 0);
      
      // Probably need lock for adding/removing objects in SyncBasedDiscovery class.
      if (syncBasedDiscovery_->removeObject(item->first, true) == 0) {
        cerr << "Did not remove from the discoveredEntityList_ in syncBasedDiscovery_" << endl;
      }
  
      // erase the item after it's removed in removeObject, or removeObject would remove the
      // wrong element: iterator is actually representing a position index, and the two vectors
      // should be exactly the same: (does it make sense for them to be shared, 
      // and mutex-locked correspondingly?)
      discoveredEntityList_.erase(item);
      
      std::vector<string>::iterator queriedItem = std::find
        (queriedEntityList_.begin(), queriedEntityList_.end(), entityName);
      if (queriedItem != queriedEntityList_.end()) {
        queriedEntityList_.erase(queriedItem);
      }
    }
    else {
      Interest timeout("/local/timeout");
      timeout.setInterestLifetimeMilliseconds(defaultTimeoutReexpressInterval_);
      faceProcessor_.expressInterest
        (timeout, bind(&EntityDiscovery::dummyOnData, this, _1, _2),
         bind(&EntityDiscovery::expressHeartbeatInterest, this, _1, interest));
    }
  }
  else {
    std::vector<string>::iterator queriedItem = std::find
      (queriedEntityList_.begin(), queriedEntityList_.end(), entityName);
    if (queriedItem != queriedEntityList_.end()) {
      queriedEntityList_.erase(queriedItem);
    } 
  }
}

void
EntityDiscovery::expressHeartbeatInterest
  (const ptr_lib::shared_ptr<const Interest>& interest,
   const ptr_lib::shared_ptr<const Interest>& entityInterest)
{
  if (!enabled_)
    return ;
  
  Interest newInterest(entityInterest->getName());
  
  newInterest.setInterestLifetimeMilliseconds(defaultHeartbeatInterval_);
  newInterest.setMustBeFresh(true);
  
  faceProcessor_.expressInterest
    (newInterest,
     bind(&EntityDiscovery::onData, this, _1, _2), 
     bind(&EntityDiscovery::onTimeout, this, _1));
}

void 
EntityDiscovery::notifyObserver(MessageTypes type, const char *msg, double timestamp)
{
  if (observer_) {
    observer_->onStateChanged(type, msg, timestamp);
  }
  else {
    string state = "";
    switch (type) {
      case MessageTypes::ADD:         state = "Add"; break;
      case MessageTypes::REMOVE:    state = "Remove"; break;
      case MessageTypes::SET:        state = "Set"; break;
      case MessageTypes::START:        state = "Start"; break;
      case MessageTypes::STOP:        state = "Stop"; break;
      default:                        state = "Unknown"; break;
    }
    cout << state << " " << timestamp << "\t" << msg << endl;
  }
}

std::string
EntityDiscovery::entitiesToString()
{
  std::string result;
  for(std::map<string, ptr_lib::shared_ptr<EntityInfoBase>>::iterator it = discoveredEntityList_.begin(); it != discoveredEntityList_.end(); ++it) {
    result += it->first;
    result += "\n";
  }
  for(std::map<string, ptr_lib::shared_ptr<EntityInfoBase>>::iterator it = hostedEntityList_.begin(); it != hostedEntityList_.end(); ++it) {
    result += (" * " + it->first);
    result += "\n";
  }
  return result;
}