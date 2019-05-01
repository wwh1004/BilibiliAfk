using System.Extensions;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Bilibili.Api {
	/// <summary>
	/// 直播API
	/// </summary>
	public static class LiveApi {
		private const string DYN_ROOMS_URL = "http://room.lc4t.cn:8000/dyn_rooms/";

		/// <summary>
		/// 动态获取房间ID列表
		/// </summary>
		/// <param name="start">起始序号（闭区间）</param>
		/// <param name="end">终止序号（开区间）</param>
		/// <returns></returns>
		public static async Task<uint[]> GetRoomIdsDynamicAsync(uint start, uint end) {
			using (HttpClient client = new HttpClient())
			using (HttpResponseMessage response = await client.SendAsync(HttpMethod.Get, DYN_ROOMS_URL + start.ToString() + "-" + end.ToString()))
				return JObject.Parse(await response.Content.ReadAsStringAsync())["roomid"].ToObject<uint[]>();
		}
	}
}
