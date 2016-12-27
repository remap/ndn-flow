using UnityEngine;
using System.Collections;



public class TrackDebugProducer : MonoBehaviour, OpenPTrackListener{
	public bool isOn = true;
	public GameObject trackDebugPrefab;
	TrackToTerrain trackToTerrain;
	// Use this for initialization
	void Start () {
		if (isOn) {
			OpenPTrackProvider opp = GetComponent<OpenPTrackProvider> ();
			opp.addListener (this);
			trackToTerrain = GetComponent<TrackToTerrain> ();
		}	
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void trackEnter(Track track) {
		Debug.Log("Adding Track to world " + track.id);
		GameObject trackDebug = (GameObject) Instantiate(trackDebugPrefab);
		TrackMoveDebug tmd = trackDebug.GetComponent<TrackMoveDebug> ();
		tmd.setup (track, trackToTerrain);

	}

	//not need Tracks see when they are dead
	public void trackExit(Track track) {
	}


}
