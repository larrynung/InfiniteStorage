﻿#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

#endregion

namespace Waveface
{
    public class EventPhoto
    {
        public BitmapImage BitmapImage { get; set; }
        public double MyWidth { get; set; }
        public double MyHeight { get; set; }
        public bool IsVideo { get; set; }
        public bool IsPhoto { get; set; }
    }

    public partial class EventUC : UserControl
    {
        private const double HH = 143; //144
        private const double WW = 143; //170

        public List<FileChange> Event { get; set; }
        public int VideosCount { get; set; }
        public int PhotosCount { get; set; }

        private string m_defaultEventName;

        public string DescribeText
        {
            set
            {
                tbDescribe.Text = value;
                m_defaultEventName = value;
            }
            get
            {
                if ((tbDescribe.Text == string.Empty) || (tbDescribe.Text == "Describe the event..."))
                {
                    return m_defaultEventName;
                }
                else
                {
                    return tbDescribe.Text;
                }
            }
        }

        public EventUC()
        {
            InitializeComponent();
        }

        public void SetUI()
        {
            GetCounts();

            SetInfor();

            List<EventPhoto> _controls = new List<EventPhoto>();

            foreach (var _file in Event)
            {
                if (_file.type == 0)
                {
                    //Hack - ToDo
                    //string _path = _file.tiny_path + ".tiny.thumb";
                    string _path;

                    int _idx = _file.tiny_path.LastIndexOf("\\", StringComparison.Ordinal);

                    _path = _file.tiny_path.Substring(0, _idx);
                    _path = _path + "\\" + _file.id + ".small.thumb";

                    if (File.Exists(_path))
                    {
                        EventPhoto _eventPhoto = new EventPhoto
                                                     {
                                                         IsVideo = false,
                                                         IsPhoto = true
                                                     };

                        BitmapImage _bi = new BitmapImage();
                        _bi.BeginInit();
                        _bi.UriSource = new Uri(_path, UriKind.Absolute);
                        _bi.EndInit();

                        _eventPhoto.BitmapImage = _bi;

                        _controls.Add(_eventPhoto);
                    }
                }
                else
                {
                    EventPhoto _eventPhoto = new EventPhoto
                                                 {
                                                     IsVideo = true,
                                                     IsPhoto = false
                                                 };

                    BitmapImage _bi = new BitmapImage();
                    _bi.BeginInit();
                    _bi.UriSource = new Uri("pack://application:,,,/PendingUI;component/Images/video_130x110.png");
                    _bi.EndInit();

                    _eventPhoto.BitmapImage = _bi;

                    _controls.Add(_eventPhoto);
                }
            }

            lbEvent.ItemsSource = _controls;
        }

        private void SetInfor()
        {
            string _timeInterval = string.Empty;
            DateTime _startDateTime = MainWindow.Current.Rt.DateTimeCache[Event[0].taken_time];
            DateTime _endDateTime = MainWindow.Current.Rt.DateTimeCache[Event[Event.Count - 1].taken_time];

            if (Event.Count == 1) //只有一筆事件
            {
                _timeInterval = _startDateTime.ToString("yyyy/MM/dd HH:mm");
            }
            else //多筆事件
            {
                if (_startDateTime.Date == _endDateTime.Date) //同一天
                {
                    if (_startDateTime.ToString("yyyy/MM/dd HH:mm") == _endDateTime.ToString("yyyy/MM/dd HH:mm")) //同時間
                    {
                        _timeInterval = _startDateTime.ToString("yyyy/MM/dd HH:mm");
                    }
                    else //不同時
                    {
                        _timeInterval = _startDateTime.ToString("yyyy/MM/dd HH:mm") + " - " + _endDateTime.ToString("HH:mm");
                    }
                }
                else //不同天
                {
                    _timeInterval = _startDateTime.ToString("yyyy/MM/dd HH:mm") + " - " + _endDateTime.ToString("yyyy/MM/dd HH:mm");
                }
            }

            tbTitle.Text = _timeInterval;

            tbTimeAgo.Text = "(" + GetPrettyDate(_startDateTime) + ")";

            tbTotalCount.Text = MainWindow.GetCountsString(PhotosCount, VideosCount);
        }

        public void GetCounts()
        {
            VideosCount = 0;
            PhotosCount = 0;

            foreach (FileChange _file in Event)
            {
                if (_file.type == 0)
                {
                    PhotosCount++;
                }
                else
                {
                    VideosCount++;
                }
            }
        }

        private void lbEvent_Loaded(object sender, RoutedEventArgs e)
        {
            ChangeSize();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ChangeSize();
        }

        private void ChangeSize()
        {
            List<Size> _sizes = new List<Size>();

            for (int i = 0; i < lbEvent.Items.Count; i++)
            {
                ListBoxItem _lbi = lbEvent.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;

                Size _size = new Size
                                 {
                                     Height = HH
                                 };

                _lbi.Height = HH;

                if (Event[i].width == 0 || Event[i].height == 0)
                {
                    _lbi.Width = WW;
                    _size.Width = WW;
                }
                else
                {
                    _lbi.Width = HH * (Event[i].width / (double)Event[i].height);
                    _size.Width = _lbi.Height;
                }

                _sizes.Add(_size);
            }

            int _start = 0;
            double _wTotal = 0;
            int k = 0;
            double _e = 0;

            foreach (Size _size in _sizes)
            {
                if ((_wTotal + _size.Width) > lbEvent.ActualWidth)
                {
                    double _rs = 0;

                    for (int j = _start; j < k; j++)
                    {
                        _rs += _sizes[j].Width / _sizes[j].Height;
                    }

                    double _h = (lbEvent.ActualWidth - 4) / _rs;
                    double _wSum = 0;

                    for (int j = _start; j < k; j++)
                    {
                        ListBoxItem _lbi = lbEvent.ItemContainerGenerator.ContainerFromIndex(j) as ListBoxItem;
                        _lbi.Height = _h;
                        _lbi.Width = _h * (_size.Width / _size.Height);

                        _wSum += _lbi.Width;
                    }

                    if (lbEvent.ActualWidth - _wSum > _e)
                    {
                        double _dw = (lbEvent.ActualWidth - _wSum) / _e;

                        for (int j = _start; j < k; j++)
                        {
                            ListBoxItem _lbi = lbEvent.ItemContainerGenerator.ContainerFromIndex(j) as ListBoxItem;
                            _lbi.Width += _dw;
                        }
                    }

                    _start = k;
                    _wTotal = 0;
                    _e = 0;
                }
                else
                {
                    _wTotal += _size.Width;
                    _e++;
                }

                k++;
            }
        }

        #region Utility

        public string GetPrettyDate(DateTime date)
        {
            // 1. Get time span elapsed since the date.
            TimeSpan _s = DateTime.Now.Subtract(date);

            // 2. Get total number of days elapsed.
            Int32 _dayDiff = (Int32)_s.TotalDays;

            // 3. Get total number of seconds elapsed.
            Int32 _secDiff = (Int32)_s.TotalSeconds;

            // 4. Handle same-day times.
            if (_dayDiff == 0)
            {
                // A. Less than one minute ago.
                if (_secDiff < 60)
                {
                    return "just now";
                }

                // B. Less than 2 minutes ago.
                if (_secDiff < 120)
                {
                    return "1 minute ago";
                }

                // C.Less than one hour ago.
                if (_secDiff < 3600)
                {
                    return String.Format("{0} minutes ago", Math.Floor((double)_secDiff / 60));
                }

                // D. Less than 2 hours ago.
                if (_secDiff < 7200)
                {
                    return "1 hour ago";
                }

                // E. Less than one day ago.
                if (_secDiff < 86400)
                {
                    return String.Format("{0} hours ago", Math.Floor((double)_secDiff / 3600));
                }
            }

            // 6. Handle previous days.
            if (_dayDiff == 1)
            {
                return "yesterday";
            }

            if (_dayDiff == 2)
            {
                return "2 days ago";
            }

            if (_dayDiff < 7)
            {
                return String.Format("{0} days ago", _dayDiff);
            }

            if (_dayDiff < 14)
            {
                return "last week";
            }

            if (_dayDiff < 21)
            {
                return "2 weeks ago";
            }

            return "over 2 weeks ago";
        }

        private void tbDescribe_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox _textbox = sender as TextBox;
            string _invalid = new string(Path.GetInvalidFileNameChars());
            Regex _rex = new Regex("[" + Regex.Escape(_invalid) + "]");
            _textbox.Text = _rex.Replace(_textbox.Text, "");

            _textbox.CaretIndex = _textbox.Text.Length;
        }

        #endregion
    }
}