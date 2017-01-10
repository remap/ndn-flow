using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class TrackToTerrain : MonoBehaviour {


	/* PUBLIC UNITY PROPERTIES */
	public TrackProvider provider;


	public bool ShowDebugTracks = true;

	public float terrainModStepSize = .01f; // track influce is the total amount of hieght all tracks will add to theterrain.
//	public float maxHeight = 100f;  range is always 0-1 thats the way terrain works!
//	public float minHeight = 0f;
	[Tooltip("Track Effect Radius is in Unity Units.")]
	public float trackEffectRadius = 1;
	[Range(0.0f, 100.0f)]
	public float errosionRate = 10;
	public float terrainErrosionDepth = .001f;

	[Tooltip("Grayscale elevation map.  Must be 513x513")] // yes that is a 3
	public Texture2D initialTerrainMap;
//	public float heightMultiplyer;


//	[Tooltip("Inital terrain height.  Downtown is 200f which is ~ 50 units ~ .14")]
//	public float initalHeight = .14f;
	Gaussian gauss;
	/*
	[Serializable]
	public class TerrainColors {
		public Color peakColor = new Color (1f, 1f, 1f);
		[Range(0.0f, 1.0f)]
		public float peakHeight = .75f;

		[Range(0.0f, 1.0f)]
		public float midTop = .70f;
		public Color midColor = new Color (1f, 1f, .2f);
		[Range(0.0f, 1.0f)]
		public float midBottom = .30f;

		[Range(0.0f, 1.0f)]
		public float baseHeight = .20f;
		public Color baseColor = new Color(0f,1f,0f);
	}
	public TerrainColors terrainColors;
*/

	/* PROTECTED */
	Terrain terr; // terrain to modify
	int terrainMapWidth; // heightmap width
	int terrainMapHeight; // heightmap height
//	float terrainMapWidthInv;
//	float terrainMapHeightInv;

	public CoordSystem trackWorldDimentions = new CoordSystem();
	public CoordSystem unityWorldDimentions = new CoordSystem();

	public  TrackProvider trackProvider;

	float[,] heights;



	//float midPeakSpan;
	//float bottomMidSpan;

//	Texture2D texture;
//	int textureWidth; // heightmap width
//	int textureHeight; // heightmap height
//	float textureWidthInv;
//	float textureHeightInv;
//	Color[] colors;


	// Use this for initialization

	public void setupTrackProvider(){
		if (trackProvider == null) {
			trackProvider = GetComponent<TrackProvider> ();
			trackWorldDimentions.x.set (trackProvider.getMinDims ().x, trackProvider.getMaxDims ().x);
			trackWorldDimentions.y.set (trackProvider.getMinDims ().y, trackProvider.getMaxDims ().y);
			trackWorldDimentions.z.set (trackProvider.getMinDims ().z, trackProvider.getMaxDims ().z);

		}
	}

	void Start () {
	
		terr = Terrain.activeTerrain;
		terrainMapWidth = terr.terrainData.heightmapWidth;
		terrainMapHeight = terr.terrainData.heightmapHeight;
		Debug.Log("Terrain Map: " + terrainMapWidth +" x "+terrainMapHeight);
		heights = terr.terrainData.GetHeights(0,0,terrainMapWidth,terrainMapHeight);


		Color[] pixels = initialTerrainMap.GetPixels ();

		int curPixel = 0;
		for (int i = 0; i < terrainMapWidth; i++) {
			for (int j = 0; j < terrainMapHeight; j++) {
				heights [i, j] = pixels [curPixel].grayscale;
				curPixel++;
			}
		}
		terr.terrainData.SetHeights(0,0,heights);

		//might have to move this to update
		setupTrackProvider ();
		unityWorldDimentions.x.flipConversions = true;
		unityWorldDimentions.z.flipConversions = true;
		//trackProvider = (TrackProvider) System.Activator.CreateInstance(System.Type.GetType(trackProviderClassName));

		unityWorldDimentions.x.Min = terr.GetPosition ().x;
		unityWorldDimentions.x.Max = terr.GetPosition ().x + terr.terrainData.size.x;
		unityWorldDimentions.z.Min = terr.GetPosition ().z;
		unityWorldDimentions.z.Max = terr.GetPosition ().z + terr.terrainData.size.z;

		gauss = new Gaussian (-trackEffectRadius, trackEffectRadius);

	//	texture = terr.terrainData.splatPrototypes [0].texture;
	//	textureWidth = texture.width;
	//	textureHeight = texture.height;
	
//		terrainMapWidthInv = 1.0f / terrainMapWidth;
//		terrainMapHeightInv = 1.0f / terrainMapHeight;

		/*
		colors = new Color[textureWidth * textureHeight];

		for (int i = 0; i < colors.Length; i++) {
			colors[i] = terrainColors.baseColor;
		}


		midPeakSpan = terrainColors.peakHeight - terrainColors.midTop;
		bottomMidSpan = terrainColors.midBottom - terrainColors.baseHeight;
*/

	}

	public Vector3 convertTrackLocToUnityDims(Vector3 pos) {
		//Debug.Log ("convertTrackLocToUnityDims: " + pos);
		return unityWorldDimentions.convertFrom (pos, trackWorldDimentions);
	}


	// Update is called once per frame
	void Update () {
		Dictionary<string, Track> tracks = trackProvider.getTracks ();
	
		foreach (Track t in tracks.Values) {
			if (t.getState () == Track.State.ACTIVE) {
				Vector3 pos = t.getPosition ();
				//	Debug.Log("pos: " + pos);

				//picrandom angle then gaussian radias?
				// isthat better?
				float randomAngle = UnityEngine.Random.Range (0.0f, 2 * Mathf.PI);
				float offsetAmount = (float)gauss.next ();
				Vector3 perterpedPos = new Vector3 (pos.x + Mathf.Cos (randomAngle) * offsetAmount, 0, pos.z + Mathf.Sin (randomAngle) * offsetAmount);


				float terrainLocX = trackWorldDimentions.x.getPercentage (perterpedPos.x) * terrainMapWidth;
				float terrainLocY = trackWorldDimentions.z.getPercentage (perterpedPos.z) * terrainMapHeight;


				//	Debug.Log("terrainLoc: " + (int) terrainLocX +","+ terrainLocY);



				//y and x are intentially flipped here
				//it seems that the terrain map does this
				// do i have to exchange with and height?
				// don't matter right now square landscape
				incHeight (terrainMapHeight - (int)terrainLocY, terrainMapWidth - (int)terrainLocX, terrainModStepSize);
			}

		}
		// stocasting minimization of terrain
		//TODO: should this be a multipler or tracks?
		//TODO: some function of the total height?


		for (int i = 0; i < tracks.Count * errosionRate + 1; i++) {
			incHeight (
				UnityEngine.Random.Range (0, terrainMapWidth),
				UnityEngine.Random.Range (0, terrainMapHeight),
				-terrainErrosionDepth);
		}


		terr.terrainData.SetHeights(0,0,heights);

//		texture.SetPixels (colors);
//		texture.Apply ();

	}

	/*
	public Color calcColorForHeight(float h) {

		if (h > terrainColors.peakHeight) {
			return terrainColors.peakColor;
		} else if (h > terrainColors.midTop) {
			float diff = h - terrainColors.midTop;
			return Color.Lerp ( terrainColors.midColor, terrainColors.peakColor,diff / midPeakSpan);
		} else if (h > terrainColors.midBottom) {
			return terrainColors.midColor;
		} else if (h > terrainColors.baseHeight) {
			float diff = h - terrainColors.baseHeight;
			return Color.Lerp (terrainColors.baseColor, terrainColors.midColor,diff / bottomMidSpan);
		} else {
			return terrainColors.baseColor;
		}

	}*/

	void incHeight(int x, int y, float amount) {
		if ((x >= terrainMapWidth) || (x < 0))
			return;
		if ((y >= terrainMapHeight) || (y < 0))
			return;
		float newH = heights [x,y];
		//TODO: add noise to make rounded mound? (or is there enough noise in tracking and movments of people)	
		newH += amount;
		newH = newH >= 1.0f ? 1.0f : newH;
		newH = newH <= 0.0f ? 0.0f : newH;

		heights [x,y] = newH;
	

//		int textureX = (int)(textureWidth * (float)x * terrainMapWidthInv);
//		int textureY = (int)(textureHeight * (float)y * terrainMapHeightInv);

//		colors[textureX + textureY * textureHeight] =  calcColorForHeight (newH);
			
	}
}
