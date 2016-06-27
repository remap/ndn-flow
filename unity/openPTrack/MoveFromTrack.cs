using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveFromTrack : MonoBehaviour, OpenPTrackListener {


	//This class is intended as a simple example of using OpenPTrack data over NDN

	//Attach this behavoir to a Unity object and its position will mirror the first track 
	//that enters the OpenPTrack space 

	OpenPTrackConsumer openPTrack = null;


	string trackID = null;
	public Vector3 intialTrackPosition;
	public Vector3 initalObjectPosition;

	// Use this for initialization
	void Start () {
		openPTrack = OpenPTrackConsumer.getOpenPTrackConsumer ();
		if (openPTrack == null) {
			Debug.LogError ("MoveFromTrack created before OpenPTrackConsumer.  Fix script execution order.  You can do this via the Unity Menu \'Edit > Project Settings > Script Execution Order\'.");
		} 
		openPTrack.addListener (this);

		initalObjectPosition = new Vector3 (transform.position.x, transform.position.y, transform.position.z);

	}

	public void trackEnter(Track track) {
		//start following the first track you see
		Debug.Log("got enter " + track.id);
		if (trackID == null) {
			trackID = track.id;
			Vector3 pos = track.getPosition ();
			intialTrackPosition = new Vector3 (pos.x, pos.y, pos.z);
		}
	}

	public void trackExit(Track track) {
		if (track.id.Equals (trackID)) {
			trackID = null;
		}
	}


	// Update is called once per frame
	void Update () {
		if (trackID != null) {		
			Track track = openPTrack.getTracks()[trackID];
			Debug.Log (track.getPosition ());
			transform.position = initalObjectPosition + (track.getPosition() - intialTrackPosition);
		}

	}
}
