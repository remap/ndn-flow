using UnityEngine;
using System.Collections;

public class BlimpEngine : MonoBehaviour {


	[Header ("Speed Limits")]
	public float maxVelocity = 1.0f;
	[Tooltip("(Pitch, Yaw, Roll) in radians/sec")]
	public Vector3 maxAngularVelocity = new Vector3(1f,1f,1f);

	[Header ("Force/Acceleration Limits")]
	public float maxThrust = 1.0f;
	[Tooltip("(Pitch, Yaw, Roll)")]
	public Vector3 maxTorque = new Vector3(1f,1f,1f);
	[Tooltip("Engine torque will cut out beyond this pitch")]
	public float maxPitch = 30;
	[Tooltip("Engine torque will cut out beyond this roll")]
	public float maxRoll = 15;
	[Tooltip("If over rotated, decelerate in that direction. (smaller number is more rapid deceleration")]
	[Range(0f, 1f)]
	public float rotationDecelerationRate = .25f;


	[Header("Realtime Controls")]
	[Range(0f, 1f)]
	public float thrust = 0;
	[Range(-1f, 1f)]
	public float pitchTorque = 0;
	[Range(-1f, 1f)]
	public float yawTorque = 0;
	[Range(-1f, 1f)]
	public float rollTorque = 0;

	[Header("Gyros")]
	[ReadOnly] public NDN_Gyro orientationGyro;
	[Tooltip("pitch/x is used for thrust, other values are ignored")]
	[ReadOnly] public NDN_Gyro throttleGyro;

	Rigidbody rb;


	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody>();	

		orientationGyro = null;
		throttleGyro = null;

		NDN_Gyro[] gyros = GetComponents<NDN_Gyro> ();
		foreach (NDN_Gyro gyro in gyros) {
			if (gyro.gyroType == NDN_Gyro.GyroTypes.OrientationGyros) {
				orientationGyro = gyro;
			} else if (gyro.gyroType == NDN_Gyro.GyroTypes.ThrottleGyros) {
				throttleGyro = gyro;
			}

		}
	}
	
	// Update is called once per frame
	void Update () {
		//still need to enforce range despite limits in UI since values might be set programmatically  (I think)


		if (orientationGyro != null) {
			pitchTorque = orientationGyro.scaledGyroValues.x;
			yawTorque = orientationGyro.scaledGyroValues.y;
			rollTorque = orientationGyro.scaledGyroValues.z;
		}
		if (throttleGyro != null) {
			thrust = throttleGyro.scaledGyroValues.x;
		}
		thrust = (thrust >= 1.0f) ? 1.0f : thrust;
		thrust = (thrust <= 0) ? 0 : thrust;

		pitchTorque = (pitchTorque >= 1.0f) ? 1.0f : pitchTorque; 
		yawTorque = (yawTorque >= 1.0f) ? 1.0f : yawTorque; 
		rollTorque = (rollTorque >= 1.0f) ? 1.0f : rollTorque; 

		thrust = (thrust <= 0f) ? 0f : thrust; 
		pitchTorque = (pitchTorque <= -1f) ? -1f : pitchTorque; 
		yawTorque = (yawTorque <= -1f) ? -1f : yawTorque; 
		rollTorque = (rollTorque <= -1f) ? -1f : rollTorque; 
	
	



		rb.AddForce(transform.forward * thrust * maxThrust);

		//limit velocity
		if (rb.velocity.magnitude > maxVelocity) {
			rb.velocity = rb.velocity.normalized * maxVelocity;
		}


		Vector3 angularVelocityCaps = maxAngularVelocity;

//		float calculatedPitch = Vector3.Angle (transform.forward, Vector3.ProjectOnPlane (transform.forward, Vector3.up));
		// don't need to calclatePitch Unityies is the same

		float appliedPitchTorque = pitchTorque * maxTorque.x;
		float siftedPitch = shiftAngle (transform.eulerAngles.x);
		if (limitRotation (siftedPitch, maxPitch, appliedPitchTorque)) {
			appliedPitchTorque = 0;
		}
		if (limitRotation (siftedPitch, maxPitch, rb.angularVelocity.x)) {
			angularVelocityCaps.x *= rotationDecelerationRate;
		}

		// there are multple wasy to computer eulerAngles.   
		// this is working better for limiting roll

		// calc pitch and roll by self
		// stay in box with abs torque

		float calculatedRoll = calculateRoll ();
		float appliedRollTorque = rollTorque * maxTorque.z;
		if (limitRotation (calculatedRoll, maxRoll, appliedRollTorque)) {
			appliedRollTorque = 0;
		}
		if (limitRotation (calculatedRoll, maxRoll, rb.angularVelocity.z)) {
			angularVelocityCaps.z *= rotationDecelerationRate;
		}


		rb.AddRelativeTorque (appliedPitchTorque, yawTorque * maxTorque.y, appliedRollTorque);
		rb.angularVelocity =clampAbsValue(rb.angularVelocity, angularVelocityCaps);


	}

	float calculateRoll() {
		float calculatedRoll = Vector3.Angle (transform.right, Vector3.ProjectOnPlane (transform.right, Vector3.up));
		if (Vector3.Angle (transform.right, Vector3.up) > 90) {
			return calculatedRoll;
		} else {
			return -calculatedRoll;
		}
	}

	float shiftAngle(float angle) {
		// unity angles are between 0 and 360
		// shift to between -180 and 180
		if (angle > 180) {
			return angle - 360;
		}
		return angle;
		
	}
	bool limitRotation(float angle,  float rotationLimit, float unlimitedValue) {
		//assume angle between -180 and 180
	

		if ((angle >= rotationLimit) && (unlimitedValue >= 0)) {// cut engine if past max pitch and trying to increase pitch
			return true;
		} else if ((angle <= -rotationLimit) && (unlimitedValue <= 0)) {
			return true;
		}	


		return false;
	}

	Vector3 clampAbsValue(Vector3 vec, Vector3 lim) {
		if (vec.x >= lim.x) {
			vec.x = lim.x;
		} else if (vec.x <= -lim.x) {
			vec.x = -lim.x;
		}
		if (vec.y >= lim.y) {
			vec.y = lim.y;
		} else if (vec.x <= -lim.y) {
			vec.y = -lim.y;
		}
		if (vec.z >= lim.z) {
			vec.z = lim.z;
		} else if (vec.z <= -lim.z) {
			vec.z = -lim.z;
		}
		// Vectors seem to be pass by value not reference so I need to return
		// (or add a ref to the parameter)
		return vec;
	}
}
