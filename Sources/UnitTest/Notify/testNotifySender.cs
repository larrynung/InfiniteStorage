﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using InfiniteStorage;
using InfiniteStorage.Notify;
using InfiniteStorage.Model;
using Newtonsoft.Json.Linq;

namespace UnitTest.Notify
{
	[TestClass]
	public class testNotifySender
	{
		private Mock<ISubscriptionContext> notifyCtx;
		private Mock<INotifySenderUtil> util;

		[TestInitialize]
		public void setup()
		{
			notifyCtx = new Mock<ISubscriptionContext>();
			util = new Mock<INotifySenderUtil>();
		}


		[TestMethod]
		public void sendSingleFile()
		{
			string sentData = null;
			notifyCtx.Setup(x => x.Send(It.IsAny<string>())).Callback<string>((data) => { sentData = data; }).Verifiable();
			notifyCtx.Setup(x => x.files_from_seq).Returns(1000);
			notifyCtx.Setup(x => x.subscribe_files).Returns(true);

			var retFiles = new List<FileChangeData>
			{
				new FileChangeData {
					dev_id = "id1",
					id = Guid.NewGuid(),
					file_name ="file1",
					height = 1024,
					width = 768,
					dev_name = "dev",
					dev_type = 0,
					device_folder = "iphone",
					size = 12345,
					saved_path = @"2012\2012-10\file1.jpg",
					seq = 1000,
					thumb_ready = true,
					type = 0,
					deleted = false
				},
			};
			util.Setup(x => x.QueryChangedFiles(1000)).Returns(retFiles);

			
			var sender = new NotifySender(notifyCtx.Object, util.Object);
			sender.Notify();


			notifyCtx.VerifyAll();
			Assert.AreEqual(1001, sender.file_seq);
			Assert.IsNotNull(sentData);
			
			var o = JObject.Parse(sentData);
			Assert.IsNotNull(o["file_changes"]);
			
			var fileChanges = o["file_changes"];
			Assert.IsTrue(fileChanges.Type == JTokenType.Array);
			Assert.AreEqual(1, fileChanges.Count());

			var file0 = fileChanges.ElementAt(0);
			Assert.IsNotNull(file0);

			Assert.AreEqual(retFiles[0].id.ToString(), file0["id"]);
			Assert.AreEqual(retFiles[0].file_name, file0["file_name"]);
			Assert.AreEqual("/iphone/2012/2012-10", file0["folder"]);

			Assert.IsTrue(file0["thumb_ready"].Value<bool>());
			Assert.AreEqual(retFiles[0].width, file0["width"].Value<long>());
			Assert.AreEqual(retFiles[0].height, file0["height"].Value<long>());
			Assert.AreEqual(retFiles[0].size, file0["size"].Value<long>());
			Assert.AreEqual(retFiles[0].type, file0["type"].Value<long>());

			Assert.AreEqual(retFiles[0].dev_id, file0["dev_id"]);
			Assert.AreEqual(retFiles[0].dev_name, file0["dev_name"]);
			Assert.AreEqual(retFiles[0].dev_type, file0["dev_type"]);

			Assert.IsFalse(file0["deleted"].Value<bool>());
			Assert.AreEqual(retFiles[0].seq, file0["seq"].Value<long>());
		}

		[TestMethod]
		public void sendLabels_with_old_seq()
		{
			string sentData = null;
			notifyCtx.Setup(x => x.Send(It.IsAny<string>())).Callback<string>(x => { sentData = x; });
			notifyCtx.Setup(x => x.subscribe_labels).Returns(true);

			var allLabels = new List<Label> { new Label { label_id = Guid.NewGuid(), name = "name1", seq = 1000 } };
			var labelFiles = new List<FileChangeData> { 
				new FileChangeData { id = Guid.NewGuid(), device_folder = "a", saved_path = "b"}, 
				new FileChangeData { id = Guid.NewGuid(), device_folder = "a", saved_path = "b"}
			};

			util.Setup(x => x.QueryAllLabels()).Returns(allLabels);
			util.Setup(x => x.QueryLabeledFiles(allLabels.ElementAt(0).label_id)).Returns(labelFiles);


			var sender = new NotifySender(notifyCtx.Object, util.Object);
			sender.label_seq.Add(allLabels[0].label_id, 5);
			sender.Notify();

			
			notifyCtx.VerifyAll();
			util.VerifyAll();

			Assert.IsNotNull(sentData);
			var o = JObject.Parse(sentData);

			Assert.AreEqual(allLabels.ElementAt(0).label_id.ToString(), o["label_id"].Value<string>());
			Assert.AreEqual(allLabels.ElementAt(0).name, o["label_name"].Value<string>());
			Assert.AreEqual(2, o["files"].Count());
			Assert.AreEqual(labelFiles[0].id.ToString(), o["files"].ElementAt(0)["id"].Value<string>());
			Assert.AreEqual(labelFiles[1].id.ToString(), o["files"].ElementAt(1)["id"].Value<string>());

			Assert.AreEqual(1000L, sender.label_seq[allLabels[0].label_id]);
		}

		[TestMethod]
		public void sendLabels_no_old_seq()
		{
			string sentData = null;
			notifyCtx.Setup(x => x.Send(It.IsAny<string>())).Callback<string>(x => { sentData = x; });
			notifyCtx.Setup(x => x.subscribe_labels).Returns(true);

			var allLabels = new List<Label> { new Label { label_id = Guid.NewGuid(), name = "name1", seq = 1000 } };
			var labelFiles = new List<FileChangeData> { 
				new FileChangeData { id = Guid.NewGuid(), device_folder = "a", saved_path = "b"}, 
				new FileChangeData { id = Guid.NewGuid(), device_folder = "a", saved_path = "b"}
			};

			util.Setup(x => x.QueryAllLabels()).Returns(allLabels);
			util.Setup(x => x.QueryLabeledFiles(allLabels.ElementAt(0).label_id)).Returns(labelFiles);


			var sender = new NotifySender(notifyCtx.Object, util.Object);
			sender.Notify();


			notifyCtx.VerifyAll();
			util.VerifyAll();

			Assert.IsNotNull(sentData);

			Assert.AreEqual(1000L, sender.label_seq[allLabels[0].label_id]);
		}

		[TestMethod]
		public void do_not_send_label_due_to_old_seq_is_larger_than_seq()
		{
			string sentData = null;
			notifyCtx.Setup(x => x.Send(It.IsAny<string>())).Callback<string>(x => { sentData = x; });
			notifyCtx.Setup(x => x.subscribe_labels).Returns(true);

			var allLabels = new List<Label> { new Label { label_id = Guid.NewGuid(), name = "name1", seq = 1000 } };
			var labelFiles = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

			util.Setup(x => x.QueryAllLabels()).Returns(allLabels);

			var sender = new NotifySender(notifyCtx.Object, util.Object);
			sender.label_seq.Add(allLabels[0].label_id, 1234L);
			sender.Notify();


			util.VerifyAll();

			Assert.IsNull(sentData);
		}
	}
}
