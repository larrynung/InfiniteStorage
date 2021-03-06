﻿using InfiniteStorage.WebsocketProtocol;
using System;
using WebSocketSharp;

namespace InfiniteStorage
{
	public class ProtocolHanlder
	{
		public IProtocolHandlerContext ctx { get; private set; }

		public ProtocolHanlder(ITempFileFactory tempfileFactory, IFileStorage storage, AbstractProtocolState initialState)
		{
			this.ctx = new ProtocolContext(tempfileFactory, storage, initialState);
		}

		public ProtocolHanlder(IProtocolHandlerContext ctx)
		{
			this.ctx = ctx;
		}

		public void HandleMessage(MessageEventArgs e)
		{
			try
			{
				if (e.Type == WebSocketSharp.Frame.Opcode.TEXT)
				{
					log4net.LogManager.GetLogger("wsproto").Debug(e.Data);

					TextCommand cmd = parseTextCommand(e);

					if (cmd.isFileStartCmd())
						ctx.handleFileStartCmd(cmd);
					else if (cmd.isFileEndCmd())
						ctx.handleFileEndCmd(cmd);
					else if (cmd.isConnectCmd())
						ctx.handleConnectCmd(cmd);
					else if (cmd.isUpdatecountCmd())
						ctx.handleUpdateCountCmd(cmd);
					else
						throw new ProtocolErrorException("Unknown action: " + cmd.action);
				}
				else if (e.Type == WebSocketSharp.Frame.Opcode.BINARY)
				{
					ctx.handleBinaryData(e.RawData);
				}
			}
			catch
			{
				if (!ctx.IsClosed)
				{
					Clear();
					throw;
				}
			}
		}

		private static TextCommand parseTextCommand(MessageEventArgs e)
		{
			try
			{
				return Newtonsoft.Json.JsonConvert.DeserializeObject<WebsocketProtocol.TextCommand>(e.Data);
			}
			catch (Exception err)
			{
				throw new ProtocolErrorException("Invalid text command: " + e.Data, err);
			}
		}

		public void Clear()
		{
			if (ctx != null)
			{
				ctx.Clear();
			}
		}

	}
}
