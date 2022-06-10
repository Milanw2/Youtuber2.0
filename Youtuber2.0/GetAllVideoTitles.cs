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
    class GetAllVideoTitles
    {
        public static async Task<dynamic> GetVideoTitlesInPlayListAsync(string playListId)
        {
            List<string> videoTitles = new List<string>();

            var parameters = new Dictionary<string, string>
            {
                ["key"] = ConfigurationManager.AppSettings["APIKey"],
                ["playlistId"] = playListId,
                ["part"] = "snippet",
                ["maxResults"] = "50"
            };

            var baseUrl = "https://www.googleapis.com/youtube/v3/playlistItems?";
            var fullUrl = MakeUrlWithQuery(baseUrl, parameters);

            var result = await new HttpClient().GetStringAsync(fullUrl).ConfigureAwait(continueOnCapturedContext: false);

            if (result != null)
            {
                //dynamic jsonObject;
                //jsonObject = (JObject)JsonConvert.DeserializeObject(result);

                playlist playlistitem = new playlist();

                playlistitem = JsonConvert.DeserializeObject<playlist>(result);

                //if (Convert.ToString(jsonObject["nextPageToken"]) == null)
                if (Convert.ToString(playlistitem.nextPageToken) == null)
                {
                    foreach (Item item in playlistitem.items)
                    {
                        //string videoTitle = Convert.ToString(item.snippet.title);
                        string videoTitle = Convert.ToString(item.snippet.title);
                        videoTitles.Add(videoTitle);
                    }
                    return videoTitles;
                }

                string test = playlistitem.nextPageToken;

                while (test != null)
                {
                    foreach (Item item in playlistitem.items)
                    {
                        //string videoTitle = Convert.ToString(item.snippet.title);
                        string videoTitle = Convert.ToString(item.snippet.title);
                        videoTitles.Add(videoTitle);
                    }

                    var nextParameters = new Dictionary<string, string>
                    {
                        ["key"] = ConfigurationManager.AppSettings["APIKey"],
                        ["playlistId"] = playListId,
                        ["pageToken"] = playlistitem.nextPageToken,
                        ["part"] = "snippet",
                        ["maxResults"] = "50"
                    };

                    fullUrl = MakeUrlWithQuery(baseUrl, nextParameters);

                    result = await new HttpClient().GetStringAsync(fullUrl);

                    playlistitem = JsonConvert.DeserializeObject<playlist>(result);

                    if (Convert.ToString(playlistitem.nextPageToken) == null)
                    {
                        foreach (Item item in playlistitem.items)
                        {
                            string videoTitle = Convert.ToString(item.snippet.title);
                            videoTitles.Add(videoTitle);
                        }
                        return videoTitles;
                    }

                    test = playlistitem.nextPageToken;
                }

                return videoTitles;
            }

            return default(dynamic);
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
