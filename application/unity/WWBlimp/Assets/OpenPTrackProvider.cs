using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using net.named_data.jndn;
using net.named_data.jndn.encoding;
using net.named_data.jndn.transport;
using net.named_data.jndn.util;

using SimpleJSON;


public class OpenPTrackProvider : MonoBehaviour, TrackProvider  {

	//Public Serializable properties show up in Unity inspector
	public NDNConfig config = new NDNConfig();
	public ProducerNameComponents producerNameComponents = new ProducerNameComponents();


	//TODO: remove this
	//public static Track DUMMY_TRACK = null;

	[System.Serializable]
	public class NDNConfig  {
//		public string hostName = "localhost";

		// Namespace configuration
		public string rootPrefix = "/ndn/edu/ucla/remap/opt";
		public string spaceName = "node0";

		// Interest configuration
		public int initialReexpressInterval	= 1; // in secs in unity
		public int defaultInitialLifetime	= 2000;
		public int defaultTrackInterestLifetime = 250;
		public int defaultHintLifetime = 500;
		public int trackTimeoutsTillDead = 10;
		public int trackUpdatesTillActive = 4;

		// The threshold for number of timeouts received in a row to decide not to fetch certain
		// track anymore
		// EGM - this is lame - shouldn't we know its gone from the trackhints?

	}

	public Vector3 minTrackDims = new Vector3(-2.5f, -1.75f, 0f);
	public Vector3 maxTrackDims = new Vector3 (4.5f, 1.75f, 3f);
	public bool flipZY = true;


	[System.Serializable]
	public class ProducerNameComponents {
		public string tracks =  "tracks";
		public string trackHint = "track_hint";
		public int trackIdOffset	= -2;
	}



	// this class is a singleton
	//	private static OpenPTrackConsumer theConsumer = null;

	//	public static OpenPTrackConsumer getOpenPTrackConsumer() {
	//		// this assumes the consumer was already created 
	//		// so make sure your scrips are execuated in the correct order
	//		return theConsumer;
	//	}



	//This class uses Java style listeners to get notified about
	//entering and exiting tracks
	// (it might be better to use Unity style messages. 
	// If I did this I probably wouldn't need  to make this class a singleton and script execuation order wouldn't matter- TODO EGM)
	List<OpenPTrackListener> openPTrackListeners = new List<OpenPTrackListener>();

	Name prefix;

	Name.Component startTimeComponent;

	Dictionary<string, Track> tracks = new Dictionary<string, Track> ();


	public Name.Component  getStartTimeComponent() {
		return startTimeComponent;
	}

	public Name.Component  setStartTimeComponent(Name.Component comp) {
		return startTimeComponent = comp;
	}

	public void Update() {
	

	}

	public void addListener(OpenPTrackListener listener) {
		openPTrackListeners.Add(listener);
	}

	public void removeListener(OpenPTrackListener listener) {
		openPTrackListeners.Remove (listener);
	}



	// start is called by Unity
	public void Start() {

		this.prefix = new Name (config.rootPrefix).append(config.spaceName);
		startTimeComponent = new Name.Component ();
		expressInitialInterest ();
		Track.updatesTillActive = config.trackUpdatesTillActive;
		Track.timeoutsTillDead = config.trackTimeoutsTillDead;

	}

	void expressInitialInterest() {
		//Debug.Log ("expressInitialInterest: start");
		Interest initialInterest = new Interest(this.prefix);
		initialInterest.setMustBeFresh(true);
		initialInterest.setInterestLifetimeMilliseconds(config.defaultInitialLifetime);
		// for initial interest, the rightMostChild is preferred
		initialInterest.setChildSelector(1);

		InitialDataHandler handler = new InitialDataHandler (this);

//		Debug.Log ("expressInitialInterest: " +  initialInterest.toUri());
		FaceSingleton.getFace().expressInterest(initialInterest, handler, handler);
	}

	public Vector3 getMinDims () {
		return minTrackDims;
	}
	public Vector3 getMaxDims () {
		return maxTrackDims;
	}


	public Dictionary<string, Track> getTracks() {
		return tracks;
	}


	// INITIAL HINT
	public class InitialDataHandler : OnData, OnTimeout {
		OpenPTrackProvider providerOuterInstance;
		public InitialDataHandler(OpenPTrackProvider providerOuterInstance) {
			this.providerOuterInstance = providerOuterInstance;
		}


		public void onData (Interest interest, Data data) {
		//			Debug.Log ("onData: " +  interest.toUri());

			Name dataName = data.getName ();
				Debug.Log ("NDN: Initial data received : " + dataName.toUri ());

			if (dataName.size () > providerOuterInstance.prefix.size ()+1) {
				providerOuterInstance.setStartTimeComponent(dataName.get (providerOuterInstance.prefix.size ()));
				//fetchTrackHint 
				providerOuterInstance.expressHintInterest();
			} else {
				Debug.LogError ("NDN: Initial interest received unexpected data : " + dataName.toUri ());
			}
		}
		public void onTimeout (Interest interest) {
//			Debug.Log ("NDN: Initial interest timed out: " + interest.getName ().toUri ());

			providerOuterInstance.reexpressInitialInterest();


		}

	}


	void reexpressInitialInterest() {
		Invoke("expressInitialInterest", config.initialReexpressInterval);
	}


	public void expressHintInterest(Name.Component excludeComp = null) {

		Name hintName = new Name(prefix);
		hintName.append(this.getStartTimeComponent()).append (producerNameComponents.trackHint);
		Interest hintInterest = new Interest (hintName);



		if (excludeComp != null) {
			Exclude exclude = new Exclude ();
			exclude.appendAny ();
	//		Debug.Log ("   excluding hint num " + excludeComp.toNumber ());
			exclude.appendComponent (excludeComp);
			hintInterest.setExclude (exclude);
		}

		hintInterest.setMustBeFresh (true);
		hintInterest.setInterestLifetimeMilliseconds (config.defaultHintLifetime);
		hintInterest.setChildSelector (1);
		HintHandler handler = new HintHandler (this);
		FaceSingleton.getFace().expressInterest (hintInterest, handler, handler); 
	}

	//fetchTrack
	public void expressInterestForTrack (string trackID, Name.Component excludeComp = null) {
		Name trackName = new Name (prefix);
		trackName.append (startTimeComponent).append (producerNameComponents.tracks).append (trackID);//.append ("0");
		Interest trackInterest = new Interest (trackName);

		if (excludeComp != null) {
			Exclude exclude = new Exclude ();
			exclude.appendAny ();
			exclude.appendComponent (excludeComp);
			trackInterest.setExclude (exclude);
		}


		trackInterest.setMustBeFresh (true);
		trackInterest.setChildSelector (1);
		TrackHandler handler = new TrackHandler (this);
		FaceSingleton.getFace().expressInterest (trackInterest, handler, handler);
	}

	public void createNewTrackAndExpressInterest(string trackID) {
		Track t = new Track (trackID);
		tracks.Add (trackID, t);
//		Debug.Log ("    ADDED track " + trackID);
		foreach (OpenPTrackListener listener in openPTrackListeners) {
			listener.trackEnter (t);
		}
		expressInterestForTrack (trackID);


	}



	// HINT DATA
	// Expected data name: [root]/opt/[node_num]/[start_timestamp]/track_hint/[num]
	public class HintHandler : OnData , OnTimeout {

		OpenPTrackProvider providerOuterInstance;
		public HintHandler(OpenPTrackProvider providerOuterInstance) {
			this.providerOuterInstance = providerOuterInstance;
		}



		public void onData (Interest interest, Data data) {
		//	Debug.Log ("NDN: HintHandler data received");

	
			JSONNode parsedHint = JSON.Parse (data.getContent ().toString ());

			foreach (JSONNode track in parsedHint["tracks"].AsArray) {
				string trackID = track ["id"];
			//	Debug.Log ("   " + trackID);
				// The consumer ignores the sequence number field in the hint for now;
				// As the consumer assumes it's getting the latest via outstanding interest.

				if (providerOuterInstance.tracks.ContainsKey (trackID)) {
					providerOuterInstance.tracks [trackID].touchByHint ();
				} else {
	//				Debug.Log ("HintHandler "+  curHintHandlerNumber + "   creating " + trackID);
					providerOuterInstance.createNewTrackAndExpressInterest (trackID);
				}

			}

//			DUMMY_TRACK.touchByHint ();

			List<Track> toCull = new List<Track>(providerOuterInstance.tracks.Count);
			foreach (Track t in providerOuterInstance.tracks.Values) {
				if (t.shouldCull()) {
					toCull.Add (t);
				}
			}

			foreach(Track t in toCull) {
				providerOuterInstance.tracks.Remove (t.id);
				//Debug.Log ("HintHandler" + curHintHandlerNumber + "   REMOVED track " + t.id);
				if (t.getState () == Track.State.ACTIVE) {
					foreach (OpenPTrackListener listener in providerOuterInstance.openPTrackListeners) {
						listener.trackExit (t);				
					}
				}
				t.cull ();
			}
			providerOuterInstance.expressHintInterest (data.getName().get(-1));
		}

		public void onTimeout (Interest interest) {
			FaceSingleton.getFace().expressInterest (interest, this, this);

		}
	}



	/*
	public Track activateTrack(string trackID, float x, float y, float z) {
		Track track;
		try {
			track = tracks [trackID];
		} catch (KeyNotFoundException) {
			// this shouldn't happen but just in case
			track = new Track (trackID); 
		}

		if (track.setPosition (x, y, z)) {
			foreach (OpenPTrackListener listener in openPTrackListeners) {
				listener.trackEnter (track);				
			}
			Debug.Log ("Activated Track: " + trackID);
		}



		return track;
	}
	*/



	public class TrackHandler : OnData, OnTimeout {
		OpenPTrackProvider providerOuterInstance;
		public TrackHandler(OpenPTrackProvider providerOuterInstance) {
			this.providerOuterInstance = providerOuterInstance;
		}

		public void onData (Interest interest, Data data) {
			JSONNode parsedTrack = JSON.Parse (data.getContent ().toString ());
			string trackID = parsedTrack ["id"];
			//Debug.Log ("TrackHandler data for " + trackID);
			try {
				Track track = providerOuterInstance.tracks[trackID];
				if(providerOuterInstance.flipZY) {
				track.setPosition(parsedTrack["x"].AsFloat, parsedTrack["z"].AsFloat,  parsedTrack["y"].AsFloat);	
				} else {
					track.setPosition(parsedTrack["x"].AsFloat, parsedTrack["y"].AsFloat,  parsedTrack["z"].AsFloat);	
				}
			} catch (KeyNotFoundException) {
				Debug.LogError ("Got Track  data for non-existant track re-creating" + trackID);
				providerOuterInstance.createNewTrackAndExpressInterest (trackID);
				// this shouldn't really happen but it does!
			}

			providerOuterInstance.expressInterestForTrack(trackID, data.getName().get(-1));
				
//			int newSeq = Int32.Parse(interest.getName().get(-1).toEscapedString()) + 1;
//			Interest newTrackInterst = new Interest(data.getName().getPrefix(-1).append(newSeq.ToString()));
//			newTrackInterst.setMustBeFresh(true);
//			newTrackInterst.setInterestLifetimeMilliseconds(providerOuterInstance.config.defaultTrackInterestLifetime);
//			providerOuterInstance.face.expressInterest(newTrackInterst, this, this); 
			//It is to reuse this right? should be better than creating new objects all the time
		}

		public void onTimeout (Interest interest) {
			string trackId = interest.getName ().get (providerOuterInstance.producerNameComponents.trackIdOffset).toEscapedString ();
		//	Debug.Log ("TrackHandler timeout for " + trackId);
			try {
				Track t = providerOuterInstance.tracks[trackId];
				if(! t.isDeadFromTimeout()) {
					FaceSingleton.getFace().expressInterest(interest, this, this);
				}
			} catch {
				// this is expected- timeout for an culled track
				}

		}
	}
}




