namespace CQRSBulkSample {
	class PendingMessage {
		public IMessage Message { get; set; }
		public dynamic TCS { get; set; }
		public bool IsRunning { get; set; }
	}
}
