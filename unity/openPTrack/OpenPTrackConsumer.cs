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


public class OpenPTrackConsumer : MonoBehaviour  {

	//Public Serializable properties show up in Unity inspector
	public NDNConfig config = new NDNConfig();
	public ProducerNameComponents producerNameComponents = new ProducerNameComponents();


	[System.Serializable]
	public class NDNConfig  {
		public string hostName = "131.229.100.141";
		public int wsPort = 6363;

		// Namespace configuration
		public string rootPrefix = "/ndn/edu/ucla/remap/opt";
		public string spaceName = "node0";

		// Interest configuration
		public int initialReexpressInterval	= 1; // in secs in unity
		public int defaultInitialLifetime	= 2000;
		public int defaultTrackLifetime = 250;
		public int defaultHintLifetime = 500;
		public int trackTimeoutCntThreshold		= 4;
		// The threshold for number of timeouts received in a row to decide not to fetch certain
		// track anymore
		// EGM - this is lame - shouldn't we know its gone from the trackhints?

	}

	[System.Serializable]
	public class ProducerNameComponents {
		public string tracks =  "tracks";
		public string trackHint = "track_hint";
		public int trackIdOffset	= -2;
	}



	// this class is a singleton
	private static OpenPTrackConsumer theConsumer = null;

	public static OpenPTrackConsumer getOpenPTrackConsumer() {
		// this assumes the consumer was already created 
		// so make sure your scrips are execuated in the correct order
		return theConsumer;
	}

			

	//This class uses Java style listeners to get notified about
	//entering and exiting tracks
	// (it might be better to use Unity style messages. 
	// If I did this I probably wouldn't need  to make this class a singleton and script execuation order wouldn't matter- TODO EGM)
	List<OpenPTrackListener> openPTrackListeners = new List<OpenPTrackListener>();

	Face face;
	Name prefix;

	Name.Component startTimeComponent;

	Dictionary<string, Track> activeTracks = new Dictionary<string, Track> ();
	Dictionary<string, Track> pendingTracks = new Dictionary<string, Track> ();


	public Name.Component  getStartTimeComponent() {
		return startTimeComponent;
	}

	public Name.Component  setStartTimeComponent(Name.Component comp) {
		return startTimeComponent = comp;
	}

	public void Update() {
		face.processEvents ();
	}

	public void addListener(OpenPTrackListener listener) {
		openPTrackListeners.Add(listener);
	}

	public void removeListener(OpenPTrackListener listener) {
		openPTrackListeners.Remove (listener);
	}



	// start is called by Unity
	public void Start() {

		if (theConsumer != null) {
			Debug.LogError ("Only one OpenPTrackListener should be created");
		} else {
			theConsumer = this;
		}

		face = new Face (config.hostName);
		this.prefix = new Name (config.rootPrefix).append(config.spaceName);
		startTimeComponent = new Name.Component ();
		expressInitialInterest ();
	}

	void expressInitialInterest() {
		Debug.Log ("expressInitialInterest: start");
		Interest initialInterest = new Interest(this.prefix);
		initialInterest.setMustBeFresh(true);
		initialInterest.setInterestLifetimeMilliseconds(config.defaultInitialLifetime);
		// for initial interest, the rightMostChild is preferred
		initialInterest.setChildSelector(1);

		InitialDataHandler handler = new InitialDataHandler (this);

		Debug.Log ("expressInitialInterest: " +  initialInterest.toUri());
		this.face.expressInterest(initialInterest, handler, handler);
	}


	public Dictionary<string, Track> getTracks() {
		return activeTracks;
	}

	// INITIAL HINT
	public class InitialDataHandler : OnData, OnTimeout {
		OpenPTrackConsumer consumerOuterInstance;
		public InitialDataHandler(OpenPTrackConsumer consumerOuterInstance) {
			this.consumerOuterInstance = consumerOuterInstance;
		}
			

		public void onData (Interest interest, Data data) {
//			Debug.Log ("onData: " +  interest.toUri());

			Name dataName = data.getName ();
		//	Debug.Log ("NDN: Initial data received : " + dataName.toUri ());
				
			if (dataName.size () > consumerOuterInstance.prefix.size ()+1) {
				consumerOuterInstance.setStartTimeComponent(dataName.get (consumerOuterInstance.prefix.size ()));
				//fetchTrackHint 
				consumerOuterInstance.expressHintInterest();
			} else {
				Debug.LogError ("NDN: Initial interest received unexpected data : " + dataName.toUri ());
			}
		}
		public void onTimeout (Interest interest) {
			Debug.LogError ("NDN: Initial interest timed out: " + interest.getName ().toUri ());

			consumerOuterInstance.reexpressInitialInterest();


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
			exclude.appendComponent (excludeComp);
		}

		hintInterest.setMustBeFresh (true);
		hintInterest.setInterestLifetimeMilliseconds (config.defaultHintLifetime);
		HintHandler handler = new HintHandler (this);
		face.expressInterest (hintInterest, handler, handler); 
	}

	//fetchTrack
	public void expressInterestForTrack (string trackID) {
		Name trackName = new Name (prefix);
		trackName.append (startTimeComponent).append (producerNameComponents.tracks).append (trackID).append ("0");
		Interest trackInterest = new Interest (trackName);
		trackInterest.setMustBeFresh (true);
		TrackHandler handler = new TrackHandler (this);
		face.expressInterest (trackInterest, handler,handler);
	}

	// HINT DATA
	// Expected data name: [root]/opt/[node_num]/[start_timestamp]/track_hint/[num]
	public class HintHandler : OnData , OnTimeout {

		OpenPTrackConsumer consumerOuterInstance;
		public HintHandler(OpenPTrackConsumer consumerOuterInstance) {
			this.consumerOuterInstance = consumerOuterInstance;
		}


		public void onData (Interest interest, Data data) {
			JSONNode parsedHint = JSON.Parse (data.getContent ().toString ());


			foreach (JSONNode track in parsedHint["tracks"].AsArray) {
				string trackID = track ["id"];
				// The consumer ignores the sequence number field in the hint for now;
				// As the consumer assumes it's getting the latest via outstanding interest.
				// Right now the consumer does not stop fetching tracks that have become inactive.
				// WHAT!!!!! EGM TODO!!!!
				if (! consumerOuterInstance.activeTracks.ContainsKey (trackID)) { 
					if(! consumerOuterInstance.pendingTracks.ContainsKey(trackID)) { // if new track
						consumerOuterInstance.addPendingTrack(trackID);
					}
				}


			}
			consumerOuterInstance.expressHintInterest (data.getName().get(-1)); //exclues this interest

		}

		public void onTimeout (Interest interest) {
			consumerOuterInstance.face.expressInterest (interest, this, this);

		}
	}


		
	public void addPendingTrack(string trackID) {
		try {
			pendingTracks.Add(trackID, new Track(trackID, 0));	
			expressInterestForTrack (trackID);
		} catch (KeyNotFoundException) {
			// if its already there we don't have to do anything
			// probalby casued by wierd timing issue between hint interest and tack interest
		}
//		Debug.Log ("Pending Track: " + trackID);
	}

	public Track activateTrack(string trackID, float x, float y, float z) {
		Track track;
		try {
			track = pendingTracks [trackID];
		} catch (KeyNotFoundException) {
			// this shouldn't happen but just in case
			track = new Track (trackID, 0); 
		}

		track.setPosition (x, y, z);

		try {
			activeTracks.Add(trackID, track);	
			pendingTracks.Remove(trackID); // its crazy to me you can't remove and return the element in one call in C#

			foreach (OpenPTrackListener listener in openPTrackListeners) {
				listener.trackEnter(track);				
			}


		} catch (KeyNotFoundException) {
			// if its already there we don't have to do anything
			// probalby casued by wierd timing issue between hint interest and tack interest
			// and its already activated
		}


//		Debug.Log ("Activated Track: " + trackID);
		return track;
	}


	public void deactivateTrack(string trackID) {
		
		try {
			Track track = activeTracks[trackID];
			activeTracks.Remove (trackID);
			foreach (OpenPTrackListener listener in openPTrackListeners) {
				listener.trackExit(track);				
			}
		} catch (KeyNotFoundException) {
		}
		if (pendingTracks.ContainsKey (trackID)) {
			try {
				pendingTracks.Remove (trackID);
				// not need to notify listeners because the track wes pending not active
			} catch {
			}
		}
//		Debug.Log ("Deactivated Track: " + trackID);

	}

		

	public class TrackHandler : OnData, OnTimeout {
		OpenPTrackConsumer consumerOuterInstance;
		public TrackHandler(OpenPTrackConsumer consumerOuterInstance) {
			this.consumerOuterInstance = consumerOuterInstance;
		}

		public void onData (Interest interest, Data data) {
			JSONNode parsedTrack = JSON.Parse (data.getContent ().toString ());
			string trackID = parsedTrack ["id"];
			try {
				Track track = consumerOuterInstance.activeTracks[trackID];
				track.setPosition(parsedTrack["x"].AsFloat, parsedTrack["y"].AsFloat, parsedTrack["z"].AsFloat);	
			} catch (KeyNotFoundException) {
				// this means its a pending track (or something really wierd happened)
				consumerOuterInstance.activateTrack (trackID, parsedTrack ["x"].AsFloat, parsedTrack ["y"].AsFloat, parsedTrack ["z"].AsFloat);
			}
				

			int newSeq = Int32.Parse(interest.getName().get(-1).toEscapedString()) + 1;
			Interest newTrackInterst = new Interest(data.getName().getPrefix(-1).append(newSeq.ToString()));
			newTrackInterst.setMustBeFresh(true);
			newTrackInterst.setInterestLifetimeMilliseconds(consumerOuterInstance.config.defaultTrackLifetime);
			consumerOuterInstance.face.expressInterest(newTrackInterst, this, this); 
			//It is to reuse this right? should be better than creating new objects all the time
		}

		public void onTimeout (Interest interest) {

			string trackId = interest.getName ().get (consumerOuterInstance.producerNameComponents.trackIdOffset).toEscapedString ();
			try {
				Track track;
				try {
					 track = consumerOuterInstance.activeTracks [trackId];
				} catch(KeyNotFoundException) {
					track = consumerOuterInstance.pendingTracks [trackId];
				}
				int timeoutCnt = track.incTimeoutCnt();
				if(timeoutCnt <= consumerOuterInstance.config.trackTimeoutCntThreshold) {
					consumerOuterInstance.face.expressInterest(interest, this, this);
				} else {
					consumerOuterInstance.deactivateTrack(trackId);
				}
			} catch(KeyNotFoundException) {
				Debug.Log ("Unexectedly adding track " + trackId + " because of expressed interest");
				// this shouldn't happen but lets add the track just in case
				// if we are here its probably because trackIDs are being mangled somehow
				consumerOuterInstance.addPendingTrack(trackId); 
			}

		}
	}
}




