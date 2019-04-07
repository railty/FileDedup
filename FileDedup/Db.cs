using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Collections;

namespace fdd
{
    class Db
    {
        int nTrans;
        const int commit_block = 1024;
        SQLiteTransaction trans = null;
        SQLiteConnection conn = null;
        public Db(string DbName = null)
        {
            if (DbName == null) DbName = Directory.GetCurrentDirectory() + "\\fdd.db";

            conn = new SQLiteConnection($"Data Source={DbName}; Version = 3;");
            conn.Open();

            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS files (id integer PRIMARY KEY, type text NOT NULL, name text NOT NULL, size integer, mtime integer NOT NULL, md5 text NOT NULL, created_at DATETIME, updated_at DATETIME );";
            cmd.ExecuteNonQuery();

            nTrans = 0;
            trans = conn.BeginTransaction();
        }

        ~Db()
        {
            this.Close();
        }
        public void Close()
        {
            if (trans != null)
            {
                trans.Commit();
                trans = null;
            }
            
            if (conn != null)
            {
                conn.Close();
                conn = null;
            }

        }

        public ArrayList top(int n)
        {
            ArrayList groups = new ArrayList();

            SQLiteDataReader datareader;
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"select size, md5, name from files where md5 in (SELECT md5 FROM files group by md5 having count(*)>1 order by size desc limit {n}) order by size desc, md5, name;";
            datareader = cmd.ExecuteReader();

            string last_md5 = "";
            int last_size = 0;
            ArrayList names = new ArrayList();
            ArrayList states = new ArrayList();

            string k;
            string[] v;
            while (datareader.Read())
            {
                string md5 = datareader.GetString(1);
                int size = datareader.GetInt32(0);
                string name = datareader.GetString(2);

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
                    groups.Add(new object[4] { last_md5, last_size, names.ToArray(), states.ToArray() });

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

        public Dictionary<string, object[]> LoadDict(string path)
        {
            Dictionary<string, object[]> dict = new Dictionary<string, object[]>();
            SQLiteCommand cmd = conn.CreateCommand();
            string pattern = path.ToLower();

            cmd.CommandText = $"select name, size, mtime, md5 from files where lower(name) like '{pattern}%' order by name;";
            SQLiteDataReader datareader = cmd.ExecuteReader();
            while (datareader.Read())
            {
                string name = datareader.GetString(0);
                long size = datareader.GetInt64(1);
                int mtime = datareader.GetInt32(2);
                string strHash = datareader.GetString(3);

                dict.Add(name, new object[] { size, mtime, strHash });
            }

            return dict;
        }

        public void Write(string type, string name, long size, int mtime, string md5)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            name = name.Replace("'", "''");
            cmd.CommandText = $"INSERT INTO files(type, name, mtime, size, md5, created_at, updated_at) VALUES('{type}','{name}',{mtime},{size},'{md5}',CURRENT_TIMESTAMP,CURRENT_TIMESTAMP);";
            cmd.ExecuteNonQuery();
            nTrans++;
            if (nTrans > commit_block)
            {
                trans.Commit();
                trans = conn.BeginTransaction();
                nTrans = 0;
            }
        }
        public void Commit()
        {
            trans.Commit();
            trans = conn.BeginTransaction();
            nTrans = 0;
        }
        public void Delete(string name)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            name = name.Replace("'", "''");
            cmd.CommandText = $"delete from files where name = '{name}';";
            cmd.ExecuteNonQuery();
            nTrans++;
            if (nTrans > commit_block)
            {
                trans.Commit();
                trans = conn.BeginTransaction();
                nTrans = 0;
            }
        }

    }
}
