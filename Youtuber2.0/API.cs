using CSCore;
using CSCore.MediaFoundation;
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
using System.Windows;
using VideoLibrary;

namespace Youtuber2._0
{
    class API
    {
        static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        string GlobalMp3Path = "";

        public static Boolean SendErrors { get; set; }
        public static Boolean SendInfo { get; set; }

        public void Init()
        {

        }

        public void ProcessVideosToMp3(List<VideoObject> allVideoIds, string pathMp3Files)
        {
            var events = new List<ManualResetEvent>();

            _log.Debug("allVideoIds.Count : " + allVideoIds.Count);
            Console.WriteLine("allVideoIds.Count : " + allVideoIds.Count);
            int numberOfTasks = allVideoIds.Count;

            try
            {
                Parallel.ForEach(allVideoIds, videoObject =>
                {
                    try
                    {

                        Stopwatch stopWatchFile = new Stopwatch();
                        stopWatchFile.Start();

                        _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Processing: " + videoObject.Title);
                        Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Processing: " + videoObject.Title);

                        var youtube = YouTube.Default;

                        string filename = string.Join("_", videoObject.Title.Split(Path.GetInvalidFileNameChars())) + " - YouTube.webm";

                        try
                        {
                            // Get all different videos
                            var videos = youtube.GetAllVideos("https://www.youtube.com/watch?v=" + videoObject.Id);

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
                            _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Audio bitrate = " + videoHighRes.AudioBitrate + " => " + filename);
                            Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Audio bitrate = " + videoHighRes.AudioBitrate + " => " + filename);

                            // Write video to file if mp3 version doesn't exist yet 
                            if (!File.Exists(pathMp3Files + filename + ".mp3"))
                            {
                                string test = pathMp3Files + filename + ".mp3";
                                for (int attempts = 0; attempts < 5; attempts++)
                                // if you really want to keep going until it works, use   for(;;)
                                {
                                    try
                                    {
                                        _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Attempt number : " + attempts + " of file " + filename);
                                        Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Attempt number : " + attempts + " of file " + filename);
                                        GlobalMp3Path = pathMp3Files;
                                        try
                                        {
                                            IWaveSource videoSource = CSCore.Codecs.CodecFactory.Instance.GetCodec(new Uri(videoHighRes.Uri));
                                            Tuple<IWaveSource, String> package = Tuple.Create(videoSource, filename);

                                            ThreadedConvertToMp3(package);
                                        }
                                        catch (Exception ex)
                                        {
                                            if (File.Exists(GlobalMp3Path + filename + ".mp3"))
                                            {
                                                File.Delete(GlobalMp3Path + filename + ".mp3");
                                            }
                                            _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video : " + filename + " with error message: " + ex.Message);
                                            Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video : " + filename + " with error message: " + ex.Message);

                                        }
                                        break;
                                    }
                                    catch (Exception x)
                                    {
                                        _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error in retry " + attempts + " with message : " + x.Message);
                                        Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error in retry " + attempts + " with message : " + x.Message);
                                    }
                                    System.Threading.Thread.Sleep(1000); // Possibly a good idea to pause here
                                }
                            }
                            else
                            {
                                _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => File already exists.");
                                Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => File already exists.");
                            }
                        }
                        catch (NullReferenceException e)
                        {
                            _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video : " + videoObject.Title + " with error message: " + e.Message);
                            Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video : " + videoObject.Title + " with error message: " + e.Message);
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video : " + videoObject.Title + " with error message: " + ex.Message);
                            Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video : " + videoObject.Title + " with error message: " + ex.Message);
                            throw ex;
                        }
                        stopWatchFile.Stop();
                        _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => File downloaded in : " + stopWatchFile.Elapsed + " seconds.");
                        Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => File downloaded in : " + stopWatchFile.Elapsed + " seconds.");
                        _log.Info($"Thread : {Thread.CurrentThread.ManagedThreadId} => Downloaded : " + videoObject.Title);
                        Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Downloaded : " + videoObject.Title);
                        _log.Debug("numberOfTasks : " + numberOfTasks);
                        Console.WriteLine("numberOfTasks : " + numberOfTasks);

                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during paralle foreach with error message: " + ex.Message);
                        Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during paralle foreach with error message: " + ex.Message);
                        var values = new[] { "niet beschikbaar", "unavailable" };
                    }
                });

                System.Media.SystemSounds.Beep.Play();

            }
            catch (AggregateException err)
            {
                foreach (var errInner in err.InnerExceptions)
                {
                    Debug.WriteLine(errInner); //this will call ToString() on the inner execption and get you message, stacktrace and you could perhaps drill down further into the inner exception of it if necessary 
                    _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error message: " + errInner);
                    Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error message: " + errInner);
                }
                throw err;
            }
            catch (Exception e)
            {
                _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error message: " + e.Message);
                Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error message: " + e.Message);
                throw e;
            }
        }

        private void ThreadedConvertToMp3(object callback)
        {
            String videoTitle = "empty";
            try
            {
                IWaveSource source = ((Tuple<IWaveSource, String>)callback).Item1;
                videoTitle = ((Tuple<IWaveSource, String>)callback).Item2;
                ConvertToMp3(source, videoTitle);
            }
            catch (NullReferenceException e)
            {
                _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error in other thread (ConvertToMp3) videoTitle : " + videoTitle + " with error message: " + e.Message);
                Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error in other thread (ConvertToMp3) videoTitle : " + videoTitle + " with error message: " + e.Message);
            }
            catch (Exception e)
            {
                _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error in other thread (ConvertToMp3) videoTitle : " + videoTitle + " with error message: " + e.Message);
                Console.WriteLine($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error in other thread (ConvertToMp3) videoTitle : " + videoTitle + " with error message: " + e.Message);
            }
        }

        private bool ConvertToMp3(IWaveSource source, string videoTitle)
        {
            var supportedFormats = MediaFoundationEncoder.GetEncoderMediaTypes(AudioSubTypes.MpegLayer3);
            if (!supportedFormats.Any())
            {
                Console.WriteLine("The current platform does not support mp3 encoding.");
                return true;
            }

            if (source.WaveFormat == null)
            {
                return true;
            }
            else
            {
                if (supportedFormats.All(
                    x => x.SampleRate != source.WaveFormat.SampleRate && x.Channels == source.WaveFormat.Channels))
                {
                    int sampleRate =
                        supportedFormats.OrderBy(x => Math.Abs(source.WaveFormat.SampleRate - x.SampleRate))
                            .First(x => x.Channels == source.WaveFormat.Channels)
                            .SampleRate;

                    Console.WriteLine("Samplerate {0} -> {1}", source.WaveFormat.SampleRate, sampleRate);
                    Console.WriteLine("Channels {0} -> {1}", source.WaveFormat.Channels, 2);
                    source = source.ChangeSampleRate(sampleRate);
                }
                using (source)
                {
                    using (var encoder = MediaFoundationEncoder.CreateMP3Encoder(source.WaveFormat, GlobalMp3Path + videoTitle + ".mp3"))
                    {
                        byte[] buffer = new byte[source.WaveFormat.BytesPerSecond];
                        int read;
                        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            encoder.Write(buffer, 0, read);
                        }
                    }
                }
                // Set metadata album to playlist name
                TagLib.File f = TagLib.File.Create(GlobalMp3Path + videoTitle + ".mp3");
                f.Tag.Album = GlobalMp3Path.Substring(GlobalMp3Path.TrimEnd('\\').LastIndexOf("\\") + 1).TrimEnd('\\');
                f.Save();
                return false;
            }
        }
    }
}
