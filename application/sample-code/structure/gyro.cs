using UnityEngine;
using System;

using net.named_data.jndn;
using net.named_data.jndn.util;
using net.named_data.jndn.security;

using SimpleJSON;

public class GyroDataHandler : OnVerified, OnVerifyFailed, OnTimeout {
	public enum GyroTypes {
		OrientationGyros,
		ThrottleGyros,
	}

	public GyroTypes gyroType = GyroTypes.OrientationGyros;

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

	public void onVerified(Data data) {
		//print ("Data received: " + data.getName ().toUri ());

		var node = JSON.Parse (data.getContent ().toString ());
		gyroValues = new Vector3 (node ["p"].AsFloat, node ["y"].AsFloat, node ["r"].AsFloat);

		Vector3 scaledValues = Vector3.Min (gyroValues, maxSensorValues);
		scaledValues = Vector3.Max (scaledValues, minSensorValues);

		scaledValues = scaledValues - minSensorValues;

		Vector3 diff = maxSensorValues - minSensorValues;
		//can't seem to do this with vector methods
		scaledValues.x /= diff.x;
		scaledValues.y /= diff.y;
		scaledValues.z /= diff.z;

		diff = maxScaleValues - minScaleValues;
		scaledValues = Vector3.Scale(scaledValues, diff);
		scaledValues = scaledValues + minScaleValues;

		scaledGyroValues = scaledValues;
	}

	public void onVerifyFailed(Data data) {
		Debug.Log("Data verify failed: " + data.getName().toUri());
	}

	public void onTimeout(Interest interest) {
		Debug.Log("Interest times out: " + interest.getName().toUri());
	}
}