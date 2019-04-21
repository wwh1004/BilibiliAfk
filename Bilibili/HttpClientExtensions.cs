using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Bilibili {
	internal static class HttpClientExtensions {
		public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string url) {
			return client.SendAsync(method, url, null, null);
		}

		public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string url, IEnumerable<KeyValuePair<string, string>> parameters, IEnumerable<KeyValuePair<string, string>> headers) {
			return client.SendAsync(method, url, parameters, headers, (byte[])null, "application/x-www-form-urlencoded");
		}

		public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string url, IEnumerable<KeyValuePair<string, string>> parameters, IEnumerable<KeyValuePair<string, string>> headers, string content, string contentType) {
			return client.SendAsync(method, url, parameters, headers, content == null ? null : Encoding.UTF8.GetBytes(content), contentType);
		}

		public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string url, IEnumerable<KeyValuePair<string, string>> parameters, IEnumerable<KeyValuePair<string, string>> headers, byte[] content, string contentType) {
			if (client == null)
				throw new ArgumentNullException(nameof(client));
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException(nameof(url));
			if (string.IsNullOrEmpty(contentType))
				throw new ArgumentNullException(nameof(contentType));
			if (method != HttpMethod.Get && parameters != null && content != null)
				throw new NotSupportedException();

			UriBuilder uriBuilder;
			HttpRequestMessage request;

			uriBuilder = new UriBuilder(url);
			if (parameters != null && method == HttpMethod.Get) {
				string query;

				query = parameters.FormToString();
				if (!string.IsNullOrEmpty(query))
					if (string.IsNullOrEmpty(uriBuilder.Query))
						uriBuilder.Query = query;
					else
						uriBuilder.Query += "&" + query;
			}
			request = new HttpRequestMessage(method, uriBuilder.Uri);
			if (content != null)
				request.Content = new ByteArrayContent(content);
			else if (parameters != null && method != HttpMethod.Get)
				request.Content = new FormUrlEncodedContent(parameters);
			if (request.Content != null)
				request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
			if (headers != null)
				request.Headers.UpdateRange(headers);
			return client.SendAsync(request);
		}

	}
}
