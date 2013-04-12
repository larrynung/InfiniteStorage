﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace InfiniteStorage.WebsocketProtocol
{
	class TransmitStartedState : IStateTransmit
	{
		public void handleBinaryData(TransmitContext ctx, byte[] data)
		{
			ctx.temp_file.Write(data);
			log4net.LogManager.GetLogger("wsproto").DebugFormat("file content of {0}: {1} bytes", ctx.fileCtx.file_name, data.Length);
		}

		public void handleFileStartCmd(TransmitContext ctx, TextCommand cmd)
		{
			throw new ProtocolErrorException("file-start cmd is not expected in transmit-started state");
		}

		public void handleFileEndCmd(TransmitContext ctx, TextCommand cmd)
		{
			ctx.temp_file.EndWrite();

			try
			{
				ctx.storage.MoveToStorage(ctx.temp_file.Path, ctx.fileCtx);
			}
			catch (Exception e)
			{
				throw new IOException("Unable to move temp file to storage. temp_file:" + ctx.temp_file.Path + ", file_name: " + ctx.fileCtx.file_name, e);
			}

			if (ctx.fileCtx.file_size != ctx.temp_file.BytesWritten)
				log4net.LogManager.GetLogger(typeof(TransmitStartedState)).WarnFormat("{0} is expected to have {1} bytes but {2} bytes received.", ctx.fileCtx.file_name, ctx.fileCtx.file_size, ctx.temp_file.BytesWritten);

			log4net.LogManager.GetLogger("wsproto").Debug("file-end: " + ctx.fileCtx.file_name);

			ctx.SetState(new TransmitInitState());
		}
	}
}
