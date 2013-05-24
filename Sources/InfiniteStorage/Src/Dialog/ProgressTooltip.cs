﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using InfiniteStorage.WebsocketProtocol;
using InfiniteStorage.Model;
using InfiniteStorage.Properties;
using InfiniteStorage.Win32;
using System.IO;

namespace InfiniteStorage
{
	public partial class ProgressTooltip : Form
	{
		private static ProgressTooltip instance = new ProgressTooltip();

		public static ProgressTooltip Instance
		{
			get { return instance; }
		}

		public ProgressTooltip()
		{
			InitializeComponent();
		}

		public void OnFileEnding(object sender, WebsocketEventArgs args)
		{
			var file_id = args.ctx.fileCtx.file_id;
			var file_name = args.ctx.fileCtx.file_name;
			var file_type = args.ctx.fileCtx.type;

			SynchronizationContextHelper.SendMainSyncContext(() =>
			{
				showFile(file_id, file_name, file_type);
			});
		}

		private void showFile(Guid file_id, string file_name, FileAssetType type)
		{
			try
			{
				filename.Text = file_name;

				PendingFile file = null;
				using (var db = new MyDbContext())
				{
					var q = from f in db.Object.PendingFiles
							where f.file_id == file_id
							select f;


					file = q.FirstOrDefault();
				}

				if (file == null)
					return;

				if (type == FileAssetType.image)
				{
					pictureBox1.ImageLocation = Path.Combine(MyFileFolder.Photo, ".pending", file.saved_path);
				}
				else
				{
					pictureBox1.Image = Resources.video;
				}

				Show();
			}
			catch (Exception err)
			{
				log4net.LogManager.GetLogger(GetType()).Warn(err.ToString());
			}
		}

		public void ShowWaitingDevice(string dev)
		{
			if (InvokeRequired)
			{
				Invoke(new MethodInvoker(() => { showWaiting(dev); }));
			}
			else
			{
				showWaiting(dev);
			}
		}

		private void showWaiting(string dev)
		{
			pictureBox1.Image = Resources.Waiting;
			filename.Text = dev + Resources.MenuItem_Preparing;
			Show();
		}

		private void ProgressTooltip_Load(object sender, EventArgs e)
		{
			Icon = Resources.ProductIcon;
			Text = Resources.ProductName;

			this.StartPosition = FormStartPosition.Manual;
			Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Size.Width,
								 Screen.PrimaryScreen.WorkingArea.Height - this.Size.Height);
		}

		private void ProgressTooltip_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
			{
				e.Cancel = true;
				Hide();
			}
		}
	}
}