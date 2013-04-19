﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InfiniteStorage;
using Moq;
using WebSocketSharp;
using Newtonsoft.Json;
using InfiniteStorage.WebsocketProtocol;

namespace UnitTest
{
	[TestClass]
	public class testProtocolHandler
	{
		Mock<IFileStorage> storage;

		[TestInitialize]
		public void setup()
		{
			storage = new Mock<IFileStorage>();
		}

		[TestMethod]
		[ExpectedException(typeof(ProtocolErrorException))]
		public void testUnknownTxtCmd()
		{
			var tempFactory = new Mock<ITempFileFactory>();
			var protoHdler = new ProtocolHanlder(tempFactory.Object, storage.Object, new TransmitInitState());

			//
			// Unknown command XXXXX
			// 
			var fileStart = new { action = "XXXXX", file_name = "file1.jpg", file_size = 20 };
			protoHdler.HandleMessage(new MessageEventArgs(JsonConvert.SerializeObject(fileStart)));

		}

		[TestMethod]
		[ExpectedException(typeof(ProtocolErrorException))]
		public void testRecvFileStartTwice()
		{
			var tempFile = new Mock<ITempFile>();

			var tempFactory = new Mock<ITempFileFactory>();
			tempFactory.Setup(x => x.CreateTempFile()).Returns(tempFile.Object);

			var protoHdler = new ProtocolHanlder(tempFactory.Object, storage.Object, new TransmitInitState());


			//
			// file-start
			// 
			var fileStart = new { action = "file-start", file_name = "file1.jpg", file_size = 20, type = "image" };
			protoHdler.HandleMessage(new MessageEventArgs(JsonConvert.SerializeObject(fileStart)));
			protoHdler.HandleMessage(new MessageEventArgs(JsonConvert.SerializeObject(fileStart)));
		}

		[TestMethod]
		[ExpectedException(typeof(ProtocolErrorException))]
		public void testInvalidTextCommand()
		{
			var tempFactory = new Mock<ITempFileFactory>();
			var protoHdler = new ProtocolHanlder(tempFactory.Object, storage.Object, new TransmitInitState());

			//
			// Unknown command XXXXX
			// 
			//var fileStart = new { action = "XXXXX", file_name = "file1.jpg", file_size = 20 };
			protoHdler.HandleMessage(new MessageEventArgs("1234567899"));

		}

		[TestMethod]
		public void tempFileIsDeletedIfErrorHappens()
		{
			var tempFile = new Mock<ITempFile>();
			tempFile.Setup(x => x.Delete()).Verifiable();

			var tempFactory = new Mock<ITempFileFactory>();
			tempFactory.Setup(x => x.CreateTempFile()).Returns(tempFile.Object).Verifiable();

			var protoHdler = new ProtocolHanlder(tempFactory.Object, storage.Object, new TransmitInitState());


			//
			// file-start
			// 
			var fileStart = new { action = "file-start", file_name = "file1.jpg", file_size = 20, type = "image", folder = "ff" };
			protoHdler.HandleMessage(new MessageEventArgs(JsonConvert.SerializeObject(fileStart)));

			// file-start again !!! Error
			try
			{
				protoHdler.HandleMessage(new MessageEventArgs(JsonConvert.SerializeObject(fileStart)));

				Assert.Fail("ProtocolErrorException is expected but is not thrown");
			}
			catch (ProtocolErrorException)
			{
				// ======= verify =======
				tempFile.VerifyAll();
			}
		}

		[TestMethod]
		public void protocolHandlerSendConnectMsgToProperHandler()
		{
			var connectMSg =new TextCommand{ action = "connect", device_name = "de", device_id = "id"};


			var ctx = new Mock<IProtocolHandlerContext>();
			ctx.Setup(x=>x.handleConnectCmd(It.Is<TextCommand>( cmd => cmd.action == "connect" && cmd.device_id == "id" && cmd.device_name == "de"))).Verifiable();
			var protoHdr = new ProtocolHanlder(ctx.Object);

			protoHdr.HandleMessage(new MessageEventArgs(JsonConvert.SerializeObject(connectMSg)));

			ctx.VerifyAll();

		}

		[TestMethod]
		public void unconnected_connect_conntected()
		{
			var connectMsg = new TextCommand { action = "connect", device_name = "dev", device_id = "guid1" };

			var tempFactory = new Mock<ITempFileFactory>();
			var fileStorage = new Mock<IFileStorage>();
			var connectHandler = new Mock<IConnectMsgHandler>();
			var retState = new Mock<AbstractProtocolState>();

			connectHandler.Setup(x => x.HandleConnectMsg(connectMsg, It.IsAny<ProtocolContext>())).Returns(retState.Object).Verifiable();

			var initState = new UnconnectedState();
			initState.handler = connectHandler.Object;
			var ctx = new ProtocolContext(tempFactory.Object, storage.Object, initState);

			

			ctx.handleConnectCmd(connectMsg);

			connectHandler.VerifyAll();
			Assert.AreEqual(retState.Object, ctx.GetState());
			Assert.AreEqual("dev", ctx.device_name);
			Assert.AreEqual("guid1", ctx.device_id);

		}

	}
}
