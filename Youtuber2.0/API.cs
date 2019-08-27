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
using Telegram.Bot;
using Telegram.Bot.Args;
using VideoLibrary;

namespace Youtuber2._0
{
    class API
    {
        static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Telegram Bot (YoutuberTwoBot)
        static ITelegramBotClient botClient;

        string GlobalMp3Path = "";

        public static Boolean SendErrors { get; set; }
        public static Boolean SendInfo { get; set; }

        public void init()
        {
            // Initiate bot
            botClient = new TelegramBotClient("738031783:AAHvgFBF37CxEK7BLpNI5fxc0vylWWmcNAQ");
            var me = botClient.GetMeAsync().Result;

            // Initiate bot receiver
            botClient.OnMessage += Bot_OnMessage;
            botClient.StartReceiving();
        }

        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                await botClient.SendTextMessageAsync(
                  chatId: e.Message.Chat,
                  text: "Hello Benno! You said:\n" + e.Message.Text
                );
            }
        }

        public async void TelegramBotSendError(string message)
        {
            if (SendErrors)
            {
                await botClient.SendTextMessageAsync(
                  chatId: 696097263,
                  text: message
                );
            }
        }

        public async void TelegramBotSendInfo(string message)
        {
            if (SendInfo)
            {
                await botClient.SendTextMessageAsync(
                chatId: 696097263,
                text: message
                );
            }
        }

        public void ProcessVideosToMp3(List<VideoObject> allVideoIds, string pathMp3Files, string playlistTile)
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
                        if (!File.Exists(pathMp3Files + videoHighRes.FullName + ".mp3"))
                        {
                            string test = pathMp3Files + videoHighRes.FullName + ".mp3";
                            for (int attempts = 0; attempts < 5; attempts++)
                            // if you really want to keep going until it works, use   for(;;)
                            {
                                try
                                {
                                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Attempt number : " + attempts + " of file " + videoHighRes.Title);
                                    //content = videoHighRes.GetBytes();
                                    GlobalMp3Path = pathMp3Files;
                                    DownloadYoutubeVideo(videoHighRes.Uri, videoHighRes.FullName);
                                    break;
                                }
                                catch (Exception x)
                                {
                                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error in retry " + attempts + " with message : " + x.Message);
                                }
                                System.Threading.Thread.Sleep(1000); // Possibly a good idea to pause here
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
                        TelegramBotSendError($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video : " + videoObject.Title + " with error message: " + ex.Message);
                    }
                    stopWatchFile.Stop();
                    _log.Debug($"Thread : {Thread.CurrentThread.ManagedThreadId} => File downloaded in : " + stopWatchFile.Elapsed + " seconds.");
                    _log.Info($"Thread : {Thread.CurrentThread.ManagedThreadId} => Downloaded : " + videoObject.Title);
                    TelegramBotSendInfo("Processed: " + videoObject.Title + " in " + stopWatchFile.Elapsed + " seconds.");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private bool DownloadYoutubeVideo(string uri, string videoFullName)
        {
            try
            {
                IWaveSource videoSource = CSCore.Codecs.CodecFactory.Instance.GetCodec(new Uri(uri));
                Tuple<IWaveSource, String> package = Tuple.Create(videoSource, videoFullName);
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(ThreadedConvertToMp3), package);
            }
            catch (Exception ex)
            {
                if (File.Exists(GlobalMp3Path + videoFullName + ".mp3"))
                {
                    File.Delete(GlobalMp3Path + videoFullName + ".mp3");
                }

                _log.Error($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video : " + videoFullName + " with error message: " + ex.Message);
                TelegramBotSendError($"Thread : {Thread.CurrentThread.ManagedThreadId} => Error during retrieving or writing video : " + videoFullName + " with error message: " + ex.Message);

                return false;
            }
            return true;
        }

        private void ThreadedConvertToMp3(object callback)
        {
            IWaveSource source = ((Tuple<IWaveSource, String>)callback).Item1;
            String videoTitle = ((Tuple<IWaveSource, String>)callback).Item2;
            ConvertToMp3(source, videoTitle);
        }

        private bool ConvertToMp3(IWaveSource source, string videoTitle)
        {
            var supportedFormats = MediaFoundationEncoder.GetEncoderMediaTypes(AudioSubTypes.MpegLayer3);
            if (!supportedFormats.Any())
            {
                Console.WriteLine("The current platform does not support mp3 encoding.");
                return true;
            }
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

                        //Console.CursorLeft = 0;
                        //Console.Write("{0:P}/{1:P}", (double)source.Position / source.Length, 1);
                    }
                }
            }
            File.Delete(GlobalMp3Path + videoTitle + ".mp4");
            return false;
        }
    }
}
