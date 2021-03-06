﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Waveface.Common;
using WebSocketSharp.Server;
using Wammer.Station;
using InfiniteStorage.Notify;
using InfiniteStorage.Properties;
using System.Drawing;
using InfiniteStorage.REST;
using System.IO;
using Microsoft.Win32;
using InfiniteStorage.Share;
using InfiniteStorage.Data;

namespace InfiniteStorage
{
	public class StationServer
	{
		private NotifyIcon m_notifyIcon;
		private NotifyIconController m_notifyIconController;
		private Timer m_NotifyTimer;
		private NoReentrantTimer m_pingTimer;
		private NoReentrantTimer m_ReRegBonjourTimer;
		private NoReentrantTimer m_recentLabelTimer;
		private WebSocketServer<InfiniteStorageWebSocketService> backup_server;
		private WebSocketServer<NotifyWebSocketService> notify_server;
		private WebSocketServer<Pair.PairWebSocketService> pair_server;
		private HttpServer rest_server;
		private Notifier m_notifier;
		private AutoLabelController m_autoLabel;
		private AutoUpdate m_autoUpdate;
		private ThumbnailCreator m_thumbnailCreator;
		private ShareLabelMonitor m_shareMonitor;

		private List<WebsocketProtocol.ProtocolContext> waitForUserAccept = new List<WebsocketProtocol.ProtocolContext>();
		private object userAcceptCS = new object();

		public StationServer()
		{
			// ----- notify icon and controller -----
			m_notifyIcon = new NotifyIcon();
			m_notifyIconController = new NotifyIconController(m_notifyIcon, this);
			initNotifyIconAndController();

			// ----- notify icon menu refresh ----
			m_NotifyTimer = new Timer();
			m_NotifyTimer.Tick += (s, e) => { m_notifyIconController.refreshNotifyIconContextMenu(); };
			m_NotifyTimer.Interval = 200;

			// ----- auto label ------
			m_autoLabel = new AutoLabelController();
			InfiniteStorageWebSocketService.FileReceived += ProgressTooltip.Instance.OnFileEnding;
			InfiniteStorageWebSocketService.FileReceived += m_autoLabel.FileReceived;

			// ----- pair -----
			InfiniteStorageWebSocketService.PairingRequesting += InfiniteStorageWebSocketService_PairingRequesting;
			Pair.PairWebSocketService.PairingModeChanging += PairWebSocketService_PairingModeChanging;
			Pair.PairWebSocketService.NewDeviceAccepting += PairWebSocketService_NewDeviceAccepting;
			Pair.PairWebSocketService.NewDeviceRejecting += PairWebSocketService_NewDeviceRejecting;


			// ----- backup status timer -----
			m_pingTimer = new NoReentrantTimer(pingPeerToCheckConnection, null, 3000, 10000);
			m_pingTimer.Start();

			// ----- notifier -----
			m_notifier = new Notifier();
			NotifyWebSocketService.Subscribing += m_notifier.OnSubscribing;
			NotifyWebSocketService.Disconnected += m_notifier.OnChannelDisconnected;



			// ----- rest server -----
			rest_server = new HttpServer(14005);
			rest_server.AddHandler("/image/", new ImageApiHandler());
			rest_server.AddHandler("/label/list_all", new LabelListApiHandler());
			rest_server.AddHandler("/label/tag", new LabelTagApiHandler());
			rest_server.AddHandler("/label/untag", new LabelUntagApiHandler());
			rest_server.AddHandler("/label/clear", new LabelClearApiHandler());
			rest_server.AddHandler("/label/rename", new LabelRenameApiHandler());
			rest_server.AddHandler("/label/add", new LabelAddApiHandler());
			rest_server.AddHandler("/label/delete", new LabelDeleteApiHandler());
			rest_server.AddHandler("/label/get", new LabelGetApiHandler());
			rest_server.AddHandler("/label/on_air", new LabelOnAirApiHandler());
			rest_server.AddHandler("/label/share", new LabelShareApiHandler());
			rest_server.AddHandler("/label/invite", new LabelInviteApiHandler());
			rest_server.AddHandler("/file/get", new FileGetApiHandler());
			rest_server.AddHandler("/pending/get", new PendingGetApiHandler());
			rest_server.AddHandler("/pending/sort", new PendingSortApiHandler());
			rest_server.AddHandler("/label_cover", new LabelCoverApiHandler());
			

			m_ReRegBonjourTimer = new NoReentrantTimer(reregisterBonjour, null, 60 * 1000, 60 * 1000);

			// ----- remove out of date labels ----
			m_recentLabelTimer = new NoReentrantTimer(removeOutOfDateLabels, null, 5000, 10 * 60 * 1000);	

			// ----- ConnectedClientCollection -----
			InfiniteStorageWebSocketService.DeviceAccepted += (s, e) => { ConnectedClientCollection.Instance.Add(e.ctx); };
			InfiniteStorageWebSocketService.DeviceDisconnected += (s, e) => { ConnectedClientCollection.Instance.Remove(e.ctx); };

			m_autoUpdate = new AutoUpdate(false);


			ProgramIPC.Instance.OnWinMsg += (s, e) =>
			{
				if (e.Message == ProgramIPC.MsgShowTooltip)
					showProgramIsAtServiceBallonTips();
			};

			m_thumbnailCreator = new ThumbnailCreator();

			m_shareMonitor = new ShareLabelMonitor();

			postServiceLibrary.postServiceClass.serverBaseUrl = ProgramConfig.FromApiBase("v3");
		}

		
		void InfiniteStorageWebSocketService_PairingRequesting(object sender, WebsocketProtocol.WebsocketEventArgs e)
		{
			var subscribers = Pair.PairWebSocketService.GetAllSubscribers();

			if (!BonjourServiceRegistrator.Instance.IsAccepting || subscribers.Count == 0)
			{
				try
				{
					log4net.LogManager.GetLogger("pairing").Debug("Not in pairing mode or subscriber is not ready. Reject this connection: " + e.ctx.device_name);
					e.ctx.handleDisapprove();
				}
				catch (Exception err)
				{
					log4net.LogManager.GetLogger("pairing").Warn("Unable to reject a pairing request", err);
				}
				return;
			}

			foreach (var subscriber in subscribers)
			{
				subscriber.NewPairingRequestComing(e.ctx.device_id, e.ctx.device_name);
			}

			lock(userAcceptCS)
			{
				if (!waitForUserAccept.Contains(e.ctx))
					waitForUserAccept.Add(e.ctx);
			}
		}

		void PairWebSocketService_NewDeviceAccepting(object sender, Pair.NewDeviceRespondingEventArgs e)
		{

			log4net.LogManager.GetLogger("pairing").Debug("UI accepts " + e.device_id);
			lock (userAcceptCS)
			{
				var toRemove = new List<WebsocketProtocol.ProtocolContext>();

				var devices = waitForUserAccept.Where(x => x.device_id == e.device_id);
				foreach (var ctx in devices)
				{
					log4net.LogManager.GetLogger("pairing").Debug("call handleApprove()");
					try
					{
						ctx.handleApprove();
					}
					catch (Exception err)
					{
						log4net.LogManager.GetLogger("pairing").Warn("Unable to approve. Connection already closed? " + ctx.device_name, err);
					}
					toRemove.Add(ctx);
				}

				foreach (var ctx in toRemove)
					waitForUserAccept.Remove(ctx);
			}
		}

		void PairWebSocketService_NewDeviceRejecting(object sender, Pair.NewDeviceRespondingEventArgs e)
		{
			lock (userAcceptCS)
			{
				var toRemove = new List<WebsocketProtocol.ProtocolContext>();

				var devices = waitForUserAccept.Where(x => x.device_id == e.device_id);
				foreach (var ctx in devices)
				{
					try
					{
						ctx.handleDisapprove();
					}
					catch (Exception err)
					{
						log4net.LogManager.GetLogger("pairing").Warn("Unable to disapprove. Connection already closed? " + ctx.device_name, err);
					}
					toRemove.Add(ctx);
				}

				foreach (var ctx in toRemove)
					waitForUserAccept.Remove(ctx);
			}
		}


		void PairWebSocketService_PairingModeChanging(object sender, Pair.PairingModeChangingEventArgs e)
		{
			BonjourServiceRegistrator.Instance.Register(e.enabled);
		}

		public void Start()
		{
			m_NotifyTimer.Start();
			m_pingTimer.Start();
			m_notifier.Start();
			
			rest_server.Start();
			var backup_port = initWebsocketServer<InfiniteStorageWebSocketService>(out backup_server, 13895);
			var notify_port = initWebsocketServer<NotifyWebSocketService>(out notify_server, 13995);
			var pair_port = initWebsocketServer<Pair.PairWebSocketService>(out pair_server, 14105);

			Registry.SetValue(@"HKEY_CURRENT_USER\Software\BunnyHome", "pair_port", pair_port);

			BonjourServiceRegistrator.Instance.SetPorts(backup_port, notify_port, 14005);

			m_recentLabelTimer.Start();
			m_autoUpdate.StartLoop();
			m_ReRegBonjourTimer.Start();

			m_thumbnailCreator.Start();
			m_shareMonitor.Start();
		}


		public void Stop()
		{
			m_thumbnailCreator.Stop();

			m_NotifyTimer.Stop();
			m_pingTimer.Stop();
			m_notifier.Start();
			m_recentLabelTimer.Stop();
			m_autoUpdate.Stop();
			m_ReRegBonjourTimer.Stop();
			m_shareMonitor.Stop();
			
			backup_server.Stop();
			notify_server.Stop();
			pair_server.Stop();
		}

		private void reregisterBonjour(object nil)
		{
			try
			{
				BonjourServiceRegistrator.Instance.Register();
			}
			catch (Exception err)
			{
				log4net.LogManager.GetLogger("main").Error("Unable to register bonjour service", err);
			}
		}

		private void initNotifyIconAndController()
		{
			m_notifyIcon.Text = Resources.ProductName;
			m_notifyIcon.Icon = Resources.ProductIcon;
			m_notifyIcon.ContextMenuStrip = new ContextMenuStrip();

			var openUIItem = m_notifyIcon.ContextMenuStrip.Items.Add(Resources.TrayMenuItem_Open, null, m_notifyIconController.OnOpenUIMenuItemCliecked);
			openUIItem.Font = new Font(openUIItem.Font, FontStyle.Bold);

			m_notifyIcon.ContextMenuStrip.Items.Add(Resources.TrayMenuItem_OpenBackupFolder, null, m_notifyIconController.OnOpenPhotoBackupFolderMenuItemClicked);

			m_notifyIcon.ContextMenuStrip.Items.Add(Resources.TrayMenuItem_Preferences, null, m_notifyIconController.OnPreferencesMenuItemClicked);
			m_notifyIcon.ContextMenuStrip.Items.Add("-");

			m_notifyIcon.ContextMenuStrip.Items.Add(Resources.TrayMenuItem_Quit, null, m_notifyIconController.OnQuitMenuItemClicked);

			m_notifyIcon.Visible = true;

			showProgramIsAtServiceBallonTips();

			m_notifyIcon.DoubleClick += m_notifyIconController.OnOpenUIMenuItemCliecked;
			InfiniteStorageWebSocketService.DeviceAccepted += m_notifyIconController.OnDeviceConnected;
			InfiniteStorageWebSocketService.DeviceDisconnected += m_notifyIconController.OnDeviceDisconnected;
			InfiniteStorageWebSocketService.TotalCountUpdated += m_notifyIconController.OnTotalCountUpdated;
			InfiniteStorageWebSocketService.FileReceiving += m_notifyIconController.OnFileReceiving;
		}

		private void showProgramIsAtServiceBallonTips()
		{
			var ballonText = string.Format(Resources.BallonText_AtService, Resources.ProductName);
			m_notifyIcon.ShowBalloonTip(3000, Resources.ProductName, ballonText, ToolTipIcon.None);
		}

		private void pingPeerToCheckConnection(object nil)
		{
			try
			{
				var ping = new ConnectionStatusUpdator();
				ping.UpdateStatusToPeers();
			}
			catch (Exception err)
			{
				log4net.LogManager.GetLogger(GetType()).Warn("Exception at remove out of date labels", err);
			}
		}

		private void removeOutOfDateLabels(object nil)
		{
			try
			{
				m_autoLabel.RemoveOutOfDateAutoLabels();
			}
			catch (Exception err)
			{
				log4net.LogManager.GetLogger(GetType()).Warn("Exception at remove out of date labels", err);
			}
		}


		private static ushort initWebsocketServer<T>(out WebSocketServer<T> server, ushort preferred_port) where T : WebSocketService, new()
		{
			bool addrUsedByOthers = false;
			var port = preferred_port;

			do
			{
				try
				{

					var url = string.Format("ws://0.0.0.0:{0}/", port);
					server = new WebSocketSharp.Server.WebSocketServer<T>(url);
					server.Start();
					return port;
				}
				catch (System.Net.Sockets.SocketException e)
				{
					if (e.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse)
					{
						addrUsedByOthers = true;
						port++;
					}
					else
						throw;
				}
			}
			while (addrUsedByOthers);


			throw new Exception("should not reach here");
		}

		public void MoveFolder(string target)
		{
			if (string.IsNullOrEmpty(target))
				throw new ArgumentNullException("target");

			var source = MyFileFolder.Photo;

			if (Directory.Exists(target))
			{
				var hasContent = Directory.GetFileSystemEntries(target).Any();

				if (hasContent)
					throw new Exception(string.Format(Resources.MoveFolder_TargetAlreadyExist, target));
				else
					Directory.Delete(target);
			}

			if (Path.GetPathRoot(source) == Path.GetPathRoot(target))
			{
				Directory.Move(source, target);
			}
			else
			{
				try
				{
					DirectoryCopy(source, target, true);

					Directory.Delete(source, true);
				}
				catch
				{
					if (Directory.Exists(target))
						Directory.Delete(target, true);
					throw;
				}
			}
		}


		private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);
			DirectoryInfo[] dirs = dir.GetDirectories();

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			// If the destination directory doesn't exist, create it. 
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, false);
			}

			// If copying subdirectories, copy them and their contents to new location. 
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}
	}
}
