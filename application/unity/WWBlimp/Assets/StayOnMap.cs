using UnityEngine;
using System.Collections;

public class StayOnMap : MonoBehaviour {

	public float maxHeight = 400;
	public float minHeight = 10;

	[Tooltip("Maximum force away from noGoZone")]
	public float maxRepultionForce = 1;
	[Tooltip("The greater the leverage the faster the blimp will turn away (and the less it will translate away)")]
	public float repultionLeverage = 1;


	[Tooltip("Choose a no Go Zone Width such that the camera can't see of the edge of the map at max height")]
	public float noGoZoneWidth = 20;
	[Tooltip("Float will start turning when it gets withing buffer width of the Go Zone Width")]
	public float bufferWidth = 30;
	public float verticalBufferWidth = 10; 

	Rigidbody rb;
	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody> ();
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 terrainSize = Terrain.activeTerrain.terrainData.size;
		Vector3 terrainPosition =  Terrain.activeTerrain.GetPosition ();

		Vector3 noGoMin = terrainPosition + new Vector3 (noGoZoneWidth, minHeight, noGoZoneWidth);
		Vector3 noGoMax = terrainPosition + new Vector3 (terrainSize.x - noGoZoneWidth, maxHeight, terrainSize.z - noGoZoneWidth);

		Vector3 bufferWidthVec = new Vector3 (bufferWidth, verticalBufferWidth, bufferWidth);
		Vector3 buffMin = noGoMin + bufferWidthVec;
		Vector3 buffMax = noGoMax - bufferWidthVec;


		//move out of no go zone if needed (shouldn't happen)
		Vector3 newPosition = Vector3.Max (transform.position, noGoMin);
		newPosition = Vector3.Min (newPosition, noGoMax);
		rb.position = newPosition;

		Vector3 replutionPoint = transform.position + transform.forward * repultionLeverage;
		if (newPosition.x < buffMin.x) {
			float dot = Vector3.Dot (transform.forward, -Vector3.left);
			if (dot > 0) {
//				float buffPercentage = dot * (buffMin.x - newPosition.x) / bufferWidth;
				float buffPercentage =  (buffMin.x - newPosition.x) / bufferWidth;
				rb.AddForceAtPosition (Vector3.left * -buffPercentage, replutionPoint);
			}
		} else if (newPosition.x > buffMax.x) {
			float dot = Vector3.Dot (transform.forward, Vector3.left);
			if (dot > 0) {
//				float buffPercentage = dot *  (newPosition.x - buffMax.x) / bufferWidth;
				float buffPercentage =   (newPosition.x - buffMax.x) / bufferWidth;
				rb.AddForceAtPosition (Vector3.left * buffPercentage, replutionPoint);
			}
		}

		if (newPosition.y < buffMin.y) {
			float dot = Vector3.Dot (transform.forward, -Vector3.up);
			if (dot > 0) { // if headed down
				float buffPercentage =  (buffMin.y - newPosition.y) / bufferWidth;
				rb.AddForceAtPosition (Vector3.up * buffPercentage, replutionPoint);
			}
		} else if (newPosition.y > buffMax.y) {
			float dot = Vector3.Dot (transform.forward, Vector3.up);
			if(dot > 0) { // if headed up
				float buffPercentage =  (newPosition.y - buffMax.y) / bufferWidth;
				rb.AddForceAtPosition (Vector3.up * -buffPercentage, replutionPoint);
			}
		}

		if (newPosition.z < buffMin.z) {
			float dot = Vector3.Dot (transform.forward, -Vector3.forward);
			if (dot > 0) { // if headed in to wall
				float buffPercentage = (buffMin.z - newPosition.z) / bufferWidth;
				rb.AddForceAtPosition (Vector3.forward * buffPercentage, replutionPoint);
			}
		} else if (newPosition.z > buffMax.z) {
			float dot = Vector3.Dot (transform.forward, Vector3.forward); 
			if (dot > 0) { // if headed in to wall
				float buffPercentage =  (newPosition.z - buffMax.z) / bufferWidth;
				rb.AddForceAtPosition (Vector3.forward * -buffPercentage, replutionPoint);
			}
		}

	}
}
