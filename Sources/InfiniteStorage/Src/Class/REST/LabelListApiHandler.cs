﻿using InfiniteStorage.Model;
using System.Collections.Generic;
using System.Linq;
using Wammer.Station;
using System;

namespace InfiniteStorage.REST
{
	class LabelListApiHandler : HttpHandler
	{
		public override void HandleRequest()
		{

			var util = new Notify.NotifySenderUtil();

			List<Label> labels;

			using (var db = new MyDbContext())
			{
				labels = (from lb in db.Object.Labels
						  where !lb.deleted && lb.label_id != Guid.Empty
						  select lb).ToList();
			}

			var result = new List<object>();

			foreach (var label in labels)
			{
				var files = util.QueryLabeledFiles(label.label_id).Select(x => x.id).ToList();

				var data = new
				{
					label_id = label.label_id,
					label_name = label.name,
					files = files,
					seq = label.seq,
					cover_url = "/label_cover/" + label.label_id.ToString() + "?seq=" + label.seq,
					auto_type = label.auto_type,
					on_air = label.on_air
				};

				result.Add(data);
			}


			respondSuccess(new { labels = result, home_sharing = HomeSharing.Enabled });
		}
	}
}
