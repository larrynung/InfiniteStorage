﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfiniteStorage
{
	public class TimeRange
	{
		public DateTime start { get; set; }
		public DateTime end { get; set; }

		public TimeRange(DateTime start, DateTime end)
		{
			this.start = start;
			this.end = end;
		}

		public bool IsValid()
		{
			return start <= end;
		}

		public TimeSpan Span
		{
			get { return end - start; }
		}
	}
}
