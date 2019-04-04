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


namespace WpfApp3
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

            SQLiteConnection conn;

            conn = new SQLiteConnection("Data Source=c:\\temp\\sqlite.db; Version = 3; ");
            conn.Open();
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

            conn.Close();

            return groups;
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
