using System;
using System.Windows.Forms;
using VideoLibrary;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.IO.Compression;
using NicoNico.Net.Managers;
using System.Net;
using NicoNico.Net.Entities.User;
using System.Collections.Generic;
using System.Text;
using SoundCloudExplode;
using System.Net.Http.Headers;
using System.Linq;

namespace ytdownload
{
    public partial class Form1 : Form
    {
        private string filepath;
        public List<string> settings;
       NicoNico.Net.Managers.AuthenticationManager authManager;
        CookieContainer cookieContainer;
        UserSession session;
        //0 defaultpath
        //1 defaultmusicbool
        //2 defaultmusictype
        //3 defaulturl
        //4 niconicoemail
        //5 niconicopass


        public  Form1()

        {
            InitializeComponent();
            settings= new List<string>(100);
            readsetting();
            textBox2.Text = settings[0];
            checkBox1.Checked = bool.Parse(settings[1]);
            comboBox1.Text = settings[2];
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
            settings[1] = checkBox1.Checked.ToString();
            settings[2] = comboBox1.Text;
            settings[3] = textBox1.Text;
            setsetting();
            Application.Exit();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(textBox2.Text)) { MessageBox.Show("パスが不正です。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                if(textBox1.Text.IndexOf("youtu.be")!=-1|| textBox1.Text.IndexOf("youtube.com") != -1)
                {
                    if(textBox1.Text.IndexOf("playlist") != -1)
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

            if (checkBox1.Checked)
            {
                try
                {
                    string kakutyousi = comboBox1.Text;
                    string num = "";
                    if (kakutyousi == "mp3")
                    {
                        num = "1";
                    }
                    else if (kakutyousi == "wav")
                    {
                        num = "2";
                    }
                    else if (kakutyousi == "ogg")
                    {
                        num = "3";
                    }
                    else
                    {
                        MessageBox.Show("音声形式が不正です。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    File.Delete(Application.LocalUserAppDataPath + @"\EcoDecoTooL114\EcoDecoTooL.ini");
                    File.Copy(Application.LocalUserAppDataPath + @"\EcoDecoTooL114\temp" + num + ".ini", Application.LocalUserAppDataPath + @"\EcoDecoTooL114\EcoDecoTooL.ini");
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = Application.LocalUserAppDataPath + @"\EcoDecoTooL114\EcoDecoTooL.exe";
                    info.Arguments = "\"" + filepath + "\"";
                    info.WindowStyle = ProcessWindowStyle.Hidden;
                    Process p = Process.Start(info);
                    p.WaitForExit();
                    File.Delete(filepath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        async
        Task
savevideo(string link, string path)
        {
            /*
            var youtube = YouTube.Default;
            
            var video = youtube.GetVideo(link);
            
            var client = new HttpClient();
            long? totalByte = 0;

            string fileresultpath = path + @"\" + delinvalidchar(video.Title);
            filepath = fileresultpath;
            Console.WriteLine(fileresultpath);
            using (Stream output = File.OpenWrite(fileresultpath))
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, video.Uri))
                {
                    totalByte = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result.Content.Headers.ContentLength;
                }
                using (var input = await client.GetStreamAsync(video.Uri))
                {
                    byte[] buffer = new byte[16 * 1024];
                    int read;
                    int totalRead = 0;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, read);
                        totalRead += read;
                        progressBar1.Value = (int)((totalRead / (double)totalByte) * 100);
                    }
                }
            }*/
            var youtube = new CustomYouTube();
            if (checkBox2.Checked)
            {
                Console.WriteLine("最高画質");
                var videos = await youtube.GetAllVideosAsync(link);
                var maxResolution = videos.First(i => i.Resolution == videos.Max(j => j.Resolution));
                await youtube
                    .CreateDownloadAsync(
                    new Uri(maxResolution.Uri),
                    Path.Combine(path, maxResolution.FullName),
                    new Progress<Tuple<long, long>>((Tuple<long, long> v) =>
                    {
                        var percent = (int)((v.Item1 * 100) / v.Item2);
                        progressBar1.Value = percent;
                        label3.Text = string.Format("Downloading.. ( % {0} ) {1} / {2} MB\r", percent, (v.Item1 / (double)(1024 * 1024)).ToString("N"), (v.Item2 / (double)(1024 * 1024)).ToString("N"));
                    }));
            }
            else
            {
                var video=await youtube.GetVideoAsync(link);
                await youtube
                    .CreateDownloadAsync(
                    new Uri(video.Uri),
                    Path.Combine(path, video.FullName),
                    new Progress<Tuple<long, long>>((Tuple<long, long> v) =>
                    {
                        var percent = (int)((v.Item1 * 100) / v.Item2);
                        progressBar1.Value = percent; 
                        label3.Text = string.Format("Downloading.. ( % {0} ) {1} / {2} MB\r", percent, (v.Item1 / (double)(1024 * 1024)).ToString("N"), (v.Item2 / (double)(1024 * 1024)).ToString("N"));
                    }));
            }
            
                
        }

         

        string delinvalidchar(string content)
        {
            while (true)
            {
                char[] invalidChars = System.IO.Path.GetInvalidPathChars();
                
                Array.Resize(ref invalidChars, invalidChars.Length+1);
                invalidChars[invalidChars.Length - 1] = ':';
                Array.Resize(ref invalidChars, invalidChars.Length + 1);
                invalidChars[invalidChars.Length - 1] = '　';
                Console.WriteLine(invalidChars);
                int num = content.IndexOfAny(invalidChars);
                Console.WriteLine(num);
                if (num == -1) { break; }
                else
                {
                    Console.WriteLine(content);
                    content = content.Remove(num, 1);
                    Console.WriteLine(content);
                }
            }
            if (content.Substring(content.Length - 4) != ".mp4") { content += ".mp4"; }
            return content;
        }

        void ecodecotool()
        {
            bool isfirst = bool.Parse(settings[1]);
            Console.WriteLine(isfirst);
            if (isfirst)
            {
                try
                {
                    byte[] ecodecotoolzip = Properties.Resources.EcoDecoTooL114;
                    ZipArchive zipArchive = new ZipArchive(new MemoryStream(ecodecotoolzip));
                    zipArchive.ExtractToDirectory(Application.LocalUserAppDataPath );
                    settings[1] = "false";
                    setsetting();
                }
                catch(Exception e)
                {
                    Console.Write("zip\n"+e.ToString());
                }
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
                StreamReader streamreader=new StreamReader(path);
               var s= streamreader.ReadToEnd().Split(',');
                settings.AddRange(s);
                streamreader.Close();
            }
            else
            {
                settings.Add("");
                settings.Add("true");
                settings.Add("mp3");
                settings.Add("");
                //path= Path.GetFullPath(path)
                var f =System.IO.File.Create("data.txt");
            
                f.Close();
                setsetting();   
            }
        }

       async Task postniconicourl(string url,string path)
        {
            try
            {
                string jsonString = "{\"url\":\"" + url + "\"}";

                var client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://nico.forestfamily4.repl.co");
                request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                var response = await client.SendAsync(request);
               string result= await response.Content.ReadAsStringAsync();
                Console.WriteLine(result);
                var d = new DateTimeOffset();
                filepath = path + @"\niconico" +d.Hour +d.Minute+d.Second +".mp4";
                await downloadniconico(result);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return; 
            }
            

/*
            var content = new StringContent(jsonString, Encoding.UTF8, @"application/json");
            //POST
            var result = await client.PostAsync(@"https://nico.forestfamily4.repl.co", content);
            Console.WriteLine(result);*/
        }

        async Task downloadniconico(string url)
        {
            filepath = delinvalidchar(filepath);
            string temp = "https://pf021372593.dmc.nico/vod/ht2_nicovideo/nicovideo-sm9664372_f99f651322b7a33066174c6777289c9612db5cd84f361923e82d238cf8f433dd?ht2_nicovideo=6-xOoSBVy9WP_1650435433594.gkm7qqhyvu_ramk4q_3gmj6464pkn7t.mp4";
            var client = new HttpClient();
            var response = await client.GetAsync(url);
           // var response = await client.GetAsync(temp);
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                Console.WriteLine(filepath);
                File.Create(filepath).Close();
                var fileInfo = new FileInfo(filepath);
                using (var fileStream = fileInfo.OpenWrite())
                {
                    
                    await stream.CopyToAsync(fileStream);
                    long length=0;
                    while (true)
                    {
                        Console.WriteLine(length);
                        if (length==new FileInfo(filepath).Length)
                        {
                            break;
                        }
                        else
                        {
                            length = fileStream.Length;
                            Console.WriteLine(length);
                        }
                    }
                }
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            filepath = textBox2.Text + @"\ニコニコ動画" + DateTime.Now.ToString();
            await downloadniconico(textBox1.Text);
        }


        private Progress<double> _Progress_Soundcloud;
        
        async void DownloadSoundCloud(string path,string link)
        {
            var soundcloud = new SoundCloudClient();

            var track=await soundcloud.Tracks.GetAsync(link);
            var trackName = string.Join("_", track.Title.Split(Path.GetInvalidFileNameChars()));
            string fileresultpath = path + "\\" + trackName + ".mp3";
            Debug.WriteLine("soundcloud");
            _Progress_Soundcloud = new Progress<double>(x =>
            {
                progressBar2.Value = (int)(x*100);
            });
            soundcloud.DownloadAsync(track, fileresultpath,_Progress_Soundcloud);
        }
    }

    class CustomYouTube : YouTube
    {
        private long chunkSize = 10_485_760;
        private long _fileSize = 0L;
        private HttpClient _client = new HttpClient();
        protected override HttpClient MakeClient(HttpMessageHandler handler)
        {
            return base.MakeClient(handler);
        }
        public async Task CreateDownloadAsync(Uri uri, string filePath, IProgress<Tuple<long, long>> progress)
        {
            var totalBytesCopied = 0L;
            _fileSize = await GetContentLengthAsync(uri.AbsoluteUri) ?? 0;
            if (_fileSize == 0)
            {
                throw new Exception("File has no any content !");
            }
            using (Stream output = File.OpenWrite(filePath))
            {
                var segmentCount = (int)Math.Ceiling(1.0 * _fileSize / chunkSize);
                for (var i = 0; i < segmentCount; i++)
                {
                    var from = i * chunkSize;
                    var to = (i + 1) * chunkSize - 1;
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Headers.Range = new RangeHeaderValue(from, to);
                    using (request)
                    {
                        // Download Stream
                        var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                        if (response.IsSuccessStatusCode)
                            response.EnsureSuccessStatusCode();
                        var stream = await response.Content.ReadAsStreamAsync();
                        //File Steam
                        var buffer = new byte[81920];
                        int bytesCopied;
                        do
                        {
                            bytesCopied = await stream.ReadAsync(buffer, 0, buffer.Length);
                            output.Write(buffer, 0, bytesCopied);
                            totalBytesCopied += bytesCopied;
                            progress.Report(new Tuple<long, long>(totalBytesCopied, _fileSize));
                        } while (bytesCopied > 0);
                    }
                }
            }
        }
        private async Task<long?> GetContentLengthAsync(string requestUri, bool ensureSuccess = true)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Head, requestUri))
            {
                var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (ensureSuccess)
                    response.EnsureSuccessStatusCode();
                return response.Content.Headers.ContentLength;
            }
        }
    }
}