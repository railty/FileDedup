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
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace fdd
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
        public static RoutedCommand GenerateCommand = new RoutedCommand();
        public static RoutedCommand ScanCommand = new RoutedCommand();
        public static RoutedCommand TestCommand = new RoutedCommand();
        private void Test_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void Test_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string s = @"C:\Temp\sqlite.db";
            string d = @"C:\Temp\aaa\";

            RemovePath(d);
            RemovePath(s);
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
            string name1 = @"D:\Pictures\video2\lost-ntfs.img";
            FileInfo fi = new FileInfo(name1);
            long size1 = fi.Length;
            string strHash1 = cal_file(name1, size1);
            */


            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = dialog.SelectedPath;

                long mem1 = get_mem();
                DateTime t1 = DateTime.Now;
                Dict = db.LoadDict(path);
                long size = 0;
                int mtime = 0;
                string md5 = "";
                int folders_count = 0;
                int files_count = 0;
                int files_cached_count = 0;
                cal_folder(path, ref size, ref mtime, ref md5, ref folders_count, ref files_count, ref files_cached_count);

                foreach (string fname in Dict.Keys)
                {
                    if (db.Delete(fname))
                    {
                        this.lblMsg.Text = $"clean up db, delete {fname}";
                        Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(delegate { }));
                    }
                }

                db.Commit();
                long mem2 = get_mem();
                DateTime t2 = DateTime.Now;

                this.lblMsg.Text = $"{path}, total time: {ToSeconds(t2 - t1)} seconds, total mem = {ToMB(mem1)} ---> {ToMB(mem2)}, total folders:{folders_count}, total files:{files_cached_count}/{files_count}, total size:{ToMB(size)}";
            }
        }

        private int ToSeconds(TimeSpan dltT)
        {
            return (int)dltT.TotalSeconds;
        }
        private string ToMB(long mem)
        {
            return (int)(mem/1024.0/1024.0) + "M";
        }

        Dictionary<string, object[]> Dict;
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
        private string cal_file_full(string fname)
        {
            long file_size = (new FileInfo(fname)).Length;
            MD5 md5 = new MD5CryptoServiceProvider();
            FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read);
            byte[] dat = new byte[BlockSize];

            int len = fs.Read(dat, 0, BlockSize);
            md5.TransformBlock(dat, 0, len, dat, 0);

            long block = 1;
            while (block * BlockSize < file_size)
            {
                fs.Seek(block * BlockSize, SeekOrigin.Begin);
                len = fs.Read(dat, 0, BlockSize);
                md5.TransformBlock(dat, 0, len, dat, 0);
                block = block + 1;
            }

            md5.TransformFinalBlock(dat, 0, 0);
            byte[] hash = md5.Hash;
            return md5hex(hash);
        }
        private string cal_file_1pass(string fname)
        {
            long file_size = (new FileInfo(fname)).Length;
            MD5 md5 = new MD5CryptoServiceProvider();
            FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read);
            byte[] dat = new byte[BlockSize];

            int len = fs.Read(dat, 0, BlockSize);
            md5.TransformBlock(dat, 0, len, dat, 0);

            long block = 1;
            while (block * BlockSize < file_size)
            {
                fs.Seek(block * BlockSize, SeekOrigin.Begin);
                len = fs.Read(dat, 0, BlockSize);
                md5.TransformBlock(dat, 0, len, dat, 0);
                block = block * 2;
            }

            md5.TransformFinalBlock(dat, 0, 0);
            byte[] hash = md5.Hash;
            return md5hex(hash);
        }

        private string cal_file_pass2(string fname)
        {
            long file_size = (new FileInfo(fname)).Length;
            long n = (long)Math.Ceiling((double)file_size / BlockSize);
            List<long> blocks = new List<long>();

            blocks.Add(0);
            blocks.Add(n - 1);
            long i = 1;
            while (i < n)
            {
                blocks.Add(i);
                blocks.Add(n - 1 - i);
                i = i * 2;
            }

            long[] unique_blocks = blocks.Distinct().ToArray<long>();
            Array.Sort(unique_blocks);

            MD5 md5 = new MD5CryptoServiceProvider();
            FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read);
            byte[] dat = new byte[BlockSize];

            for (long block = 0; block < unique_blocks.Length; block++)
            {
                fs.Seek(block * BlockSize, SeekOrigin.Begin);
                int len = fs.Read(dat, 0, BlockSize);
                md5.TransformBlock(dat, 0, len, dat, 0);

            }

            md5.TransformFinalBlock(dat, 0, 0);
            byte[] hash = md5.Hash;
            return md5hex(hash);

        }
        private string cal_file(string fname)
        {
            return cal_file_pass2(fname);
        }
        private void cal_folder(string path, ref long size, ref int mtime, ref string strHash, ref int folders_count, ref int files_count, ref int files_cached_count)
        {
            var files = Directory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
            ArrayList names = new ArrayList();
            foreach (string file in files)
            {
                //.$signature
                //Thumbs.db
                if (!Regex.Match(file, @"[A-Z]\:\\\$RECYCLE\.BIN|System Volume Information|Recovery|wasteland\.fdd|Thumbs.db").Success)
                    names.Add(file);
            }
            names.Sort();

            MD5 md5 = new MD5CryptoServiceProvider();
            for (int i = 0; i < names.Count; i++)
            {
                string p = (string)names[i];
                FileInfo fi = new FileInfo(p);

                if (fi.Attributes.HasFlag(FileAttributes.Directory))
                {
                    string folder_md5 = "";
                    long folder_size = 0;
                    int folder_mtime = 0;

                    int sub_folders_count = 0;
                    int sub_files_count = 0;
                    int sub_files_cached = 0;

                    cal_folder(p, ref folder_size, ref folder_mtime, ref folder_md5, ref sub_folders_count, ref sub_files_count, ref sub_files_cached);
                    folders_count += sub_folders_count;
                    files_count += sub_files_count;
                    files_cached_count += sub_files_cached;

                    size = size + folder_size;
                    byte[] folder_md5_bytes = Encoding.ASCII.GetBytes(folder_md5);
                    md5.TransformBlock(folder_md5_bytes, 0, folder_md5_bytes.Length, folder_md5_bytes, 0);

                    folders_count++;
                }
                else
                {
                    long file_size = fi.Length;
                    strHash = "";
                    int file_mtime = (int)(fi.LastWriteTimeUtc - epoch).TotalSeconds;

                    if (Dict.ContainsKey(p))
                    {
                        if ((long)Dict[p][0] == file_size && (int)Dict[p][1] == file_mtime)
                        {
                            strHash = (string)Dict[p][2];
                            files_cached_count++;
                        }
                        else
                        {
                            strHash = cal_file(p);
                            db.Delete(p);
                            db.Write("f", p, file_size, file_mtime, strHash);
                        }
                        Dict.Remove(p);
                    }
                    else
                    {
                        strHash = cal_file(p);
                        db.Write("f", p, file_size, file_mtime, strHash);
                    }

                    size = size + file_size;
                    byte[] bytesHash = Encoding.ASCII.GetBytes(strHash);
                    md5.TransformBlock(bytesHash, 0, bytesHash.Length, bytesHash, 0);
                    files_count++;
                }
            }
            byte[] bytes = new byte[0];
            md5.TransformFinalBlock(bytes, 0, 0);
            strHash = md5hex(md5.Hash);

            mtime = (int)((new FileInfo(path)).LastWriteTimeUtc - epoch).TotalSeconds;

            if (Dict.ContainsKey(path))
            {
                if ((long)Dict[path][0] == size && (int)Dict[path][1] == mtime && (string)Dict[path][2] == strHash)
                { }
                else
                {
                    db.Delete(path);
                    db.Write("d", path, size, mtime, strHash);
                }

                Dict.Remove(path);
            }
            else db.Write("d", path, size, mtime, strHash);

            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(delegate { }));
            this.lblMsg.Text = $"{path}, total folders:{folders_count}, total files:{files_cached_count}/{files_count}, total size:{ToMB(size)}";
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

                            str = str + $"move \"{path}\" {drive}\\waste\\ \r\n";
                            RemovePath(path);
                        }
                    }
                }
            }

            this.cmds.Text = str;
            File.WriteAllText(@"C:\Temp\dedup.cmd", str);
        }

        string WasteFolder = @":\wasteland.fdd";
        private void RemovePath(string path)
        {
            string dest = path.Replace(":", WasteFolder);
            Match m = Regex.Match(dest, @"(.*)\\(.*)$");
            string folder = m.Groups[1].Value;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            FileInfo fi = new FileInfo(path);
            if ((fi.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                Directory.Move(path, dest);
            }
            else
            {
                File.Move(path, dest);
            }
        }
        private void Refresh_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Refresh_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ArrayList groups = db.top(10);

            string strGroups = "";

            for (int k=0; k<groups.Count; k++)
            {
                object[] os = (object[])groups[k];

                string md5 = (string)os[0];
                long size = (long)os[1];
                object[] names = (object[])os[2];
                object[] states = (object[])os[3];

                string strFs = "";
                for (int i = 0; i < names.Length; i++)
                {
                    string name = (string)names[i];
                    name = name.Replace("&", "&amp;");
                    strFs = strFs + $@"<RadioButton Content=""{name}"" />";
                }
                    
                strGroups = strGroups + $@"<GroupBox Header =""{ToMB(size)}""><StackPanel>{strFs}</StackPanel></GroupBox>";
            }
            string strPanel = $@"<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" > {strGroups} </StackPanel>";
            StackPanel stackPanel = (StackPanel)XamlReader.Parse(strPanel);
            this.groups.Content = stackPanel;
        }

        Db db;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            db = new Db();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            db.Close();
        }
    }
}
/*
1. delete signature from d:\data\sning\books\operator
and it should match d:\data\sning\operators
2. rescan and delete from db
3. move to waste.fdd
    double quote
    move sub folder

    */
