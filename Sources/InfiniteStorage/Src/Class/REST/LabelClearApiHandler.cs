﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wammer.Station;
using InfiniteStorage.Model;
using System.Net;

namespace InfiniteStorage.REST
{
	class LabelClearApiHandler : HttpHandler
	{
		public override void HandleRequest()
		{
			CheckParameter("label_id");

			var label_id = new Guid(Parameters["label_id"]);

			using (var db = new MyDbContext())
			{
				var conn = db.Object.Database.Connection;
				conn.Open();


				var cmd = conn.CreateCommand();
				cmd.Connection = conn;
				cmd.CommandText = "delete from [LabelFiles] where label_id = @id";
				cmd.CommandType = System.Data.CommandType.Text;

				var par = cmd.CreateParameter();
				par.ParameterName = "@id";
				par.Value = label_id;
				cmd.Parameters.Add(par);

				var nDeleted = cmd.ExecuteNonQuery();

				if (nDeleted > 0)
				{
					cmd.CommandText = "update [LabelFiles] set seq = @seq where label_id = @id";
					var parSeq = cmd.CreateParameter();
					parSeq.Value = SeqNum.GetNextSeq();
					parSeq.ParameterName = "@seq";
					cmd.ExecuteNonQuery();
				}

				
			}

			respondSuccess();
		}
	}
}
