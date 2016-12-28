using UnityEngine;
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

class FetchInterestHandler : OnInterestCallback, OnRegisterFailed {
	public FetchInterestHandler() {
	}

	public void onInterest(Name prefix, Interest interest, Face face, long interestFilterId,
		InterestFilter filter)
	{
		Console.Out.WriteLine("Data not found: " + interest.getName().toUri());
	}

	public void onRegisterFailed(Name prefix) {
		Console.Out.WriteLine("Register failed for prefix: " + prefix.toUri());
	}

}