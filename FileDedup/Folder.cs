using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace fdd
{
    public class Folder : Doc
    {
        private int folders_count;
        private int files_count;
        public int Folders
        {
            get { return this.folders_count; }
        }
        public int Files
        {
            get { return this.files_count; }
        }

        public Folder(string path) : base(path)
        {
        }

        private DateTime epoch = new DateTime(1970, 1, 1);
        public string[] filters;

        protected override string cal_hash(string path, int option = 2)
        {
            string[] filters = new string[] { @"$RECYCLE\.BIN", "System Volume Information", "Recovery", "Thumbs.db", @"wasteland\.fdd" };
            string filter = String.Join("|", filters);

            var files = Directory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
            List<string> names = new List<string>();
            foreach (string file in files)
            {
                if (!Regex.Match(file, $@"[A-Z]\:\\\{filter}").Success) names.Add(file);
            }
            names.Sort();

            MD5 md5 = new MD5CryptoServiceProvider();
            for (int i = 0; i < names.Count; i++)
            {
                string p = (string)names[i];
                FileInfo fi = new FileInfo(p);

                if (fi.Attributes.HasFlag(FileAttributes.Directory))
                {
                    Folder fd = new Folder(p);

                    folders_count += fd.Folders;
                    files_count += fd.Files;

                    size = size + fd.Size;
                    byte[] hash_bytes = Encoding.ASCII.GetBytes(fd.Hash);
                    md5.TransformBlock(hash_bytes, 0, hash_bytes.Length, hash_bytes, 0);

                    folders_count++;
                }
                else
                {
                    long file_size = fi.Length;
                    if (file_size > 0)
                    {
                        Doc doc = new Doc(p);
                        size = size + file_size;
                        byte[] hash_bytes = Encoding.ASCII.GetBytes(doc.Hash);
                        md5.TransformBlock(hash_bytes, 0, hash_bytes.Length, hash_bytes, 0);
                        files_count++;
                    }
                }
            }
            byte[] bytes = new byte[0];
            md5.TransformFinalBlock(bytes, 0, 0);
            hash = Doc.md5hex(md5.Hash);
            return hash;
        }
    }
}
