namespace ndn_iot.discovery {
	using System;

	public interface ExternalObserver {
		void onStateChanged(string name, string msgTyle, string message);
	}
}