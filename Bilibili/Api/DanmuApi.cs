using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bilibili.Api {
	/// <summary>
	/// 弹幕API
	/// </summary>
	public static class DanmuApi {
		private const string DANMU_HOST_NAME = "broadcastlv.chat.bilibili.com";
		private const int DANMU_HOST_PORT = 2243;

		private static readonly Random _random = new Random();
		private static readonly byte[] _heartBeatPacket = Pack(DanmuType.HeartBeat, Array.Empty<byte>());

		/// <summary>
		/// 连接弹幕服务器
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public static void Connect(TcpClient client) {
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			try {
				client.Connect(DANMU_HOST_NAME, DANMU_HOST_PORT);
			}
			catch (Exception ex) {
				throw new ApiException(ex);
			}
		}

		/// <summary>
		/// 进入指定房间
		/// </summary>
		/// <param name="client"></param>
		/// <param name="roomId">房间ID</param>
		/// <returns></returns>
		public static void EnterRoom(TcpClient client, uint roomId) {
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			try {
				client.Client.Send(Pack(DanmuType.EnterRoom, string.Format("{{\"roomid\":{0},\"uid\":{1}}}", roomId, _random.Next())));
			}
			catch (Exception ex) {
				throw new ApiException(ex);
			}
		}

		/// <summary>
		/// 发送心跳
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public static void SendHeartBeat(TcpClient client) {
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			try {
				client.Client.Send(_heartBeatPacket);
			}
			catch (Exception ex) {
				throw new ApiException(ex);
			}
		}

		/// <summary>
		/// 解析弹幕
		/// </summary>
		/// <param name="socket"></param>
		/// <returns></returns>
		public static unsafe Danmu ResolveDanmu(Socket socket) {
			if (socket == null)
				throw new ArgumentNullException(nameof(socket));

			byte[] buffer;
			DanmuType action;

			buffer = new byte[16];
			socket.Receive(buffer);
			// receive header
			fixed (byte* p = buffer) {
				int value;

				value = IPAddress.NetworkToHostOrder(*(int*)p);
				// packet length
				buffer = new byte[value - 16];
#pragma warning disable IDE0059
				value = IPAddress.NetworkToHostOrder(*(short*)(p + 4));
				// header length
				value = IPAddress.NetworkToHostOrder(*(short*)(p + 6));
				// version
				value = IPAddress.NetworkToHostOrder(*(int*)(p + 8));
				// action
				action = (DanmuType)value;
				value = IPAddress.NetworkToHostOrder(*(int*)(p + 12));
				// magic
#pragma warning restore IDE0059
			}
			if (buffer.Length != 0)
				// 握手确认这样的消息没有payload
				socket.Receive(buffer);
			// receive payload
			return ResolveDanmu(action, buffer);
		}

		/// <summary>
		/// 解析弹幕
		/// </summary>
		/// <param name="action"></param>
		/// <param name="payload">原始的二进制格式弹幕</param>
		/// <returns></returns>
		public static Danmu ResolveDanmu(DanmuType action, byte[] payload) {
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));

			switch (action) {
			case DanmuType.Command:
			case DanmuType.Handshaking:
				return new Danmu(action, payload);
			default:
				return Danmu.Empty;
			}
		}

		private static byte[] Pack(DanmuType action, string payload) {
			return Pack(action, payload == null ? null : Encoding.UTF8.GetBytes(payload));
		}

		private static byte[] Pack(DanmuType action, byte[] payload) {
			byte[] packet;

			if (payload == null)
				payload = Array.Empty<byte>();
			packet = new byte[payload.Length + 16];
			using (MemoryStream stream = new MemoryStream(packet)) {
				stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packet.Length)), 0, 4);
				// packet length
				stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)16)), 0, 2);
				// header length
				stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)1)), 0, 2);
				// version
				stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)action)), 0, 4);
				// action
				stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(1)), 0, 4);
				// magic
				if (payload.Length > 0)
					stream.Write(payload, 0, payload.Length);
				// payload
			}
			return packet;
		}
	}
}
