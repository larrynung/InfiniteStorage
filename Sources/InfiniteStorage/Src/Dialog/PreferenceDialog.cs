﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using InfiniteStorage.Properties;

namespace InfiniteStorage
{
	public partial class PreferenceDialog : Form
	{
		public PreferenceDialog()
		{
			InitializeComponent();
			Text = Resources.ProductName;
			Icon = Resources.product_icon;
		}

		private void PreferenceDialog_Load(object sender, EventArgs e)
		{
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.Cancel;
			Close();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.OK;
			MessageBox.Show("Not implemented yet");
			Close();
		}
	}
}
