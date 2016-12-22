using System;
using System.Collections.Generic;
using System.Text;
using ILOG.J2CsMapping.NIO;

using net.named_data.jndn;
using net.named_data.jndn.util;
using net.named_data.jndn.transport;
using net.named_data.jndn.security;
using net.named_data.jndn.security.identity;
using net.named_data.jndn.security.policy;

using ndn_iot.bootstrap;

class PhoneHandler {
  public Face face_;
  public KeyChain keyChain_;
  public Name certificateName_;

  public MemoryContentCache memoryContentCache_;

  public Bootstrap bootstrap_;

  public const string hostName_ = "localhost";
  public const string instanceName_ = "/home/flow-csharp";
  
  public const string fetchVerb = "fetch";
  public const string linkVerb = "link";
  public const int segmentSize = 2000;
  public const int defaultDataFreshnessPeriod = 6000;

  public Name fetchPrefix_;
  public Name linkPrefix_;

  public void start() {
    // generic face setup
    face_ = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo(hostName_));

    bootstrap_ = new Bootstrap(face_);
    keyChain_ = bootstrap_.setupDefaultIdentityAndRoot(new Name(instanceName_), new Name());
    certificateName_ = bootstrap_.getDefaultCertificateName();

    // class-specific start
    memoryContentCache_ = new MemoryContentCache(face_);

    fetchPrefix_ = new Name(instanceName_).append(fetchVerb);
    linkPrefix_ = new Name(instanceName_).append(linkVerb);
    
    FetchInterestHandler fh = new FetchInterestHandler(this);
    memoryContentCache_.registerPrefix(fetchPrefix_, fh, fh);

    LinkInterestHandler lh = new LinkInterestHandler(this);
    face_.registerPrefix(linkPrefix_, lh, lh);

    // publish html content for given mobile, call when needed
    // for this example call this on start
    string htmlString = "<p>Hello world!</p>";
    string mobileName = "/home/browser1";
    publishHtmlForMobile(mobileName, htmlString);

    // update event
    while (true) {
      face_.processEvents();
      System.Threading.Thread.Sleep(5);
    }
  }

  public void publishHtmlForMobile(string mobileName, string htmlString, string identifier = "") {
    int startIdx = 0;
    int endIdx = 0;

    int finalBlockNumber = (int)Math.Floor((Double)htmlString.Length / segmentSize);
    int currentBlockNumber = 0;

    // by default, append the current timestamp as version number to differentiate the data
    // otherwise use the given component
    string version = "";
    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
    int secondsSinceEpoch = (int)t.TotalSeconds;

    if (version == "") {
      version = secondsSinceEpoch.ToString();
    }

    while (startIdx < htmlString.Length) {
      Data data = new Data(new Name(fetchPrefix_).append(mobileName).append(version).append(Name.Component.fromSegment(currentBlockNumber)));
      Console.Out.WriteLine("added data: " + data.getName().toUri());
      endIdx = htmlString.Length > (startIdx + segmentSize) ? (startIdx + segmentSize) : htmlString.Length;
      data.setContent(new Blob(htmlString.Substring(startIdx, endIdx)));
      data.getMetaInfo().setFinalBlockId(Name.Component.fromSegment(finalBlockNumber));
      data.getMetaInfo().setFreshnessPeriod(defaultDataFreshnessPeriod);

      startIdx = endIdx;
      currentBlockNumber += 1;
      memoryContentCache_.add(data);
    }
    return;
  }

  class FetchInterestHandler : OnInterestCallback, OnRegisterFailed {
    public FetchInterestHandler(PhoneHandler handler) {
      phoneHandler_ = handler;
    }

    public void onInterest(Name prefix, Interest interest, Face face, long interestFilterId,
      InterestFilter filter)
    {
      Console.Out.WriteLine("Data not found: " + interest.getName().toUri());
    }

    public void onRegisterFailed(Name prefix) {
      Console.Out.WriteLine("Register failed for prefix: " + prefix.toUri());
    }

    PhoneHandler phoneHandler_;
  }

  class LinkInterestHandler : OnInterestCallback, OnRegisterFailed {
    public LinkInterestHandler(PhoneHandler handler) {
      phoneHandler_ = handler;
    }

    public void onInterest(Name prefix, Interest interest, Face face, long interestFilterId,
      InterestFilter filter)
    {
      string phoneId = interest.getName().get(phoneHandler_.linkPrefix_.size()).toEscapedString();
      string linkContent = interest.getName().get(phoneHandler_.linkPrefix_.size() + 1).toEscapedString();
      
      Console.Out.WriteLine("User " + phoneId + " clicked link \"" + linkContent + "\"");

      var data = new Data(interest.getName());
      var content = "User " + phoneId + " clicked link \"" + linkContent + "\"";
      data.setContent(new Blob(content));
      data.getMetaInfo().setFreshnessPeriod(2000);

      try {
        phoneHandler_.keyChain_.sign(data, phoneHandler_.certificateName_);      
      } catch (SecurityException exception) {
        // Don't expect this to happen.
        throw new SecurityException("SecurityException in sign: " + exception);
      }

      Console.Out.WriteLine("Sent content " + content);
      try {
        phoneHandler_.face_.putData(data);
      } catch (Exception ex) {
        Console.Out.WriteLine("Echo: Exception in sending data " + ex);
      }
    }

    public void onRegisterFailed(Name prefix) {
      Console.Out.WriteLine("Register failed for prefix: " + prefix.toUri());
    }

    PhoneHandler phoneHandler_;
  }
}

class TestHtmlHandler {
  public static void Main() {
    PhoneHandler handler = new PhoneHandler();
    handler.start();
  }
}