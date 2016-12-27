using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface TrackProvider  {
	//TODO make OpenPTrackConsumer.cs implment this
	Dictionary<string, Track> getTracks ();

	Vector3 getMinDims ();
	Vector3 getMaxDims ();
}
