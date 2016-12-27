using UnityEngine;
using System.Collections;
using System;


/* this is a class for representing and converting between
 * differn coordiante systems.
 * (it seems like the typo of thing unity should have built into it
 * but I haven't found it - egm
 */
[Serializable]
public class CoordSystem {
	[Serializable]
	public class Dim {
		public float min;
		public float max;
		public bool flipConversions = false;

		public Dim() {}
		public Dim(float mn, float mx) {
			set(mn, mx);
		}

		public float Min {
			get { return min; }
			set { min = value; update (); }
		}
		public float Max {
			get { return max; }
			set { max = value; update (); }
		}

		private float diff;
		private float diffInv;


		public void set(float mn, float mx) {
			min = mn;
			max = mx;
			update();
		}

		public void update() {
			diff = max - min;
			diffInv = 1.0f / diff;
		}

		public float getPercentage(float loc) {
			if (flipConversions) {
				return (max - loc)  * diffInv;
			} else {
				return (loc - min)  * diffInv;
			}

		}

		public float getLocFromPercentage(float perc) {
			if (flipConversions) {
				return max - (diff * perc);
			} else {
				return (diff * perc) + min;
			}
		}

		public float convertFrom(float otherLoc, Dim other) {
		//	Debug.Log(this + "convertFrom(" + otherLoc.ToString() + "," + other.ToString()+ ")");
		//	Debug.Log("     returning " + (min + diff * other.getPercentage (otherLoc)));
			if (flipConversions) {
				return  max - diff * other.getPercentage (otherLoc);
			} else {
				return min + diff * other.getPercentage (otherLoc);
			}
			
		}

		override public string ToString() {
			return "Dim(" + min + ", " + max + ")"; 
		}
	
	}


	public Dim x = new Dim();
	public Dim y = new Dim();
	public Dim z = new Dim();

	public Vector3 convertFrom(Vector3 otherLoc, CoordSystem otherCoordSys) {
	//	Debug.Log (this + "convertFrom: " + otherLoc + "," + otherCoordSys.ToString());

		return new Vector3 (
			x.convertFrom (otherLoc.x, otherCoordSys.x),
			y.convertFrom (otherLoc.y, otherCoordSys.y),
			z.convertFrom (otherLoc.z, otherCoordSys.z));
	}

	public Vector2 convertFrom2D(Vector3 otherLoc, CoordSystem otherCoordSys) {
		return new Vector2 (
			x.convertFrom (otherLoc.x, otherCoordSys.x),
			z.convertFrom (otherLoc.z, otherCoordSys.z));
	}

	override public string ToString() {
		return "CoordSystem(" + x.ToString () + ", " + y.ToString () + ", " + z.ToString () + ")";  
	}

}

