﻿using InfiniteStorage.Model;
using System;
using System.IO;

namespace InfiniteStorage.WebsocketProtocol
{
	public class TransmitStartedState : AbstractProtocolState
	{
		public ITransmitStateUtility Util { get; set; }

		public TransmitStartedState()
		{
			Util = new TransmitUtility();
		}

		public override void handleBinaryData(ProtocolContext ctx, byte[] data)
		{
			ctx.temp_file.Write(data);
			log4net.LogManager.GetLogger("wsproto").DebugFormat("file content of {0}: {1} bytes", ctx.fileCtx.file_name, data.Length);
			ctx.raiseOnFileProgress();
		}

		public override void handleFileEndCmd(ProtocolContext ctx, TextCommand cmd)
		{
			ctx.temp_file.EndWrite();

			ctx.raiseOnFileEnding();

			if (!Util.HasDuplicateFile(ctx.fileCtx, ctx.device_id))
			{
				string saved;
				try
				{
					saved = ctx.storage.MoveToStorage(ctx.temp_file.Path, ctx.fileCtx);
				}
				catch (Exception e)
				{
					throw new IOException("Unable to move temp file to storage. temp_file:" + ctx.temp_file.Path + ", file_name: " + ctx.fileCtx.file_name, e);
				}

				var partial_path = PathUtil.MakeRelative(saved, MyFileFolder.Photo);

				var fileAsset = new FileAsset
				{
					device_id = ctx.device_id,
					event_time = ctx.fileCtx.datetime,
					file_id = Guid.NewGuid(),
					file_name = ctx.fileCtx.file_name,
					file_path = Path.Combine(ctx.fileCtx.folder, ctx.fileCtx.file_name),
					file_size = ctx.fileCtx.file_size,
					type = (int)ctx.fileCtx.type,
					saved_path = partial_path,
					parent_folder = Path.GetDirectoryName(partial_path),
					seq = Util.GetNextSeq(), 
				};
				Util.SaveFileRecord(fileAsset);


				if (ctx.fileCtx.file_size != ctx.temp_file.BytesWritten)
					log4net.LogManager.GetLogger(typeof(TransmitStartedState)).WarnFormat("{0} is expected to have {1} bytes but {2} bytes received.", ctx.fileCtx.file_name, ctx.fileCtx.file_size, ctx.temp_file.BytesWritten);

				ctx.fileCtx.file_id = fileAsset.file_id;
				ctx.raiseOnFileReceived();
			}
			else
			{
				ctx.temp_file.Delete();
			}

			log4net.LogManager.GetLogger("wsproto").Debug("send back file-exist for file recv success");
			ctx.Send(new TextCommand { action = "file-exist", file_name = ctx.fileCtx.file_name });

			ctx.recved_files++;
			ctx.SetState(new TransmitInitState());
		}
	}
}
