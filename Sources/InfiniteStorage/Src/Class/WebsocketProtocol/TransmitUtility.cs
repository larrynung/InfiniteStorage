﻿using InfiniteStorage.Model;
using System;
using System.IO;
using System.Linq;

namespace InfiniteStorage.WebsocketProtocol
{
	class TransmitUtility : ITransmitStateUtility, IFileUtility
	{
		public void SaveFileRecord(Model.FileAsset file)
		{
			using (var db = new MyDbContext())
			{
				db.Object.Files.Add(file);
				db.Object.SaveChanges();
			}
		}

		public bool HasDuplicateFile(FileContext file, string device_id)
		{
			var full_path = Path.Combine(file.folder, file.file_name);

			using (var db = new MyDbContext())
			{
				var saved_file = from f in db.Object.Files
								 where f.file_path.Equals(full_path, StringComparison.InvariantCultureIgnoreCase) && f.device_id == device_id
								 select f;


				var pending_files = from f in db.Object.PendingFiles
									where f.file_path.Equals(full_path, StringComparison.InvariantCultureIgnoreCase) && f.device_id == device_id
									select f;

				return saved_file.Any() || pending_files.Any();
			}
		}

		public long GetNextSeq()
		{
			return SeqNum.GetNextSeq();
		}
	}
}
