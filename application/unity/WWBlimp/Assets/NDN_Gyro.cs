using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// TODO: to be finished up with consumer functions as in other languages
using System.Runtime.InteropServices;

using net.named_data.jndn.security.policy;    
using net.named_data.jndn;
using net.named_data.jndn.encoding;
using net.named_data.jndn.util;
using net.named_data.jndn.security;
using net.named_data.jndn.security.identity;
using net.named_data.jndn.security.certificate;
using net.named_data.jndn.encoding.tlv;

using net.named_data.jndn.transport;

using ndn_iot.bootstrap;
using ndn_iot.consumer;


using SimpleJSON;

public class NDN_Gyro : MonoBehaviour {
	

	public enum GyroTypes {
		OrientationGyros,
		ThrottleGyros,
	}

	public GyroTypes gyroType = GyroTypes.OrientationGyros;


	//for some reason unity is caching the old property value
	//changing the varaible name to force update! (now I'm worried about other parts of this project)
	public string gyroPrefix = "/home/flow/gyros/gyro1/";


	[Header("Sensor Scaling")]
	[Tooltip("pitch, yaw, roll")]
	public  Vector3 minSensorValues = new Vector3 (-1, -1, -1);
	[Tooltip("pitch, yaw, roll")]
	public  Vector3 maxSensorValues = new Vector3 (1, 1, 1);
	[Tooltip("pitch, yaw, roll")]
	public  Vector3 minScaleValues = new Vector3 (-1, -1, -1);
	[Tooltip("pitch, yaw, roll")]
	public  Vector3 maxScaleValues = new Vector3 (1, 1, 1);



	[Header("Sensor Values (readonly)")]
	[ReadOnly] public Vector3 gyroValues = new Vector3 ();
	[ReadOnly] public Vector3 scaledGyroValues = new Vector3 ();


//	Face face;
 	

	class ConsumerDataHandler : OnVerified, OnVerifyFailed, OnDataValidationFailed, OnTimeout {
		NDN_Gyro gyro;

		public ConsumerDataHandler(NDN_Gyro gyro) {
			this.gyro = gyro;
		}
		public void onVerified(Data data) {
			//print ("Data received: " + data.getName ().toUri ());

			JSONNode node = JSON.Parse (data.getContent ().toString ());
			gyro.gyroValues = new Vector3 (node ["p"].AsFloat, node ["y"].AsFloat, node ["r"].AsFloat);



			Vector3 scaledValues = Vector3.Min (gyro.gyroValues, gyro.maxSensorValues);
			scaledValues = Vector3.Max (scaledValues, gyro.minSensorValues);

			scaledValues = scaledValues - gyro.minSensorValues;

			Vector3 diff = gyro.maxSensorValues - gyro.minSensorValues;
			//can't seem to do this with vector methods
			scaledValues.x /= diff.x;
			scaledValues.y /= diff.y;
			scaledValues.z /= diff.z;

			diff = gyro.maxScaleValues - gyro.minScaleValues;
			scaledValues = Vector3.Scale(scaledValues, diff);
			scaledValues = scaledValues + gyro.minScaleValues;

			gyro.scaledGyroValues = scaledValues;




		}

		public void onVerifyFailed(Data data) {
			print("Data verify failed: " + data.getName().toUri());

		}

		public void onTimeout(Interest interest) {
			//print("Interest times out: " + interest.getName().toUri());
		}

		public void onDataValidationFailed (Data data, string reason) {
			print("Data verify failed: " + data.getName().toUri());
			print (reason);
		}
	}
		
	// Use this for initialization
	void Start () {
		gyroValues =  new Vector3 ();
		scaledGyroValues =  new Vector3 ();

		// main is static so cannot refer to non-static members here, if want to make onRequestSuccess and onRequestFailed non-static
		AppConsumerTimestamp consumer = new AppConsumerTimestamp(FaceSingleton.getFace(), FaceSingleton.getKeychain(), false);
		ConsumerDataHandler cdh = new ConsumerDataHandler(this);

		// todo: fill in simulator prefix
		consumer.consume(new Name(gyroPrefix), cdh, cdh, cdh);
	}
	
	// Update is called once per frame
	void Update () {

	}
}
