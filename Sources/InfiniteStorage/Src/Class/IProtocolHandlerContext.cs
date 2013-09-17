﻿#region

using InfiniteStorage.WebsocketProtocol;

#endregion

namespace InfiniteStorage
{
	public interface IProtocolHandlerContext
	{
		void handleFileStartCmd(TextCommand cmd);

		void handleFileEndCmd(TextCommand cmd);

		void handleBinaryData(byte[] data);

		void handleConnectCmd(TextCommand cmd);

		void Clear();

		void handleUpdateCountCmd(TextCommand textCommand);

		bool IsClosed { get; set; }

		void handleThumbStartCmd(TextCommand cmd);

		void handleThumbEndCmd(TextCommand cmd);
	}
}