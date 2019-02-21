using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Youtuber2._0
{
    class GetPlaylistName
    {
        public static async Task<dynamic> GetPlaylistNameAsync(string playListId)
        {
            string playlistName = "";

            var parameters = new Dictionary<string, string>
            {
                ["key"] = ConfigurationManager.AppSettings["APIKey"],
                ["id"] = playListId,
                ["part"] = "snippet",
                ["maxResults"] = "50"
            };

            var baseUrl = "https://www.googleapis.com/youtube/v3/playlists?";
            var fullUrl = MakeUrlWithQuery(baseUrl, parameters);

            var result = await new HttpClient().GetStringAsync(fullUrl).ConfigureAwait(continueOnCapturedContext: false);

            dynamic jsonObject = (JObject)JsonConvert.DeserializeObject(result);

            if (Convert.ToString(jsonObject.nextPageToken) == null)
            {
                foreach (var item in jsonObject.items)
                {
                    playlistName = Convert.ToString(item.snippet.title);
                }
            }

            return playlistName;
        }


        private static string MakeUrlWithQuery(string baseUrl,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            if (parameters == null || parameters.Count() == 0)
                return baseUrl;

            return parameters.Aggregate(baseUrl,
                (accumulated, kvp) => string.Format($"{accumulated}{kvp.Key}={kvp.Value}&"));
        }
    }
}
