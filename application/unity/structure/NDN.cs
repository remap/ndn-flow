using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using net.named_data.jndn;
using net.named_data.jndn.encoding;
using net.named_data.jndn.util;
using net.named_data.jndn.transport;
using net.named_data.jndn.security;

using ndn_iot.bootstrap;
using ndn_iot.consumer;

public class NDN : MonoBehaviour {
	public Face face_;
	public KeyChain keyChain_;
	public Name certificateName_;

	public Bootstrap bootstrap_;

	// Flow related functionalities: a gyro instance, an opt instance, and a phone instance
	public AppConsumerTimestamp gyroConsumer_;
	public GyroDataHandler gyroDataHandler_;

	public const string instanceName = "/home/flow-csharp";
	public const string hostName = "localhost";
	public const string gyroPrefix = "/home/flow1/gyro-sim1";

	// Use this for initialization
	void Start () {
		face_ = new Face(new TcpTransport(), new TcpTransport.ConnectionInfo(hostName));

		bootstrap_ = new Bootstrap(face_);
		keyChain_ = bootstrap_.setupDefaultIdentityAndRoot(new Name(instanceName), new Name());
		certificateName_ = bootstrap_.getDefaultCertificateName();

		// initialize the gyro instance, called when this starts, or when triggered by other components
		startGyroConsumer(gyroPrefix);
		Debug.Log(certificateName_.toUri());
	}

	// Update is called once per frame
	void Update () {
		face_.processEvents ();
	}
	
	void startGyroConsumer(string prefix) {
		gyroConsumer_ = new AppConsumerTimestamp(face_, keyChain_, false, -1);
		gyroDataHandler_ = new GyroDataHandler();

		gyroConsumer_.consume(new Name(prefix), gyroDataHandler_, gyroDataHandler_, gyroDataHandler_);
	}

	void startOptConsumer(string prefix) {
	}
}
