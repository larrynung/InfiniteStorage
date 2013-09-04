﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace Waveface.Client
{
	/// <summary>
	/// ShareCallout.xaml 的互動邏輯
	/// </summary>
	public partial class ShareCallout : UserControl
	{
		public String SelectionText
		{
			get { return (String)GetValue(SelectionTextProperty); }
			set { SetValue(SelectionTextProperty, value); }
		}

		public static readonly DependencyProperty SelectionTextProperty =
			DependencyProperty.Register("SelectionText", typeof(String), typeof(ShareCallout), null);


		public event EventHandler CreateOnlineAlbumClicked;
		public event EventHandler SaveToClicked;

		public ShareCallout()
		{
			InitializeComponent();
		}

		private void btnOnlineAlbum_click(Object sender, RoutedEventArgs e)
		{
			var handler = CreateOnlineAlbumClicked;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		private void btnSaveTo_click(Object sender, RoutedEventArgs e)
		{
			var handler = SaveToClicked;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}
	}
}