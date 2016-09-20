#include "sync-based-discovery.hpp"
#include <sys/time.h>
#include <openssl/ssl.h>
#include <iostream>

using namespace std;
using namespace ndn;
using namespace ndn::func_lib;
using namespace entity_discovery;

#if NDN_CPP_HAVE_STD_FUNCTION && NDN_CPP_WITH_STD_FUNCTION
using namespace func_lib::placeholders;
#endif

void 
SyncBasedDiscovery::onData
  (const ptr_lib::shared_ptr<const Interest>& interest,
   const ptr_lib::shared_ptr<Data>& data)
{
  if (!enabled_)
    return ;
  string content;
  for (size_t i = 0; i < data->getContent().size(); ++i)
    content += (*data->getContent())[i];
  
  vector<std::string> objects = SyncBasedDiscovery::stringToObjects(content);
  
  // Finding vector differences by using existing function, 
  // which requires both vectors to be sorted.
  std::sort(objects.begin(), objects.end());
  std::sort(objects_.begin(), objects_.end());

  std::vector<std::string> setDifferences;

  std::set_symmetric_difference
    (objects.begin(),
     objects.end(),
     objects_.begin(),
     objects_.end(),
     std::back_inserter(setDifferences));
  
  onReceivedSyncData_(setDifferences);
  
  // Express interest again immediately may not be the best idea...
  // Try expressing in a given timeout period: 
  // Why it does not work as expected, without this interval?
  
  Interest timeout("/local/timeout");
  timeout.setInterestLifetimeMilliseconds(defaultInterestLifetime_);
  
  face_.expressInterest
    (timeout, bind(&SyncBasedDiscovery::dummyOnData, shared_from_this(), _1, _2),
     bind(&SyncBasedDiscovery::expressBroadcastInterest, shared_from_this(), _1));
  return;
}

void
SyncBasedDiscovery::expressBroadcastInterest
  (const ptr_lib::shared_ptr<const Interest>& interest)
{
  Name name(broadcastPrefix_);
  name.append(currentDigest_);

  Interest newInterest(name);
  newInterest.setInterestLifetimeMilliseconds(defaultInterestLifetime_);
  newInterest.setMustBeFresh(true);
  
  face_.expressInterest
    (newInterest, bind(&SyncBasedDiscovery::onData, shared_from_this(), _1, _2),
     bind(&SyncBasedDiscovery::onTimeout, shared_from_this(), _1));
  return;
}

void 
SyncBasedDiscovery::dummyOnData
  (const ptr_lib::shared_ptr<const Interest>& interest,
   const ptr_lib::shared_ptr<Data>& data)
{
  if (!enabled_)
    return ;
  cerr << "Dummy onData called." << endl;
  throw std::runtime_error("Discovery library called dummyOnData, which shouldn't be called in any case.\n");
  return;
}

void 
SyncBasedDiscovery::onTimeout
  (const ptr_lib::shared_ptr<const Interest>& interest)
{
  if (!enabled_)
    return ;
  Name interestName(broadcastPrefix_);
  interestName.append(currentDigest_);
  
  Interest newInterest(interestName);
  newInterest.setInterestLifetimeMilliseconds(defaultInterestLifetime_);
  newInterest.setMustBeFresh(true);

  face_.expressInterest
    (newInterest, bind(&SyncBasedDiscovery::onData, shared_from_this(), _1, _2), 
     bind(&SyncBasedDiscovery::onTimeout, shared_from_this(), _1));
}

void 
SyncBasedDiscovery::onInterestCallback
  (const ndn::ptr_lib::shared_ptr<const ndn::Name>& prefix,
   const ndn::ptr_lib::shared_ptr<const ndn::Interest>& interest, ndn::Face& face,
   uint64_t registeredPrefixId, const ndn::ptr_lib::shared_ptr<const ndn::InterestFilter>& filter)
{
  if (!enabled_)
    return ;
  string syncDigest = interest->getName().get
    (broadcastPrefix_.size()).toEscapedString();
    
  if (syncDigest != currentDigest_) {
    // the syncDigest differs from the local knowledge, reply with local knowledge
    // We don't have digestLog implemented in this, so incremental replying is not considered now
    
    // It could potentially cause one name corresponds with different data in different locations,
    // Same as publishing two conferences simultaneously: such errors should be correctible
    // later steps
    Data data(interest->getName());
    std::string content = objectsToString();
    
    data.setContent((const uint8_t *)&content[0], content.size());
    
    data.getMetaInfo().setFreshnessPeriod(defaultDataFreshnessPeriod_);
    
    keyChain_.sign(data, certificateName_);
    face.putData(data);
  }
  else if (syncDigest != newComerDigest_) {
    // Store this steady-state (outstanding) interest in application PIT, unless neither the sender
    // nor receiver knows anything
    pendingInterestTable_.push_back(ptr_lib::shared_ptr<PendingInterest>
      (new PendingInterest(interest, face)));
  }
}

void 
SyncBasedDiscovery::onRegisterFailed
  (const ptr_lib::shared_ptr<const Name>& prefix)
{
  if (!enabled_)
    return ;
  ostringstream ss;
  ss << "Prefix registration for name " << prefix->toUri() << " failed." << endl;
  cerr << ss.str();
  throw std::runtime_error(ss.str());
}

// Digest methods are not yet tested
void
SyncBasedDiscovery::recomputeDigest()
{
  SHA256_CTX sha256;
  SHA256_Init(&sha256);
  
  for (std::vector<std::string>::iterator it = objects_.begin(); it != objects_.end(); ++it) {
    SHA256_Update(&sha256, &((*it)[0]), it->size());
  }
  
  uint8_t currentDigest[SHA256_DIGEST_LENGTH];
  SHA256_Final(&currentDigest[0], &sha256);
  
  currentDigest_ = toHex(currentDigest, sizeof(currentDigest));
}

void
SyncBasedDiscovery::stringHash()
{

}

int
SyncBasedDiscovery::stopPublishingObject(std::string name)
{
  return removeObject(name, true);
}

void
SyncBasedDiscovery::publishObject(std::string name)
{
  // addObject sorts the objects array
  // Here using addObject without updating hash immediately, because we want the content cache
  // to store the data {name: old digest, content: the new dataset}
  // We update hash and express interest about the new hash after 
  // storing the above mentioned stuff in the content cache
  
  if (addObject(name, false)) {
  
    // Do not add itself to contentCache if its currentDigest is "00".
    if (currentDigest_ != newComerDigest_) {
      Name dataName = Name(broadcastPrefix_).append(currentDigest_);
      Data data(dataName);
    
      std::string content = objectsToString();
      data.setContent((const uint8_t *)&content[0], content.size());
      data.getMetaInfo().setFreshnessPeriod(defaultDataFreshnessPeriod_);
    
      keyChain_.sign(data, certificateName_);
    
      contentCacheAdd(data);
    }
    
    recomputeDigest();
    
    Name interestName = Name(broadcastPrefix_).append(currentDigest_);
    Interest interest(interestName);
    interest.setInterestLifetimeMilliseconds(defaultInterestLifetime_);
    interest.setMustBeFresh(true);
    
    face_.expressInterest
      (interest, bind(&SyncBasedDiscovery::onData, shared_from_this(), _1, _2), 
       bind(&SyncBasedDiscovery::onTimeout, shared_from_this(), _1));
  }
  else {
    cerr << "Object already exists." << endl;
  }
}

void
SyncBasedDiscovery::contentCacheAdd(const Data& data)
{
  contentCache_.add(data);

  // Remove timed-out interests and check if the data packet matches any pending
  // interest.
  // Go backwards through the list so we can erase entries.
  MillisecondsSince1970 nowMilliseconds = ndn_getNowMilliseconds();
  for (int i = (int)pendingInterestTable_.size() - 1; i >= 0; --i) {
    if (pendingInterestTable_[i]->isTimedOut(nowMilliseconds)) {
      pendingInterestTable_.erase(pendingInterestTable_.begin() + i);
      continue;
    }

    if (pendingInterestTable_[i]->getInterest()->matchesName(data.getName())) {
      try {
        // Send to the same transport from the original call to onInterest.
        // wireEncode returns the cached encoding if available.
        pendingInterestTable_[i]->getFace().putData(data);
      }
      catch (std::exception& e) {
        // print the error message and throw again.
        cerr << e.what() << endl;
        throw;
      }

      // The pending interest is satisfied, so remove it.
      pendingInterestTable_.erase(pendingInterestTable_.begin() + i);
    }
  }
}

SyncBasedDiscovery::PendingInterest::PendingInterest
  (const ptr_lib::shared_ptr<const Interest>& interest, Face& face)
  : interest_(interest), face_(face)
{
  // Set up timeoutTime_.
  if (interest_->getInterestLifetimeMilliseconds() >= 0.0)
    timeoutTimeMilliseconds_ = ndn_getNowMilliseconds() +
      interest_->getInterestLifetimeMilliseconds();
  else
    // No timeout.
    timeoutTimeMilliseconds_ = -1.0;
}