using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Youtuber2._0
{
    public class GetIDs
    {
        public static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static async Task<List<VideoObject>> GetVideosInPlayListAsync(string playListId)
        {
            List<VideoObject> videoObjects = new List<VideoObject>();

            var parameters = new Dictionary<string, string>
            {
                ["key"] = ConfigurationManager.AppSettings["APIKey"],
                ["playlistId"] = playListId,
                ["part"] = "snippet",
                ["maxResults"] = "50"
            };

            var baseUrl = "https://www.googleapis.com/youtube/v3/playlistItems?";
            var fullUrl = MakeUrlWithQuery(baseUrl, parameters);

            try
            {
                var result = await new HttpClient().GetStringAsync(fullUrl).ConfigureAwait(continueOnCapturedContext: false);

                if (result != null)
                {

                    dynamic jsonObject = (JObject)JsonConvert.DeserializeObject(result);

                    if (Convert.ToString(jsonObject.nextPageToken) == null)
                    {
                        foreach (var item in jsonObject.items)
                        {
                            // CHECK ITEM.SNIPPET.TITLE IF IT CONTAINS '?'
                            VideoObject videoObject = new VideoObject { Id = Convert.ToString(item.snippet.resourceId.videoId), Title = Convert.ToString(item.snippet.title) };
                            videoObjects.Add(videoObject);
                        }
                        return videoObjects;
                    }

                    string test = Convert.ToString(jsonObject.nextPageToken);

                    while (test != null)
                    {
                        foreach (var item in jsonObject.items)
                        {
                            // CHECK ITEM.SNIPPET.TITLE IF IT CONTAINS '?' 
                            VideoObject videoObject = new VideoObject { Id = Convert.ToString(item.snippet.resourceId.videoId), Title = Convert.ToString(item.snippet.title) };

                            videoObjects.Add(videoObject);
                        }

                        var nextParameters = new Dictionary<string, string>
                        {
                            ["key"] = ConfigurationManager.AppSettings["APIKey"],
                            ["playlistId"] = playListId,
                            ["pageToken"] = Convert.ToString(jsonObject.nextPageToken),
                            ["part"] = "snippet",
                            ["maxResults"] = "50"
                        };

                        fullUrl = MakeUrlWithQuery(baseUrl, nextParameters);

                        result = await new HttpClient().GetStringAsync(fullUrl);

                        jsonObject = (JObject)JsonConvert.DeserializeObject(result);

                        if (Convert.ToString(jsonObject.nextPageToken) == null)
                        {
                            foreach (var item in jsonObject.items)
                            {
                                VideoObject videoObject = new VideoObject { Id = Convert.ToString(item.snippet.resourceId.videoId), Title = Convert.ToString(item.snippet.title) };
                                videoObjects.Add(videoObject);
                            }
                            return videoObjects;
                        }

                        test = Convert.ToString(jsonObject.nextPageToken);
                    }

                    return videoObjects;
                }

            }
            catch (Exception ex)
            {
                _log.Error("Error during retrieving of the video IDs: " + ex.Message);
                throw;
            }
            return default(List<VideoObject>);
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
