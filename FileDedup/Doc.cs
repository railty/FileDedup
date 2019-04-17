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
        int BlockSize = 16 * 512;

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
        public string cal_file(string fname, int option=2)
        {
            long file_size = (new FileInfo(fname)).Length;
            if (file_size == 0) return "";

            if (option == 1) return one_pass(fname, file_size);
            if (option == 2) return two_pass(fname, file_size);
            return full_pass(fname, file_size);
        }

        public string md5hex(byte[] hash)
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
