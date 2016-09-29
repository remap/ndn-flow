namespace ndn_iot.discovery {
	using System;

	public interface EntitySerializer {
		string serialize(EntityInfoBase info);
	}
}