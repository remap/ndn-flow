using UnityEngine;

public class Track {
	public string id;
	Vector3 position;

	public Vector3 unityPosition;

	public static int updatesTillActive = 4;
	public static int timeoutsTillDead = 10;
	public int updateCount = 0;
	public int timeoutCount = 0;

	public enum State { UNINITIALIZED, PENDING, ACTIVE, DEAD }
	State state = State.UNINITIALIZED;

	bool wasTouchedByHint = true;



	// Do not use this 
	// depricated
	public Track(string id, int ignoreThisNumber) {
		this.id = id;  
		position = new Vector3 ();
	}
	public Track(string id) {
		this.id = id;  
		position = new Vector3 ();

	}
	//TODO - way to denote that a hint was received but not track data

	/* returns true if newly active */
	public bool setPosition(float x, float y, float z) {
		position.Set (x, y, z);
		//Debug.Log("Track(" + id + "): setPosition=" + position);
		updateCount++;
		timeoutCount = 0;
		if (updateCount == updatesTillActive) {
			state = State.ACTIVE;
			return true;
		} else if (updateCount == 1) {
			state = State.PENDING;
			return false;
		} else {
			return false;
		}
	}

	public Vector3 getPosition () {
		return position;
	}

	public State getState() {
		return state;
	}
	public void touchByHint() {
		wasTouchedByHint = true;
	}

	public bool shouldCull() {
		if ((state == State.DEAD) || (!wasTouchedByHint)) {
			return true;
		} else {
			wasTouchedByHint = false;
			return false;
		}
	}
	public void cull() {
		state = State.DEAD;
	}
	public bool isDeadFromTimeout() {
		timeoutCount++;
		if (timeoutCount >= timeoutsTillDead) {
			state = State.DEAD;
			return true;
		}
		return false;
	}
}