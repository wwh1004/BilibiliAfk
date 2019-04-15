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
				client.Client.Send(PackData(7, string.Format("{{\"roomid\":{0},\"uid\":{1}}}", roomId, _random.Next())));
			}
			catch (Exception ex) {
				throw new ApiException(ex);
			}
		}

		/// <summary>
		/// 解析弹幕
		/// </summary>
		/// <param name="data">原始的二进制格式弹幕</param>
		/// <returns></returns>
		public static Danmu ResolveDanmu(byte[] data) {
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			throw new NotImplementedException();
		}

		private static byte[] PackData(int option, string body) {
			if (body == null)
				throw new ArgumentNullException(nameof(body));

			byte[] dataBody;
			byte[] data;

			dataBody = Encoding.UTF8.GetBytes(body);
			data = new byte[dataBody.Length + 16];
			using (MemoryStream stream = new MemoryStream(data)) {
				stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length)), 0, 4);
				// data length
				stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)16)), 0, 2);
				// header length
				stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)1)), 0, 2);
				// version
				stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(option)), 0, 4);
				// option
				stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(1)), 0, 4);
				// sequence
				if (dataBody.Length > 0)
					stream.Write(dataBody, 0, dataBody.Length);
				// body
			}
			return data;
		}
	}
}
