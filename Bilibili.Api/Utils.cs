using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Bilibili.Api {
	internal static class Utils {
		public static void UpdateRange(this Dictionary<string, string> target, Dictionary<string, string> source) {
			foreach (KeyValuePair<string, string> item in source)
				target[item.Key] = item.Value;
		}

		public static void UpdateRange(this HttpRequestHeaders requestHeaders, IEnumerable<KeyValuePair<string, string>> headers) {
			if (requestHeaders == null)
				throw new ArgumentNullException(nameof(requestHeaders));
			if (headers == null)
				throw new ArgumentNullException(nameof(headers));

			foreach (KeyValuePair<string, string> item in headers)
				requestHeaders.TryAddWithoutValidation(item.Key, item.Value);
		}

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

				query = parameters.Join();
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

		public static string FormatJson(string json) {
			using (StringWriter writer = new StringWriter())
			using (JsonTextWriter jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
			using (StringReader reader = new StringReader(json))
			using (JsonTextReader jsonReader = new JsonTextReader(reader)) {
				jsonWriter.WriteToken(jsonReader);
				return writer.ToString();
			}
		}

		public static string ToFullString(this Exception exception) {
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			StringBuilder sb;

			sb = new StringBuilder();
			DumpException(exception, sb);
			return sb.ToString();
		}

		private static void DumpException(Exception exception, StringBuilder sb) {
			sb.AppendLine("Type: " + Environment.NewLine + exception.GetType().FullName);
			sb.AppendLine("Message: " + Environment.NewLine + exception.Message);
			sb.AppendLine("Source: " + Environment.NewLine + exception.Source);
			sb.AppendLine("StackTrace: " + Environment.NewLine + exception.StackTrace);
			sb.AppendLine("TargetSite: " + Environment.NewLine + exception.TargetSite.ToString());
			sb.AppendLine("----------------------------------------");
			if (exception.InnerException != null)
				DumpException(exception.InnerException, sb);
		}
	}
}
