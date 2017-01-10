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

class TrackIdMatcher {
  public Face face_;
  public KeyChain keyChain_;
  public Name certificateName_;

  public Bootstrap bootstrap_;
  public Name prefix_;

  public const string hostName = "localhost";
  public const string instanceName = "/home/flow-csharp";
  public const string commandVerb = "match";

  public void start() {
    // generic face setup
    face_ = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo(hostName));

    bootstrap_ = new Bootstrap(face_);
    keyChain_ = bootstrap_.setupDefaultIdentityAndRoot(new Name(instanceName), new Name());
    certificateName_ = bootstrap_.getDefaultCertificateName();

    // class-specific start
    InterestHandler ih = new InterestHandler(this);
    prefix_ = new Name(instanceName).append(commandVerb);
    face_.registerPrefix(prefix_, ih, ih);

    // update event
    while (true) {
      face_.processEvents();
      System.Threading.Thread.Sleep(5);
    }
  }

  class InterestHandler : OnInterestCallback, OnRegisterFailed {
    public InterestHandler(TrackIdMatcher matcher) {
      matcher_ = matcher;
    }

    public void onInterest(Name prefix, Interest interest, Face face, long interestFilterId,
      InterestFilter filter)
    {
      // TODO: command interest verification
      string matchId = interest.getName().get(matcher_.prefix_.size()).toEscapedString();
      // TODO: merge with class Track (Person) and Tracks to tell if there's tracks that can be matched
      //   for now, return match success

      var data = new Data(interest.getName());
      var content = "{\"status\": \"200\", \"trackId\": \"3\", \"mobileId\":\"" + matchId + "\"}";
      data.setContent(new Blob(content));
      data.getMetaInfo().setFreshnessPeriod(2000);

      try {
        matcher_.keyChain_.sign(data, matcher_.certificateName_);      
      } catch (SecurityException exception) {
        // Don't expect this to happen.
        throw new SecurityException("SecurityException in sign: " + exception);
      }

      Console.Out.WriteLine("Sent content " + content);
      try {
        matcher_.face_.putData(data);
      } catch (Exception ex) {
        Console.Out.WriteLine("Echo: Exception in sending data " + ex);
      }
    }

    public void onRegisterFailed(Name prefix) {
      Console.Out.WriteLine("Register failed for prefix: " + prefix.toUri());
    }

    TrackIdMatcher matcher_;
  }
}

class TestTrackIdMatcher {
  public static void Main() {
    TrackIdMatcher idMatcher = new TrackIdMatcher();
    idMatcher.start();
  }
}