using UnityEngine;
using System.Collections;

public class ImageDrop : MonoBehaviour {

	public GameObject dropClothPrefab;


	string absoluteMediaPath;

//	public float dropHeight = 100f;

	// Use this for initialization
	void Start () {
	
	}
	// Update is called once per frame
	void Update () {
	
	}

	public void Drop(Vector3 dropLocation, Vector3 dropVelocity, string imageName) {


		Quaternion rot =  Quaternion.Euler (new Vector3 (0, 90, 0));
		//Vector3 loc = new Vector3 (dropLocation.x, dropHeight, dropLocation.z);
		GameObject cloth = (GameObject) Instantiate(dropClothPrefab, dropLocation, rot);
		Rigidbody clothRigidbody = cloth.GetComponent<Rigidbody>();
		clothRigidbody.velocity = dropVelocity;

		Renderer rend = cloth.GetComponent<Renderer>();

		WWW www = new WWW("file://"  + imageName);
		loadImageUrl (www);
		if (www.error == null) {
			rend.material.mainTexture = www.texture;
		} else {
			Debug.LogError ("Error loading image " + imageName + ": " + www.error);
		}
		www.Dispose(); 
		www = null; 

	}

	IEnumerator loadImageUrl(WWW www) { 
		yield return www;


	} 


}
