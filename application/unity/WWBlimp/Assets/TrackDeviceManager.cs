using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;


public class TrackDeviceManager : MonoBehaviour {

	ImageDrop imageDropScript;



	[Tooltip("relative paths start inside project folder")]
	public string mediaFolderPath = "../../wwFlowMedia/";

	[Header ("Track/Device Matching")]
	[Tooltip("x location (in real world coords) that mobiles are associated with tracks")]
	public float matchLocationX = 0;
	[Tooltip("z location (in real world coords) that mobiles are associated with tracks")]
	public float matchLocationZ = 0;
	[Tooltip("radius (openptrack units - meters?) from center of match to be considered a match")]
	public float matchRadius = .5f;
	[Tooltip("distance below blimp cloth should be dropped")]
	public float dropOffset = 1f;

	// not sure why these are showing up in inspector (as read only)
	OpenPTrackProvider openPTrack ;
	string[] mediaFiles;
	string mediaDirectory;

	Dictionary<string , string > devTrackDict = new Dictionary<string , string >();


	Rigidbody theBlimpRigidbody;

	// Use this for initialization
	void Start () {
		openPTrack = GetComponent<OpenPTrackProvider> ();
		imageDropScript = GetComponent<ImageDrop> ();

		GameObject theBlimp = GameObject.Find ("Blimp");
		theBlimpRigidbody = theBlimp.GetComponent<Rigidbody> ();

		WebComm.addRFC ("match", match);
		WebComm.addRFC ("drop", drop);

		DirectoryInfo dir = new DirectoryInfo(mediaFolderPath);
		FileInfo[] fileInfo = dir.GetFiles("*.*");
		mediaDirectory = dir.FullName;
		mediaFiles = new string[fileInfo.Length];
		for (int i = 0; i < mediaFiles.Length; i++) {
			mediaFiles [i] = fileInfo[i].Name;
		}

	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown ("space"))
			drop ("-1", new string[]{"drop", "RS112_1957_CathedralHigh_TypingClass.jpg"});
	
	}


	//changed to drop from blimp
	public void drop(string devID, string[] fNameAndArgs) {	
		Vector3 vel = theBlimpRigidbody.velocity;
		Vector3 pos = theBlimpRigidbody.transform.position;

		imageDropScript.Drop(new Vector3(pos.x, pos.y - dropOffset, pos.z), vel, mediaDirectory+fNameAndArgs[1]);
		/*
		try {
			string trackID = devTrackDict[devID];
			try {
				Track track = openPTrack.getTracks()[trackID];
				print(track.getPosition());
				imageDropScript.Drop(track.unityPosition, mediaDirectory+fNameAndArgs[1]);
			} catch {
				Debug.LogError ("TrackDeviceManager.drop unable to get track for id " + trackID + " (for device "  +devID+")");
			}
		} catch {
			Debug.LogError ("TrackDeviceManager.drop unable to get track id for device " + devID);
		}
		*/


	}
	
	// matches to closests to center of match area (if one exists)
	// TODO: EGM would it be better to give an error and not match if two are in the radius? 
	public void match(string devID, string[] fNameAndArgs) {		
		print ("matching");
		Dictionary<string, Track> tracks = openPTrack.getTracks ();
		float bestDistSqr = matchRadius*matchRadius;
		Track bestTrack = null;
		foreach(Track track in tracks.Values)
		{
			Vector3 posLoc = track.getPosition ();
			float d = posLoc.x - matchLocationX;
			float distSqr = d * d;
			d = posLoc.z - matchLocationZ;
			distSqr += d * d;
			if (distSqr <= bestDistSqr) {
				bestDistSqr = distSqr;
				bestTrack = track;
			} else {
				print("dist to candidate: " + distSqr.ToString() + " " + track.id + "(x: " + posLoc.x.ToString() + ", z:" + posLoc.z.ToString() + ")");
			}
		}
		if (bestTrack == null) {
			print ("no match  " + devID);
			string html = "<p = class=\"errorMsg\"><b>Please go to that special spot on the floor and click</b>: " +
				WebComm.expressionLink ("Associate me with a track", "match") + "</p>";
			WebComm.publishHtml (devID, html);
		} else {
			print ("got a match");
			devTrackDict [devID] = bestTrack.id;
			string html = "<p> Select an image to drop from your location (track ID: " + bestTrack.id + "):\n  <ul>";
			foreach(string fname in mediaFiles) {
				html += "    <li>";
				// can't use full path because we hit a length limit
				// work around for now
				// better solution add html append in addition to replace
				// and automatically cut up long html strings 
				html += WebComm.expressionLink (fname, "drop," + fname);
				html += "</li>\n";				
			}
			html+="  </ul>\n</p>\n";
			try {
				WebComm.publishHtml (devID, html);
			} catch (Exception e) {
				Debug.LogError ("exception publishign " + html);
				Debug.LogError (e);


			}
			//TODO: come up with better copy EGM

		}
	}

}
