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
using System.Text.RegularExpressions;
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

                    Playlist playlistitem = new Playlist();
                    playlistitem = JsonConvert.DeserializeObject<Playlist>(result);

                    if (Convert.ToString(playlistitem.NextPageToken) == null)
                    {
                        foreach (var item in playlistitem.Items)
                        {
                            // CHECK ITEM.SNIPPET.TITLE IF IT CONTAINS '?'
                            VideoObject videoObject = new VideoObject { Id = Convert.ToString(item.Snippet.ResourceId.VideoId), Title = Convert.ToString(item.Snippet.Title) };
                            videoObjects.Add(videoObject);
                        }
                        return videoObjects;
                    }

                    string test = Convert.ToString(playlistitem.NextPageToken);

                    while (test != null)
                    {
                        foreach (var item in playlistitem.Items)
                        {
                            // CHECK ITEM.SNIPPET.TITLE IF IT CONTAINS '?' 
                            VideoObject videoObject = new VideoObject { Id = Convert.ToString(item.Snippet.ResourceId.VideoId), Title = Convert.ToString(item.Snippet.Title) };

                            videoObjects.Add(videoObject);
                        }

                        var nextParameters = new Dictionary<string, string>
                        {
                            ["key"] = ConfigurationManager.AppSettings["APIKey"],
                            ["playlistId"] = playListId,
                            ["pageToken"] = Convert.ToString(playlistitem.NextPageToken),
                            ["part"] = "snippet",
                            ["maxResults"] = "50"
                        };

                        fullUrl = MakeUrlWithQuery(baseUrl, nextParameters);

                        result = await new HttpClient().GetStringAsync(fullUrl);

                        playlistitem = JsonConvert.DeserializeObject<Playlist>(result);

                        if (Convert.ToString(playlistitem.NextPageToken) == null)
                        {
                            foreach (var item in playlistitem.Items)
                            {
                                VideoObject videoObject = new VideoObject { Id = Convert.ToString(item.Snippet.ResourceId.VideoId), Title = Convert.ToString(item.Snippet.Title) };
                                videoObjects.Add(videoObject);
                            }
                            return videoObjects;
                        }

                        test = Convert.ToString(playlistitem.NextPageToken);
                    }

                    return videoObjects;
                }

            }
            catch (Exception ex)
            {
                _log.Error("Error during retrieving of the video IDs: " + ex.Message);
                Console.WriteLine("Error during retrieving of the video IDs: " + ex.Message);
            }
            return default;
        }

        private static string MakeUrlWithQuery(string baseUrl, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            if (parameters == null || parameters.Count() == 0)
                return baseUrl;

            return Regex.Replace(parameters.Aggregate(baseUrl,
                (accumulated, kvp) => string.Format($"{accumulated}{kvp.Key}={kvp.Value}&")), @"\s+", "");
        }
    }
}
