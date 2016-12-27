using UnityEngine;
using System.Collections;
using System.Collections.Generic;


// does openptrack publish dims
// edges
//

public class TrackMoveDebug : MonoBehaviour
{
	public string trackID;
	TrackToTerrain trackToTerrain;
	TextMesh textMesh;
	Track track;
	public Vector3 trackPosition;
	public Vector3 unityPosition;
	// Use this for initialization
	void Start ()
	{
	
	}

	public void setup (Track track, TrackToTerrain trackToTerrain) {
		this.track = track;
		trackID = track.id;
		textMesh = GetComponent<TextMesh> ();
		textMesh.text = track.id;
		this.trackToTerrain = trackToTerrain;
	}
	
	// Update is called once per frame
	void Update ()
	{
		switch (track.getState ()) {
		case Track.State.UNINITIALIZED:
			return;
		case Track.State.PENDING:
			textMesh.color = new Color (1, 0, 0);
			break;
		case Track.State.ACTIVE:
			textMesh.color = new Color (0, 1, 0);
			break;
		case Track.State.DEAD:
			textMesh.color = new Color (0, 0, 1);
			Destroy (gameObject);
			break;
		}
			
		trackPosition = track.getPosition ();
		Vector3 pos = trackToTerrain.convertTrackLocToUnityDims (trackPosition);
		pos.y = Terrain.activeTerrain.SampleHeight (transform.position) + 20	;
		unityPosition = pos;
		transform.position = pos;
	}
	
}

