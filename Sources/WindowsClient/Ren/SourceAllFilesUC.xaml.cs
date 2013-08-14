﻿#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using InfiniteStorage.Model;
using Microsoft.Win32;
using Waveface.Model;

#endregion

namespace Waveface.Client
{
	public partial class SourceAllFilesUC : UserControl
	{
		private IService m_currentDevice;
		private MainWindow m_mainWindow;

		private DispatcherTimer m_startTimer;
		private DispatcherTimer m_refreshTimer;

		private int m_videosCount;
		private int m_photosCount;
		private int m_hasOriginCount;

		private string m_basePath;
		private string m_thumbsPath;

		private List<FileEntry> m_fileEntries { get; set; }

		private Dictionary<string, List<FileEntry>> m_YM_Files;
		private List<List<FileEntry>> m_months;
		private ObservableCollection<EventUC> m_eventUCs;

		public SourceAllFilesUC()
		{
			InitializeComponent();

			m_basePath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\BunnyHome", "ResourceFolder", "");
			m_thumbsPath = Path.Combine(m_basePath, ".thumbs");

			InitTimer();
		}

		private void InitTimer()
		{
			m_startTimer = new DispatcherTimer();
			m_startTimer.Tick += StartTimerOnTick;
			m_startTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);

			m_refreshTimer = new DispatcherTimer();
			m_refreshTimer.Tick += RefreshTimerOnTick;
			m_refreshTimer.Interval = new TimeSpan(0, 0, 0, 0, 2000);
		}

		public void Stop()
		{
			if (m_startTimer != null)
			{
				m_startTimer.Stop();
			}
		}

		public void Load(IService device, MainWindow mainWindow)
		{
			gridWaitingPanel.Visibility = Visibility.Visible;

			m_mainWindow = mainWindow;
			m_currentDevice = device;

			m_startTimer.Start();
		}

		private void StartTimerOnTick(object sender, EventArgs e)
		{
			m_startTimer.Stop();

			List<FileAsset> _files = GetFilesFromDB();

			if (_files.Count == 0)
			{
				tbTitle0.Visibility = Visibility.Collapsed;
				tbTitle.Visibility = Visibility.Collapsed;
				tbTotalCount.Visibility = Visibility.Collapsed;

				return;
			}

			gridWaitingPanel.Visibility = Visibility.Collapsed;

			prepareData(_files);

			tbTitle0.Visibility = Visibility.Visible;
			tbTitle.Visibility = Visibility.Visible;
			tbTotalCount.Visibility = Visibility.Visible;

			tbTitle.Text = m_currentDevice.Name;

			ShowEvents_Init();

			m_refreshTimer.Start();
		}

		void RefreshTimerOnTick(object sender, EventArgs e)
		{
			m_refreshTimer.Stop();

			List<FileAsset> _files = GetFilesFromDB();
			int _hasOriginCount = GetHasOriginCount(_files);

			if ((_hasOriginCount != m_hasOriginCount) || (_files.Count != m_fileEntries.Count))
			{
				prepareData(_files);

				ShowEvents();
			}

			m_refreshTimer.Start();
		}

		private List<FileAsset> GetFilesFromDB()
		{
			using (var _db = new MyDbContext())
			{
				IQueryable<FileAsset> _q = from _f in _db.Object.Files
										   where _f.device_id == m_currentDevice.ID && !_f.deleted
										   select _f;

				return _q.ToList();
			}
		}

		public void prepareData(List<FileAsset> files)
		{
			m_fileEntries = new List<FileEntry>();

			List<FileEntry> _fCs = files.Select(x => new FileEntry
			{
				id = x.file_id.ToString(),
				file_name = x.file_name,
				tiny_path = Path.Combine(m_thumbsPath, x.file_id + ".s92.thumb"),
				taken_time = x.event_time,
				width = x.width,
				height = x.height,
				size = x.file_size,
				type = x.type,
				saved_path = "", //Path.Combine(m_basePath, x.saved_path),
				has_origin = x.has_origin
			}).ToList();

			m_fileEntries = _fCs;
			m_fileEntries = m_fileEntries.OrderBy(o => o.taken_time).ToList();

			m_hasOriginCount = GetHasOriginCount(files);
		}

		private int GetHasOriginCount(List<FileAsset> files)
		{
			int _hasOriginCount = 0;

			foreach (var _entry in files)
			{
				if (_entry.has_origin)
				{
					_hasOriginCount++;
				}
			}

			return _hasOriginCount;
		}

		public Dictionary<string, List<FileEntry>> GroupingByMonth()
		{
			Dictionary<string, List<FileEntry>> _YM_Files = new Dictionary<string, List<FileEntry>>();

			m_months = new List<List<FileEntry>>();

			foreach (FileEntry _item in m_fileEntries)
			{
				DateTime _dt = _item.taken_time;

				string _by = _dt.ToString("yyyy-MM");

				if (!_YM_Files.ContainsKey(_by))
				{
					_YM_Files.Add(_by, new List<FileEntry>());
				}

				_YM_Files[_by].Add(_item);
			}

			_YM_Files.Keys.ToList().Sort();

			foreach (string _day in _YM_Files.Keys)
			{
				m_months.Add(_YM_Files[_day]);
			}

			m_months.Reverse();

			return _YM_Files;
		}

		#region Show

		private void ShowEvents_Init()
		{
			Cursor = Cursors.Wait;

			m_photosCount = 0;
			m_videosCount = 0;

			m_eventUCs = new ObservableCollection<EventUC>();
			listBoxEvent.ItemsSource = m_eventUCs;

			m_YM_Files = GroupingByMonth();

			foreach (List<FileEntry> _entries in m_months)
			{
				EventUC _ctl = new EventUC
								   {
									   Event = _entries,
									   YM = _entries[0].taken_time.ToString("yyyy-MM")
								   };

				_ctl.SetUI();

				m_photosCount += _ctl.PhotosCount;
				m_videosCount += _ctl.VideosCount;

				m_eventUCs.Add(_ctl);

				DoEvents();
			}

			ShowInfor();
		}

		private void ShowEvents()
		{
			Cursor = Cursors.Wait;

			m_photosCount = 0;
			m_videosCount = 0;

			Dictionary<string, List<FileEntry>> _YM_Files = GroupingByMonth();

			foreach (List<FileEntry> _entries in m_months)
			{
				EventUC _ctl = null;
				string _YM = _entries[0].taken_time.ToString("yyyy-MM");

				if (m_YM_Files.Keys.Contains(_YM))
				{
					foreach (EventUC _eventUc in m_eventUCs)
					{
						if (_eventUc.YM == _YM)
						{
							_ctl = _eventUc;
							break;
						}
					}

					_ctl.Event = _entries;
				}
				else
				{
					_ctl = new EventUC
						       {
							       Event = _entries,
							       YM = _YM
						       };

					m_eventUCs.Add(_ctl);
				}

				_ctl.SetUI();

				m_photosCount += _ctl.PhotosCount;
				m_videosCount += _ctl.VideosCount;

				DoEvents();
			}

			m_YM_Files = _YM_Files;

			ShowInfor();
		}

		private void ShowInfor()
		{
			if (m_eventUCs.Count == 0)
			{
				gridWaitingPanel.Visibility = Visibility.Visible;
				tbTotalCount.Text = "";
			}
			else
			{
				string _tbTotalCount = GetCountsString(m_photosCount, m_videosCount);
				gridWaitingPanel.Visibility = Visibility.Collapsed;
				tbTotalCount.Text = _tbTotalCount;
			}
		}

		public static string GetCountsString(int photosCount, int videosCount)
		{
			string _c = string.Empty;

			string _photo = " " + (string)Application.Current.FindResource("photo");
			string _photos = " " + (string)Application.Current.FindResource("photos");
			string _video = " " + (string)Application.Current.FindResource("video");
			string _videos = " " + (string)Application.Current.FindResource("videos");

			if (photosCount > 0)
			{
				_c = photosCount + ((photosCount == 1) ? _photo : _photos);
			}

			if (videosCount > 0)
			{
				if (photosCount > 0)
				{
					_c = _c + ", ";
				}

				_c = _c + videosCount + ((videosCount == 1) ? _video : _videos);
			}

			return _c;
		}

		#endregion

		private void listBoxEvent_LayoutUpdated(object sender, EventArgs e)
		{
			Cursor = Cursors.Arrow;
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Cursor = Cursors.Wait;
		}

		#region DoEvents

		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public void DoEvents()
		{
			var _frame = new DispatcherFrame();

			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), _frame);

			Dispatcher.PushFrame(_frame);
		}

		public object ExitFrame(object f)
		{
			((DispatcherFrame)f).Continue = false;

			return null;
		}

		#endregion
	}
}