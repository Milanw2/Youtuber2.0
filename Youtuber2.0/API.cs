using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VideoLibrary;

namespace Youtuber2._0
{
    class API
    {
        static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Boolean ProcessVideosParallel(List<VideoObject> allVideoIds, string pathMp3Files, Boolean fileDownloaded, string pathVideoFiles)
        {
            try
            {
                Parallel.ForEach(allVideoIds, (videoObject) =>
                {
                    Stopwatch stopWatchFile = new Stopwatch();
                    stopWatchFile.Start();

                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Processing: " + videoObject.Title);

                    var youtube = YouTube.Default;

                    try
                    {
                        // Get all different videos
                        var videos = YouTube.Default.GetAllVideos("http://www.youtube.com/watch?v=" + videoObject.Id);

                        // Create object to store highest quality
                        VideoLibrary.YouTubeVideo videoHighRes = null;
                        int maxAudioBitrate = 0;

                        foreach (var video in videos)
                        {
                            if (video.AudioBitrate > maxAudioBitrate && (video.FileExtension == ".mp4" || video.FileExtension == ".webm"))
                            {
                                maxAudioBitrate = video.AudioBitrate;
                                videoHighRes = video;
                            }
                        }
                        _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Audio bitrate = " + videoHighRes.AudioBitrate + " => " + videoHighRes.FullName);

                        // Write video to file if mp3 version doesn't exist yet 
                        if (!File.Exists(pathMp3Files + videoHighRes.FullName.Replace(".webm", ".mp3").Replace(".mp4", ".mp3")))
                        {
                            fileDownloaded = true;
                            byte[] content = null;
                            for (int attempts = 0; attempts < 5; attempts++)
                            // if you really want to keep going until it works, use   for(;;)
                            {
                                try
                                {
                                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Attempt number : " + attempts + " of file " + videoHighRes.Title);
                                    content = videoHighRes.GetBytes();
                                    break;
                                }
                                catch (Exception x)
                                {
                                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error in retry " + attempts + " with message : " + x.Message);
                                }
                                System.Threading.Thread.Sleep(1000); // Possibly a good idea to pause here
                            }

                            if (content != null)
                            {
                                _log.Debug($"Thread: { Thread.CurrentThread.ManagedThreadId} => Retrieved video data");
                                System.IO.File.WriteAllBytes(pathVideoFiles + videoHighRes.FullName, content);
                                _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Wrote file to disk = " + pathVideoFiles + videoHighRes.FullName);
                            }
                            else
                            {
                                _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error with video : " + videoObject.Title);
                                throw new System.ArgumentException($"Thread : {Thread.CurrentThread.ManagedThreadId} => Something went wrong when retrieving the video!", "See logging for more info");
                            }
                        }
                        else
                        {
                            _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => File already exists.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video = " + ex.Message);
                    }
                    stopWatchFile.Stop();
                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => File downloaded in : " + stopWatchFile.Elapsed + " seconds.");
                    _log.Info($"Thread : {Thread.CurrentThread.ManagedThreadId} => Downloaded : " + videoObject.Title);
                });
            }
            catch (Exception e)
            {
                throw e;
            }

            return fileDownloaded;
        }

        public Boolean ProcessVideos(List<VideoObject> allVideoIds, string pathMp3Files, Boolean fileDownloaded, string pathVideoFiles)
        {
            try
            {
                foreach (VideoObject videoObject in allVideoIds)
                {
                    Stopwatch stopWatchFile = new Stopwatch();
                    stopWatchFile.Start();

                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Processing: " + videoObject.Title);

                    var youtube = YouTube.Default;

                    try
                    {
                        // Get all different videos
                        var videos = YouTube.Default.GetAllVideos("http://www.youtube.com/watch?v=" + videoObject.Id);

                        // Create object to store highest quality
                        VideoLibrary.YouTubeVideo videoHighRes = null;
                        int maxAudioBitrate = 0;

                        foreach (var video in videos)
                        {
                            if (video.AudioBitrate > maxAudioBitrate && (video.FileExtension == ".mp4" || video.FileExtension == ".webm"))
                            {
                                maxAudioBitrate = video.AudioBitrate;
                                videoHighRes = video;
                            }
                        }
                        _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Audio bitrate = " + videoHighRes.AudioBitrate + " => " + videoHighRes.FullName);

                        // Write video to file if mp3 version doesn't exist yet 
                        if (!File.Exists(pathMp3Files + videoHighRes.FullName.Replace(".webm", ".mp3").Replace(".mp4", ".mp3")))
                        {
                            fileDownloaded = true;
                            byte[] content = null;
                            for (int attempts = 0; attempts < 5; attempts++)
                            // if you really want to keep going until it works, use   for(;;)
                            {
                                try
                                {
                                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Attempt number : " + attempts + " of file " + videoHighRes.Title);
                                    content = videoHighRes.GetBytes();
                                    break;
                                }
                                catch (Exception x)
                                {
                                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error in retry " + attempts + " with message : " + x.Message);
                                }
                                System.Threading.Thread.Sleep(1000); // Possibly a good idea to pause here
                            }

                            if (content != null)
                            {
                                _log.Debug($"Thread: { Thread.CurrentThread.ManagedThreadId} => Retrieved video data");
                                System.IO.File.WriteAllBytes(pathVideoFiles + videoHighRes.FullName, content);
                                _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Wrote file to disk = " + pathVideoFiles + videoHighRes.FullName);
                            }
                            else
                            {
                                _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error with video : " + videoObject.Title);
                                throw new System.ArgumentException($"Thread : {Thread.CurrentThread.ManagedThreadId} => Something went wrong when retrieving the video!", "See logging for more info");
                            }
                        }
                        else
                        {
                            _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => File already exists.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video : " + videoObject.Title + " with error message: " + ex.Message);
                    }
                    stopWatchFile.Stop();
                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => File downloaded in : " + stopWatchFile.Elapsed + " seconds.");
                    _log.Info($"Thread : {Thread.CurrentThread.ManagedThreadId} => Downloaded : " + videoObject.Title);
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return fileDownloaded;
        }
    }
}
