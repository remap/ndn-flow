namespace ndn_iot.discovery {
	using System;

	public class EntityInfoBase {
		public EntityInfoBase() {
			timeoutCountCnt_ = 0;
		}

		public bool incrementTimeoutCnt() {
			if (timeoutCountCnt_ ++ > SyncBasedDiscovery.TimeoutCntThreshold) {
				return true;
			} else {
				return false;
			}
		}

		public void resetTimeoutCnt() {
			timeoutCountCnt_ = 0;
		}

		public int getTimeoutCountCnt() {
			return timeoutCountCnt_;
		}

		protected int timeoutCountCnt_;
	}
}