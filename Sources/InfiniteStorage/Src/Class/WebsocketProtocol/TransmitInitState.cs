﻿using InfiniteStorage.Model;
using System;
using System.Diagnostics;

namespace InfiniteStorage.WebsocketProtocol
{
	public interface IFileUtility
	{
		bool HasDuplicateFile(FileContext file, string device_id);
	}


	public class TransmitInitState : AbstractProtocolState
	{
		private IFileUtility util;

		public TransmitInitState()
		{
			this.util = new TransmitUtility();
		}

		public TransmitInitState(IFileUtility util)
		{
			this.util = util;
		}

		public override void handleFileStartCmd(ProtocolContext ctx, TextCommand cmd)
		{
			if (string.IsNullOrEmpty(cmd.type))
				throw new ProtocolErrorException("missing fied: type");
			if (string.IsNullOrEmpty(cmd.file_name))
				throw new ProtocolErrorException("missing fied: file_name");
			if (string.IsNullOrEmpty(cmd.folder))
				throw new ProtocolErrorException("missing fied: folder");

			FileAssetType type;
			if (!Enum.TryParse<FileAssetType>(cmd.type, true, out type))
				throw new ProtocolErrorException("unknown type: " + cmd.type);

			ctx.backup_count = cmd.backuped_count;
			ctx.total_count = cmd.total_count;

			var fileCtx = new FileContext
			{
				file_name = cmd.file_name,
				file_size = cmd.file_size,
				folder = cmd.folder,

				datetime = cmd.datetime,
				type = type
			};

			var hasDup = util.HasDuplicateFile(fileCtx, ctx.device_id);

			if (hasDup)
			{
				ctx.fileCtx = null;
				ctx.raiseOnFileReceiving();
				ctx.Send(new TextCommand { action = "file-exist", file_name = cmd.file_name });
				log4net.LogManager.GetLogger("wsproto").Debug("file duplicate! send back file-exist");
			}
			else
			{
				ctx.fileCtx = fileCtx;
				ctx.raiseOnFileReceiving();
				ctx.temp_file = ctx.factory.CreateTempFile();
				ctx.Send(new TextCommand { action = "file-go", file_name = cmd.file_name });
				ctx.SetState(new TransmitStartedState());
			}
		}

		public override void handleUpdateCountCmd(ProtocolContext ctx, TextCommand cmd)
		{
			ctx.total_count = cmd.transfer_count;
			ctx.backup_count = cmd.backuped_count;
			ctx.raiseOnTotalCountUpdated();
		}
	}
}
