﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using readCamera;

namespace InfiniteStorage
{
	public partial class AskCameraImportDialog : Form
	{
		private List<_deviceInfo> devices;
		public _deviceInfo SelectedCamera { get; set; }

		public AskCameraImportDialog()
		{
			InitializeComponent();
		}

		public AskCameraImportDialog(List<_deviceInfo> devices)
		{
			InitializeComponent();
			this.devices = devices;
		}

		private void AskCameraImportDialog_Load(object sender, EventArgs e)
		{
			listBox1.DataSource = devices;
		}

		private void AskCameraImportDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			
		}

		private void button2_Click(object sender, EventArgs e)
		{
			SelectedCamera = (_deviceInfo)listBox1.SelectedItem;
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}