using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Markup;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Diagnostics;

namespace FDD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Scan_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private long get_mem()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return Process.GetCurrentProcess().VirtualMemorySize64;
        }

        private void Scan_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            /*
            load_dict(@"d:Alex\");
            string name1 = @"d:Alex\Documents\alex.mp4";
            long size1 = 0;
            int mtime1 = 0;
            string strHash1 = "";
            cal_file(name1, ref size1, ref mtime1, ref strHash1);
            */

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = dialog.SelectedPath;

                long mem1 = get_mem();
                DateTime t1 = DateTime.Now;
                load_dict(path);
                long sz = 0;
                int mtime = 0;
                string md5 = "";
                cal_folder(path, ref sz, ref mtime, ref md5);
                long mem2 = get_mem();
                DateTime t2 = DateTime.Now;

                this.lblMsg.Text = $"total time: {(t2 - t1).TotalSeconds} seconds, total mem = {mem1/1024.0/1024.0}M ---> {mem2 / 1024.0 / 1024.0}M";
            }
        }

        Dictionary<string, object[]> dict = new Dictionary<string, object[]>();
        private void load_dict(string path)
        {
            dict.Clear();
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            string pattern = path.ToLower();

            sqlite_cmd.CommandText = $"select name, size, mtime, md5 from files where lower(name) like '{pattern}%' order by name;";
            SQLiteDataReader sqlite_datareader = sqlite_cmd.ExecuteReader();
            while (sqlite_datareader.Read())
            {
                string name = sqlite_datareader.GetString(0);
                long size = sqlite_datareader.GetInt64(1);
                int mtime = sqlite_datareader.GetInt32(2);
                string strHash = sqlite_datareader.GetString(3);

                dict.Add(name, new object[] {size, mtime, strHash});
            }
        }

        int BlockSize = 16 * 512;

        private string md5hex(byte[] hash)
        {
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sBuilder.Append(hash[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        DateTime epoch = new DateTime(1970, 1, 1);

        /*
         * read 0
         * read 1 2 4 8 ... until over the size
         * in the next version should do it reverse, ie
         * seek to end, and reverse back the same way
         */
        private string cal_file(string fname, ref long size, ref int mtime, ref string strHash)
        {
            FileInfo fi = new FileInfo(fname);
            size = fi.Length;
            mtime = (int)(fi.LastWriteTimeUtc - epoch).TotalSeconds;

            if (dict.ContainsKey(fname))
            {
                if ((long)dict[fname][0] == size && (int)dict[fname][1] == mtime)
                {
                    strHash = (string)dict[fname][2];
                    return strHash;
                }
            }

	        MD5 md5 = new MD5CryptoServiceProvider();
            FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read);
            byte[] dat = new byte[BlockSize];
            int len = fs.Read(dat, 0, BlockSize);
            md5.TransformBlock(dat, 0, len, dat, 0);
            //string x = md5hex(md5.Hash);
            int sz = 1;
            size = new FileInfo(fname).Length;
            while (sz * BlockSize < size)
            {
                fs.Seek(sz * BlockSize, SeekOrigin.Begin);
                len = fs.Read(dat, 0, BlockSize);
                md5.TransformBlock(dat, 0, len, dat, 0);
                //x = md5hex(md5.Hash);
                sz = sz * 2;
            }
            md5.TransformFinalBlock(dat, 0, 0);
            byte[] hash = md5.Hash;
            strHash = md5hex(hash);
            return strHash;
        }


        private void cal_folder(string path, ref long size, ref int mtime, ref string strHash)
        {
            var files = Directory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
            ArrayList names = new ArrayList();
            foreach (string file in files)
            {
                names.Add(file);
            }
            names.Sort();

            MD5 md5 = new MD5CryptoServiceProvider();
            for (int i = 0; i < names.Count; i++)
            {
                string p = (string) names[i];
                FileAttributes attr = File.GetAttributes(p);

                if (attr.HasFlag(FileAttributes.Directory))
                {
                    string folder_md5 = "";
                    long folder_size = 0;
                    int folder_mtime = 0;

                    cal_folder(p, ref folder_size, ref folder_mtime, ref folder_md5);
                    size = size + folder_size;
                    byte[] folder_md5_bytes = Encoding.ASCII.GetBytes(folder_md5);
                    md5.TransformBlock(folder_md5_bytes, 0, folder_md5_bytes.Length, folder_md5_bytes, 0);
                }

                else
                {
                    string file_md5 = "";
                    long file_size = 0;
                    int file_mtime = 0;
                    cal_file(p, ref file_size, ref file_mtime, ref file_md5);

                    size = size + file_size;
                    byte[] file_bytes = Encoding.ASCII.GetBytes(file_md5);
                    md5.TransformBlock(file_bytes, 0, file_bytes.Length, file_bytes, 0);
                }
            }
            byte[] bytes = new byte[0];
            md5.TransformFinalBlock(bytes, 0, 0);
            strHash = md5hex(md5.Hash);
        }

        private void Generate_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Generate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string str = "";
            StackPanel gs = (StackPanel)this.groups.Content;
            for (int i = 0; i < gs.Children.Count; i++)
            {
                GroupBox g = (GroupBox)gs.Children[i];
                StackPanel p = (StackPanel)g.Content;

                bool selected = false;

                for (int k = 0; k < p.Children.Count; k++)
                {
                    RadioButton rb = (RadioButton)p.Children[k];
                    if (rb.IsChecked.HasValue && rb.IsChecked == true)
                    {
                        selected = true;
                    }

                }

                if (selected)
                {
                    for (int k = 0; k < p.Children.Count; k++)
                    {
                        RadioButton rb = (RadioButton)p.Children[k];
                        if (!rb.IsChecked.HasValue || rb.IsChecked == false)
                        {
                            string path = (string)rb.Content;
                            string drive = Path.GetPathRoot(path);

                            str = str + $"move '{path}' {drive}\\waste\\ \r\n";
                        }

                    }
                }
            }

            this.cmds.Text = str;
            File.WriteAllText(@"C:\Temp\dedup.cmd", str);
        }


        public static RoutedCommand GenerateCommand = new RoutedCommand();
        public static RoutedCommand ScanCommand = new RoutedCommand();


        private void Refresh_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Refresh_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ArrayList groups = read_db();

            string strGroups = "";

            for (int k=0; k<groups.Count; k++)
            {
                object[] os = (object[])groups[k];

                string md5 = (string)os[0];
                int size = (int)os[1];
                object[] names = (object[])os[2];
                object[] states = (object[])os[3];

                string strFs = "";
                for (int i = 0; i < names.Length; i++)
                    strFs = strFs + $@"<RadioButton Content=""{(string)names[i]}"" />";

                strGroups = strGroups + $@"<GroupBox Header =""{md5}""><StackPanel>{strFs}</StackPanel></GroupBox>";
            }
            string strPanel = $@"<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" > {strGroups} </StackPanel>";
            StackPanel stackPanel = (StackPanel)XamlReader.Parse(strPanel);
            this.groups.Content = stackPanel;
        }

        private ArrayList read_db()
        {
            ArrayList groups = new ArrayList();

            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "select size, md5, name from files where md5 in (SELECT md5 FROM files group by md5 having count(*)>1 order by size desc limit 10) order by size desc, md5, name;";

            sqlite_datareader = sqlite_cmd.ExecuteReader();

            string last_md5 = "";
            int last_size = 0;
            ArrayList names = new ArrayList();
            ArrayList states = new ArrayList();

            string k;
            string[] v;
            while (sqlite_datareader.Read())
            {
                string md5 = sqlite_datareader.GetString(1);
                int size = sqlite_datareader.GetInt32(0);
                string name = sqlite_datareader.GetString(2);

                if (last_md5 == md5 || last_md5 == "")
                {
                    names.Add(name);
                    states.Add("Yes");
                }
                else
                {
                    k = $"{last_md5}-{last_size}";
                    v = new string[names.Count];
                    for (int i = 0; i < names.Count; i++) v[i] = (string)names[i];
                    groups.Add(new object[4]{last_md5, last_size, names.ToArray(), states.ToArray() });

                    names = new ArrayList();
                    states = new ArrayList();
                    names.Add(name);
                    states.Add("Yes");
                }
                last_md5 = md5;
                last_size = size;
            }

            k = $"{last_md5}-{last_size}";
            v = new string[names.Count];
            for (int i = 0; i < names.Count; i++) v[i] = (string)names[i];
            groups.Add(new object[4] { last_md5, last_size, names.ToArray(), states.ToArray() });

            return groups;
        }

        SQLiteConnection conn;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            conn = new SQLiteConnection("Data Source=c:\\temp\\sqlite.db; Version = 3; ");
            conn.Open();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            conn.Close();
        }
    }
}

/*
 using System.Threading;

ThreadPool.QueueUserWorkItem(new WaitCallback(Process));
SetMaxThreads();
SetMinThreads()

        static void Process(object callback)
        {
        }


	FileStream.Read(Byte[], Int32, Int32)
	FileStream.ReadAsync
	public override long Seek (long offset, System.IO.SeekOrigin origin);
	
	
	
	
	MD5 md5Hash = MD5.Create())
	MD5 md5 = new MD5CryptoServiceProvider();
       byte[] result = md5.ComputeHash(data);

	
	using System.Collections.Generic;
 
	 HashSet< string > hSet = new HashSet< string >(names);
	

Console.WriteLine($"Hello, {name}! Today is {date.DayOfWeek}, it's {date:HH:mm} now.");


StringReader stringReader = new StringReader(savedButton);

     */
