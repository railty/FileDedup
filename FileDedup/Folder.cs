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
    public class Folder
    {
        private DateTime epoch = new DateTime(1970, 1, 1);
        public string[] filters;
        Doc doc = new Doc();
        public Folder(string[] filters=null)
        {
            if (filters == null) filters = new string[]{@"$RECYCLE\.BIN", "System Volume Information", "Recovery", "Thumbs.db", @"wasteland\.fdd" };
            this.filters = filters;
        }

        public void cal_folder(string path, ref long size, ref int mtime, ref string strHash, ref int folders_count, ref int files_count, ref int files_cached_count)
        {

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

                        strHash = doc.cal_file(p);

                        size = size + file_size;
                        byte[] bytesHash = Encoding.ASCII.GetBytes(strHash);
                        md5.TransformBlock(bytesHash, 0, bytesHash.Length, bytesHash, 0);
                        files_count++;
                    }
                }
            }
            byte[] bytes = new byte[0];
            md5.TransformFinalBlock(bytes, 0, 0);
            strHash = doc.md5hex(md5.Hash);

            mtime = (int)((new FileInfo(path)).LastWriteTimeUtc - epoch).TotalSeconds;
        }
    }
}
