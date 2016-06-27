using UnityEngine;

public class Track {
	public string id;
	public int timeoutCnt;

	Vector3 position;

	public Track(string id, int timeoutCnt) {
		this.id = id;  
		this.timeoutCnt = timeoutCnt;
		position = new Vector3 ();
	}
	//TODO - way to denote that a hint was received but not track data


	public void setPosition(float x, float y, float z) {
		position.Set (x, y, z);
		this.timeoutCnt = 0; //update timeoutCnt since we just got info
	}

	public Vector3 getPosition () {
		return position;
	}

	public int incTimeoutCnt() {
		timeoutCnt++;
		return timeoutCnt;
	}
}