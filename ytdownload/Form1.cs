using SoundCloudExplode;
using SoundCloudExplode.Tracks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;

namespace ytdownload
{
    public partial class Form1 : Form
    {
        private string filepath;
        public List<string> settings;
        private YoutubeClient youtube = new YoutubeClient();
        private SoundCloudClient soundcloud = new SoundCloudClient();

        private enum VideoConfig
        {
            普通,
            最低画質,
            最高画質,
            音声のみ
        }
        public Form1()

        {
            InitializeComponent();
            settings = new List<string>(100);
            readsetting();
            textBox2.Text = settings[0];
            comboBox_Video.Text = settings[1];
            comboBox_SRT.Text = settings[2];
            textBox1.Text = settings[3];
            ecodecotool();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "出力するフォルダーを開いてください。";
            if (dialog.ShowDialog(this) == DialogResult.Cancel)
            {
                return;
            }
            string folderpath = dialog.SelectedPath;
            dialog.Dispose();
            textBox2.Text = folderpath;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            settings[0] = textBox2.Text;
            settings[1] = comboBox_Video.Text;
            settings[2] = comboBox_SRT.Text;
            settings[3] = textBox1.Text;
            setsetting();
            Application.Exit();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(textBox2.Text)) { MessageBox.Show("パスが不正です。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                if (textBox1.Text.IndexOf("youtu.be") != -1 || textBox1.Text.IndexOf("youtube.com") != -1)
                {
                    if (textBox1.Text.IndexOf("playlist") != -1)
                    {
                        MessageBox.Show("playlistはまた今度対応します...");
                        return;
                    }
                    else
                    {
                        await savevideo(textBox1.Text, textBox2.Text);
                    }
                }
                else if (textBox1.Text.IndexOf("soundcloud.com") != -1)
                {
                    //await postniconicourl(textBox1.Text,textBox2.Text);
                    DownloadSoundCloud(textBox2.Text, textBox1.Text);
                }



            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        async
        Task
savevideo(string link, string path)
        {
            var manifest = await youtube.Videos.Streams.GetManifestAsync(link);
            var videoinfo = await youtube.Videos.GetAsync(link);


            IStreamInfo video;
            var c = GetVideoConfig();
            string title;

            Action<string> callback = null;
            if (c == VideoConfig.音声のみ)
            {
                video = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                callback = (x) =>
                {
                    var temp = Path.Combine(Path.GetTempPath(), $"Template{new Random().Next()}{new Random().Next()}{new Random().Next()}.mp4");
                    var tempo = Path.ChangeExtension(temp, ".mp3");
                    File.Copy(x, temp);
                    FFmpegRun(temp,tempo);
                    File.Copy(tempo, Path.ChangeExtension(x, ".mp3"));
                    File.Delete(temp);
                    File.Delete(tempo);
                    File.Delete(x);
                };
            }
            else
            {
                if (c == VideoConfig.最高画質)
                {
                    video = manifest.GetMuxedStreams().GetWithHighestVideoQuality();
                }
                else if (c == VideoConfig.普通)
                {
                    var a = manifest.GetMuxedStreams();
                    video = a.ElementAt((int)(a.Count() / 2.0f));
                }
                else if (c == VideoConfig.最低画質)
                {
                    video = manifest.GetMuxedStreams().FirstOrDefault();
                }
                else
                {
                    throw new Exception("動画の種類の設定が正しくありません。");
                }
            }
            title = delinvalidchar(videoinfo.Title, ".mp4");
            label4.Text = video.Url;
            var srtpath = Path.Combine(path, title.Substring(0, title.Length - 3) + "srt");
            Console.WriteLine(srtpath);
            path = Path.Combine(path, title);

            var dirPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dirPath))
                Directory.CreateDirectory(dirPath);



            await youtube.Videos.Streams.DownloadAsync(video, path, new Progress<double>((double x) => {
                var recieved = x * video.Size.MegaBytes;
                progressBar1.Value = (int)(x * 100);
                label3.Text = string.Format("Downloading.. ( % {0} ) {1} / {2} MB\r", System.Math.Floor(x * 100), recieved.ToString("N"), video.Size.MegaBytes.ToString("N"));
            }));
            
            

            if (GetSRTLang() != "")
            {
                var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(
    link
);
                var t = trackManifest.GetByLanguage(GetSRTLang());
                await youtube.Videos.ClosedCaptions.DownloadAsync(t, srtpath);
            }
            callback?.Invoke(path);
            label3.Text = "完了!";
        }

        void FFmpegRun(string input,string output,string arg = "")
        {
            var p = new Process();
            p.StartInfo.FileName = FFmpegPath();
            p.StartInfo.WorkingDirectory = Path.GetTempPath();
            p.StartInfo.Arguments = $"-i \"{input}\" {arg} \"{output}\"";
            p.Start();
            p.WaitForExit();
        }

        string delinvalidchar(string content, string kakutyousi)
        {
            char[] invalidChars = System.IO.Path.GetInvalidPathChars();

            System.Array.Resize(ref invalidChars, invalidChars.Length + 1);
            invalidChars[invalidChars.Length - 1] = ':';
            System.Array.Resize(ref invalidChars, invalidChars.Length + 1);
            invalidChars[invalidChars.Length - 1] = '　';
            System.Array.Resize(ref invalidChars, invalidChars.Length + 1);
            invalidChars[invalidChars.Length - 1] = '/';

            while (true)
            {
                int num = content.IndexOfAny(invalidChars);
                if (num == -1) { break; }
                else
                {
                    content = content.Remove(num, 1);
                }
            }
            if (content.Substring(content.Length - kakutyousi.Length) != kakutyousi) { content += kakutyousi; }
            return content;
        }

        void ecodecotool()
        {
            bool isfirst = true;
            bool.TryParse(settings[1], out isfirst);
            if (isfirst)
            {
                settings[1] = "false";
                setsetting();
            }
            if (!File.Exists(FFmpegPath()))
            {
                ExtractFFmpeg();
            }
        }

        void setsetting()
        {
            string path = Application.LocalUserAppDataPath + @"\data.txt";
            StreamWriter s = new StreamWriter(path);
            string a = "";
            for (int i = 0; i < settings.Count; i++)
            {
                a += settings[i] + ",";
            }
            s.Write(a);
            s.Close();
        }

        void readsetting()
        {
            string path = Application.LocalUserAppDataPath + @"\data.txt";
            if (File.Exists(path))
            {
                StreamReader streamreader = new StreamReader(path);
                var s = streamreader.ReadToEnd().Split(',');
                settings.AddRange(s);
                streamreader.Close();
            }
            else
            {
                settings.Add("");
                settings.Add("普通");
                settings.Add("なし");
                settings.Add("");
                File.Create("data.txt").Close();
                setsetting();
            }
        }

        private Progress<double> _Progress_Soundcloud;

        async void DownloadSoundCloud(string path, string link)
        {

            try
            {
                TrackClient t = new TrackClient(soundcloud,new System.Net.Http.HttpClient());
                if (!t.IsUrlValid(link)) { label3.Text = "不明なsoundcloudトラック"; return; }
                var track = await t.GetAsync(link);
                var trackName = string.Join("_", track.Title.Split(Path.GetInvalidFileNameChars()));
                string fileresultpath = path + "\\" + trackName + ".mp3";
                label4.Text =await t.GetDownloadUrlAsync(track);
                _Progress_Soundcloud = new Progress<double>(x =>
                {
                    progressBar2.Value = (int)(x * 100);
                    label3.Text = $"Downloading... {x * 100}%";
                });
                await soundcloud.DownloadAsync(track, fileresultpath, _Progress_Soundcloud);
                label3.Text = "完了！";
            }
            catch(Exception e)
            {
                
            }
        }


        private void label4_Click(object sender, EventArgs e)
        {
            Process.Start(label4.Text);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        string GetSRTLang()
        {
            var t = comboBox_SRT.Text;
            if (t == "なし") { return ""; }
            else if (t == "en" || t == "ja") { return t; }
            else
            {
                return "";
            }
        }

        VideoConfig GetVideoConfig()
        {
            var t = comboBox_Video.Text;
            if (comboBox_Video.Items.Contains(t))
            {
                var a = (VideoConfig)Enum.ToObject(typeof(VideoConfig), comboBox_Video.SelectedIndex);
                return a;
            }
            else
            {
                return VideoConfig.普通;
            }
        }
        
        async void ExtractFFmpeg()
        {
            Console.WriteLine("thinking");
            this.Hide();
            var pw = new ProgressWindow("Downloading..");
            pw.Show();
            this.ShowInTaskbar = false;
            this.Opacity = 0;
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official,new Progress<ProgressInfo>(x => {
                int a =(int)((float) x.DownloadedBytes / (float)x.TotalBytes*100);
                pw.label.Text = $"ファイル操作のためにFFmpegをダウンロードしています... {a}% {(float)x.DownloadedBytes/100000}MB / {(int)(float)x.TotalBytes/100000}MB ";
                pw.progressBar.Value = a;
            }) );
            pw.Dispose();
            var defaultdir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            File.Copy(Path.Combine(defaultdir,"ffmpeg.exe"), FFmpegPath());
            File.Delete(Path.Combine(defaultdir, "ffmpeg.exe"));
            File.Delete(Path.Combine(defaultdir, "ffprobe.exe"));
            this.ShowInTaskbar = true;
            this.Opacity = 100;
        }

        string FFmpegPath()
        {
            return Path.Combine(Application.LocalUserAppDataPath, "ffmpeg.exe");
        }
    }
    
}