﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.IO;
using log4net;

namespace InfiniteStorage
{
	class InfiniteStorageWebSocketService: WebSocketService
	{
		private static ILog logger = LogManager.GetLogger("WebsocketService");
		private ProtocolHanlder handler;

		public InfiniteStorageWebSocketService()
		{
			// TODO: use non-hardcode data
			var userFolder = Environment.GetEnvironmentVariable("USERPROFILE");
			var appFolder = Path.Combine(userFolder, "InfiniteStorage");
			var tempFolder = Path.Combine(appFolder, "temp");
			var deviceFolder = Path.Combine(appFolder, "samsung gt-9300");

			if (!Directory.Exists(tempFolder))
				Directory.CreateDirectory(tempFolder);

			if (!Directory.Exists(deviceFolder))
				Directory.CreateDirectory(deviceFolder);

			handler = new ProtocolHanlder(new TempFileFactory(tempFolder), new FlatFileStorage(deviceFolder));
		}

		protected override void onMessage(object sender, MessageEventArgs e)
		{
			try
			{
				logger.DebugFormat("{0} bytes received from {1}", e.RawData.Length, this.ID);
				handler.HandleMessage(e);
				base.onMessage(sender, e);
			}
			catch (WebsocketProtocol.ProtocolErrorException err)
			{
				logger.Warn("Protocol error. Close connection.", err);
				Stop(WebSocketSharp.Frame.CloseStatusCode.PROTOCOL_ERROR, err.Message);
			}
			catch (Exception err)
			{
				logger.Warn("Error handing websocket data", err);
				Stop(WebSocketSharp.Frame.CloseStatusCode.SERVER_ERROR, err.Message);
			}
		}

		protected override void onOpen(object sender, EventArgs e)
		{
			base.onOpen(sender, e);
		}

		protected override void onError(object sender, WebSocketSharp.ErrorEventArgs e)
		{
			logger.Warn("Error occured: " + e.Message);
			handler.OnError();
			base.onError(sender, e);
		}

		protected override void onClose(object sender, CloseEventArgs e)
		{
			handler.Clear();
			base.onClose(sender, e);
		}
	}
}
