using System;
using System.IO;
using System.IO.Compression;
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
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

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
        }
        private void Test_Executed2(object sender, ExecutedRoutedEventArgs e)
        {
            List<Rule> rules = new List<Rule>();
            rules.Add(new Rule(@"E:\", @"D:\"));
            rules.Add(new Rule(@"E:\", @"C:\"));
            rules.Add(new Rule(@"D:\", @"C:\"));

            int n = 0;
            ArrayList dups = db.top(n);
            for (int iDup = 0; iDup < dups.Count; iDup++)
            {
                object[] dup = (object[])dups[iDup];
                object[] files = (object[])dup[2];

                for (int iRule = 0; iRule < rules.Count; iRule++)
                {
                    string FileToKeep = "";
                    string[] FilesToRemove = rules[iRule].filter(files, ref FileToKeep);
                    if (FilesToRemove != null)
                    {
                        for (int iFiles = 0; iFiles < FilesToRemove.Length; iFiles++)
                        {
                            //Debug.WriteLine($"fc /b '{FilesToRemove[iFiles]}' '{FileToKeep}'");
                            RemovePath(FilesToRemove[iFiles]);

                            this.lblMsg.Text = $"Removing {FilesToRemove[iFiles]}";
                            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(delegate { }));
                        }
                    }
                }
            }
            this.lblMsg.Text = $"Removed top {n} duplicates";
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


        DateTime epoch = new DateTime(1970, 1, 1);

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
                    if (file_size > 0)
                    {
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
                                strHash = (new Doc(p)).Hash;
                                db.Delete(p);
                                db.Write("f", p, file_size, file_mtime, strHash);
                            }
                            Dict.Remove(p);
                        }
                        else
                        {
                            strHash = (new Doc(p)).Hash;
                            db.Write("f", p, file_size, file_mtime, strHash);
                        }

                        size = size + file_size;
                        byte[] bytesHash = Encoding.ASCII.GetBytes(strHash);
                        md5.TransformBlock(bytesHash, 0, bytesHash.Length, bytesHash, 0);
                        files_count++;
                    }
                }
            }
            byte[] bytes = new byte[0];
            md5.TransformFinalBlock(bytes, 0, 0);
            strHash = Doc.md5hex(md5.Hash);

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

            ShowTop(10);
        }

        string WasteFolder = @":\wasteland.fdd";
        private void RemovePath(string path)
        {
            if (Directory.Exists(path))
            {
                string dest = path.Replace(":", WasteFolder);
                Match m = Regex.Match(dest, @"(.*)\\(.*)$");
                string folder = m.Groups[1].Value;
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                FileInfo fi = new FileInfo(path);
                if ((fi.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    Directory.Move(path, dest);
                else
                    File.Move(path, dest);
            }
            db.DeleteFolder(path);
        }
        private void Refresh_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void ShowTop(int n)
        {
            ArrayList groups = db.top(n);
            string strGroups = "";

            for (int k = 0; k < groups.Count; k++)
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
        private void Refresh_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowTop(10);
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
