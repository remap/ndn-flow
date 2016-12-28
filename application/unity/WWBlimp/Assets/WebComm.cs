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
		face = FaceSingleton.getFace ();

		// class-specific start
		memoryContentCache = new MemoryContentCache(face);

		fetchPrefix = new Name(FaceSingleton.getSpaceName()).append(fetchVerb);
		linkPrefix = new Name(FaceSingleton.getSpaceName()).append(linkVerb);

		FetchInterestHandler fh = new FetchInterestHandler();
		memoryContentCache.registerPrefix(fetchPrefix, fh, fh);

		LinkInterestHandler lh = new LinkInterestHandler(this);
		face.registerPrefix(linkPrefix, lh, lh);

		// publish html content for given mobile, call when needed
		// for this example call this on start
		string htmlString = "<p>Hello world!</p>";
		string mobileName = "/home/browser1";
		publishHtmlForMobile(mobileName, htmlString);
	}
	public void gotALink(string s) {
		print (s);
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
			print(data.getMetaInfo().getFreshnessPeriod());

			startIdx = endIdx;
			currentBlockNumber += 1;
			memoryContentCache.add(data);
		}
		return;
	}

	// Update is called once per frame
	void Update () {
	
	}
}
