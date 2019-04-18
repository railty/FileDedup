using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
namespace fdd
{
    public class Doc
    {
        private int BlockSize = 16 * 512;
        private DateTime epoch = new DateTime(1970, 1, 1);

        protected string name;
        protected long size;
        protected int mtime;
        protected string hash;
        
        public Doc(string path)
        {
            this.name = path;

            hash = cal_hash(path);
        }

        public string Name
        {
            get { return this.name; }
        }
        public long Size
        {
            get { return this.size; }
        }
        public long MTime
        {
            get { return this.mtime; }
        }
        public string Hash
        {
            get { return this.hash; }
        }

        /*read everything*/
        private string full_pass(string fname, long file_size)
        {
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
            fs.Close();
            md5.TransformFinalBlock(dat, 0, 0);
            byte[] hash = md5.Hash;
            return md5hex(hash);
        }

        /*
        * read 0
        * read 1 2 4 8 ... until over the size
        */
        private string one_pass(string fname, long file_size)
        {
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
            fs.Close();
            md5.TransformFinalBlock(dat, 0, 0);
            byte[] hash = md5.Hash;
            return md5hex(hash);
        }
        /*
        1 pass and 1 pass reverse
        */
        private string two_pass(string fname, long file_size)
        {
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
                fs.Seek(unique_blocks[block] * BlockSize, SeekOrigin.Begin);
                int len = fs.Read(dat, 0, BlockSize);
                md5.TransformBlock(dat, 0, len, dat, 0);
            }
            fs.Close();
            md5.TransformFinalBlock(dat, 0, 0);
            byte[] hash = md5.Hash;
            return md5hex(hash);

        }
        protected virtual string cal_hash(string path, int option=2)
        {
            FileInfo fi = new FileInfo(path);
            size = fi.Length;
            mtime = (int)(fi.LastWriteTimeUtc - epoch).TotalSeconds;

            if (size == 0) hash = "";
            else
            {
                if (option == 1) hash = one_pass(path, size);
                else if (option == 2) hash = two_pass(path, size);
                else hash = full_pass(path, size);
            }
            return hash;
        }

        public static string md5hex(byte[] hash)
        {
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sBuilder.Append(hash[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

    }
}
