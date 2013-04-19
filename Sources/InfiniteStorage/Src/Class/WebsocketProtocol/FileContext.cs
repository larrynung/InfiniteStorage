﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfiniteStorage.Model;

namespace InfiniteStorage.WebsocketProtocol
{
	public class FileContext
	{
		public string file_name { get; set; }
		public long file_size { get; set; }
		public string folder { get; set; }
		public DateTime datetime { get; set; }
		public FileAssetType type { get; set; }
	}
}
