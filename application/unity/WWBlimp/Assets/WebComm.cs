using UnityEngine;
using System.Collections.Generic;
using System.Text;
using ILOG.J2CsMapping.NIO;

using net.named_data.jndn;
using net.named_data.jndn.util;
using net.named_data.jndn.transport;
using net.named_data.jndn.security;
using net.named_data.jndn.security.identity;
using net.named_data.jndn.security.policy;

using ndn_iot.bootstrap;

public class WebComm : MonoBehaviour {


	public delegate void NDN_RFC(string callerID, string[] methodNameAndArgs) ;

	static Dictionary<string, NDN_RFC> rfcs = new Dictionary<string, NDN_RFC> ();


	private static WebComm theWebComm;


	Face face;
	MemoryContentCache memoryContentCache;

	Name fetchPrefix;
	Name linkPrefix;



	public string fetchVerb = "fetch";
	public string linkVerb = "link";

	public int segmentSize = 2000;
	public static int defaultDataFreshnessPeriod = 60000;

	// Use this for initialization



	public Name getLinkPrefix() {
		return linkPrefix;
	}

	public Name getFetchPrefix() {
		return fetchPrefix;
	}

	void Start () {
		if (theWebComm != null) {
			Debug.LogError ("attempt to create two WebComm Objects");
			return;
		}

		theWebComm = this;

		face = FaceSingleton.getFace ();

		// class-specific start
		memoryContentCache = new MemoryContentCache(face);

		fetchPrefix = new Name(FaceSingleton.getSpaceName()).append(fetchVerb);
		linkPrefix = new Name(FaceSingleton.getSpaceName()).append(linkVerb);

		FetchInterestHandler fh = new FetchInterestHandler();
		memoryContentCache.registerPrefix(fetchPrefix, fh, fh);

		LinkInterestHandler lh = new LinkInterestHandler(this);
		face.registerPrefix(linkPrefix, lh, lh);

	}

	public void handleLink(string id, string data) {
		//TODO: the deivce name is coming in URL encoded. There is not Name from url.  
		// this s probably not complete.
		id = id.Replace("%2F", "/");
		data = data.Replace("%2C", ",");
		data = data.Replace("%2F", "/");
		invokeRFC (id, data);
	}

	public static void publishHtml(string toDeivce, string html) {
		if (theWebComm != null) {
			theWebComm.publishHtmlForMobile (toDeivce, html);
		}
	}

	public void publishHtmlForMobile(string mobileName, string htmlString, string identifier = "") {
		int startIdx = 0;
		int endIdx = 0;

		int finalBlockNumber = (int)Mathf.Floor((float)htmlString.Length / segmentSize);
		int currentBlockNumber = 0;

		// by default, append the current timestamp as version number to differentiate the data
		// otherwise use the given component
		string version = "";
		System.TimeSpan t = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
		int secondsSinceEpoch = (int)t.TotalSeconds;

		if (version == "") {
			version = secondsSinceEpoch.ToString();
		}

		while (startIdx < htmlString.Length) {
			Data data = new Data(new Name(fetchPrefix).append(mobileName).append(version).append(Name.Component.fromSegment(currentBlockNumber)));
			print("added data: " + data.getName().toUri());
			endIdx = htmlString.Length > (startIdx + segmentSize) ? (startIdx + segmentSize) : htmlString.Length;
			data.setContent(new Blob(htmlString.Substring(startIdx, endIdx)));
			data.getMetaInfo().setFinalBlockId(Name.Component.fromSegment(finalBlockNumber));
			data.getMetaInfo().setFreshnessPeriod(defaultDataFreshnessPeriod);

			startIdx = endIdx;
			currentBlockNumber += 1;
			memoryContentCache.add(data);
		}
		return;
	}


	public static void addRFC(string functionName, NDN_RFC function) {
		rfcs [functionName] = function;

	}

	public static void removeRFC(string functionName) {
		rfcs.Remove (functionName);
	}

	static void invokeRFC(string device, string invocationString) {
		string[] mathodNameAndArgs = invocationString.split (",");

		try {
			rfcs [mathodNameAndArgs [0]] (device, mathodNameAndArgs);
		}
		catch {
			Debug.LogError ("Unable to invoke " + invocationString + " from device:" + device);
		}
	}

	public static string expressionLink(string displayText, string data) {
		return "<a href=\"#\" class=\"expression\" data=\"" + data + "\">" + displayText + "</a>";
	}

	// Update is called once per frame
	void Update () {
	
	}
}
