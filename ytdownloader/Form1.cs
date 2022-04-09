using System;
using System.Windows.Forms;
using VideoLibrary;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.IO.Compression;
using System.Reflection;

namespace ytdownloader
{
    public partial class Form1 : Form
    {
        private string filepath;
        public Form1()
        {
            ecodecotool();
            InitializeComponent();
            textBox2.Text = Properties.Settings.Default.defaultpath;
           checkBox1.Checked = Properties.Settings.Default.defaultmusicbool;
            comboBox1.Text = Properties.Settings.Default.defaultmusictype;
           textBox1.Text = Properties.Settings.Default.defaulturl;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "出力するフォルダーを開いてください。";
            if (dialog.ShowDialog(this) == DialogResult.Cancel)
            {
                return;
            }
            string folderpath=dialog.SelectedPath;
            dialog.Dispose();
            textBox2.Text=folderpath;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.defaultpath = textBox2.Text;
            Properties.Settings.Default.defaultmusicbool = checkBox1.Checked;
            Properties.Settings.Default.defaultmusictype = comboBox1.Text;
            Properties.Settings.Default.defaulturl = textBox1.Text;
            Properties.Settings.Default.Save();
            Application.Exit();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(textBox2.Text)) { MessageBox.Show("パスが不正です。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                await savevideo(textBox1.Text, textBox2.Text);
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
savevideo(string link,string path)
        {
            var youtube = YouTube.Default;
            var video = youtube.GetVideo(link);
            var client = new HttpClient();
            long? totalByte = 0;
            
            string fileresultpath = path + @"\" + video.Title;
            fileresultpath=delinvalidchar(fileresultpath);
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
                        progressBar1.Value = (int)((totalRead / (double)totalByte)*100);
                        Console.WriteLine(totalRead);
                    }
                }
            }
        }

        string delinvalidchar(string content)
        {
            while (true)
            {
                char[] invalidChars = System.IO.Path.GetInvalidPathChars();
                int num = content.IndexOfAny(invalidChars);
                Console.WriteLine(num);
                if (num == -1) { break; }
                else
                {
                    Console.WriteLine(content);
                  content= content.Remove(num,1);
                    Console.WriteLine(content);
                }
            }
            if (content.Substring(content.Length - 4) != ".mp4") { content += ".mp4"; }
            return content;
        }

        void ecodecotool()
        {
             bool isfirst=Properties.Settings.Default.isfirst;
           
            Console.WriteLine(isfirst);
           
            if (isfirst)
            {
                try
                {

                    byte[] ecodecotoolzip = ytdownloader.Properties.Resources.EcoDecoTooL114;
                    ZipArchive zipArchive = new ZipArchive(new MemoryStream(ecodecotoolzip));
                    zipArchive.ExtractToDirectory(Application.LocalUserAppDataPath);
                    Properties.Settings.Default.isfirst = false;
                    Properties.Settings.Default.Save();
                    
                }
                catch { 
                }
            }
        }
    }
}