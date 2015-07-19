using System.Collections.Generic;

namespace CQRSBulkSample {
	class ReferenceEqualityComparer<TKey> : IEqualityComparer<TKey> {
		public static ReferenceEqualityComparer<TKey> Instance = new ReferenceEqualityComparer<TKey>();

		public bool Equals(TKey x, TKey y) {
			return object.ReferenceEquals(x, y);
		}

		public int GetHashCode(TKey obj) {
			return obj.GetHashCode();
		}
	}
}
