﻿
namespace InfiniteStorage
{
	public class DeviceSummary
	{
		public TimeRange backup_range { get; set; }

		public long photo_count { get; set; }
		public long audio_count { get; set; }
		public long video_count { get; set; }
	}
}
