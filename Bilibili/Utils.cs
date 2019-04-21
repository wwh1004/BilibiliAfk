using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Bilibili {
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

		public static string FormToString(this IEnumerable<KeyValuePair<string, string>> values) {
			return string.Join("&", values.Select(t => t.Key + "=" + Uri.EscapeDataString(t.Value)));
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
	}
}
