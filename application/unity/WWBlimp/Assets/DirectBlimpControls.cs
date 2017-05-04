using UnityEngine;
using System.Collections;

public class DirectBlimpControls : MonoBehaviour {


	public float rotationSpeed = .1f;
	public float maxPitch = 30;
	public float maxRoll = 15;

	[Tooltip("Interpolation rate for velocity.")]
	[Range(0f, 1f)]
	public float acceleration = .01f;

	[Header ("Speed Limits")]
	public float maxVelocity = 1.0f;




	//[Header("Realtime Controls")]
	//[Range(0f, 1f)]
	//public float thrust = 0;




	[Header("Gyros")]
	[ReadOnly] public NDN_Gyro orientationGyro;
	[Tooltip("pitch/x is used for thrust, other values are ignored")]
	[ReadOnly] public NDN_Gyro throttleGyro;

	[ReadOnly] public Vector3 goalRotation = new Vector3(0,0,0);
	[Tooltip("Desired velocity as a percentage of maxVelocity")]
	[ReadOnly] public float goalVelocityPercent = 0.0f;

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
		Quaternion curRoation = transform.rotation;
		Quaternion goal = Quaternion.identity;

		goalRotation.x = map (orientationGyro.minScaleValues.x, orientationGyro.maxScaleValues.x, -maxPitch, maxPitch, orientationGyro.scaledGyroValues.x);
		goalRotation.z = map (orientationGyro.minScaleValues.z, orientationGyro.maxScaleValues.z, -maxRoll, maxRoll, orientationGyro.scaledGyroValues.z);

		goal.eulerAngles = goalRotation;


		transform.rotation = Quaternion.Slerp(curRoation, goal, rotationSpeed);

		if (throttleGyro != null) {
			goalVelocityPercent = throttleGyro.scaledGyroValues.x;
		}


		goalVelocityPercent = (goalVelocityPercent >= 1.0f) ? 1.0f : goalVelocityPercent;
		goalVelocityPercent = (goalVelocityPercent <= 0) ? 0 : goalVelocityPercent;

		Vector3 goalVelVec = transform.forward * goalVelocityPercent * maxVelocity;


		rb.velocity = Vector3.Lerp (rb.velocity, goalVelVec, acceleration);

	}

	float map(float fromMin, float fromMax, float toMin, float toMax, float fromVal) {
		float t = Mathf.InverseLerp (fromMin, fromMax, fromVal);
		return Mathf.Lerp (toMin, toMax, t);
	}
}
