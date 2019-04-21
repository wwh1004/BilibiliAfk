using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Bilibili {
	internal static class TcpClientExtensions {
		public static Task ReceiveAsync(this TcpClient client, byte[] buffer) {
			if (client == null)
				throw new ArgumentNullException(nameof(client));
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			return client.GetStream().ReadAsync(buffer);
		}

		public static Task ReceiveAsync(this TcpClient client, byte[] buffer, int offset, int count) {
			if (client == null)
				throw new ArgumentNullException(nameof(client));
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			return client.GetStream().ReadAsync(buffer, offset, count);
		}

		public static Task SendAsync(this TcpClient client, byte[] buffer) {
			if (client == null)
				throw new ArgumentNullException(nameof(client));
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			return client.GetStream().WriteAsync(buffer);
		}

		public static Task SendAsync(this TcpClient client, byte[] buffer, int offset, int count) {
			if (client == null)
				throw new ArgumentNullException(nameof(client));
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			return client.GetStream().WriteAsync(buffer, offset, count);
		}
	}
}
