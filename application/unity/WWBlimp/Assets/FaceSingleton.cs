﻿using UnityEngine;
using System.Collections;

using net.named_data.jndn;
using net.named_data.jndn.encoding;
using net.named_data.jndn.transport;
using net.named_data.jndn.util;
using net.named_data.jndn.security;
using ndn_iot.bootstrap;


public class FaceSingleton : MonoBehaviour {
	public  string hostName = "localhost";
	public  string space =  "/home/flow-csharp";


	private static Face theFace = null;
	private static FaceSingleton theFaceOwner = null;
	private static Bootstrap theBootstrap = null;
	private static KeyChain theKeyChain;
	private static Name theCertificateName;

	public static Face getFace(FaceSingleton owner = null) {
		// not thread safe ok for unity
		if (theFace == null) {
			if ((owner != null) && (theFaceOwner == null)) {
				theFaceOwner = owner;
				theFace = new Face (theFaceOwner.hostName);
				// the fisrt owner to claim the face gets it
				theBootstrap = new Bootstrap (theFace);
				theKeyChain = theBootstrap.setupDefaultIdentityAndRoot (new Name (theFaceOwner.space), new Name ());
				theCertificateName = theBootstrap.getDefaultCertificateName ();

			} else {
				Debug.Log ("Attempt to get face before setup by FaceSingleton owner");
			}
		}
		return theFace;
		
	}

	public static KeyChain getKeychain() {
		if (theFace == null) {
			getFace ();
		}
		return theKeyChain;
	}

	public static Bootstrap getBoostrap() {
		if (theFace == null) {
			getFace ();
		}
		return theBootstrap;
	}
	public static Name getCertificateName() {
		if (theFace == null) {
			getFace ();
		}
		return theCertificateName;
	}


	//this should happen beofe all the calls to start

	public void Awake() {
		print ("awake");
		getFace (this);
	}
	// Use this for initialization
	void Start () {
		print ("start");
	}
	
	// Update is called once per frame
	void Update () {
		if ((theFace != null) && (theFaceOwner == this)) {
			// only call process if this instance is the owner
			theFace.processEvents ();
		}
	}


	void OnApplicationQuit() {
		if ((theFace != null) && (theFaceOwner == this)) {
			// only call process if this instance is the owner
			theFace.shutdown ();
			theFace = null;
			theFaceOwner = null;
		}
	}


}
