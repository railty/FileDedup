using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace fdd
{
    public class HashTest
    {
        static string text = "Hello World!!!";
        static string zipFile = @"..\..\testdata.zip";
        static string dataPath = @"..\..\data";

        string fSmall = $@"{dataPath}\small.txt";
        string hashSmall = "7b75fb712727b98ad06edc65a20ae524";

        string fBig = $@"{dataPath}\big.txt";
        string hashBig = "2cfc69a5c268e65e811fec91d846d5f8";

        string fHuge = $@"{dataPath}\huge.txt";
        string hashHuge = "575b6fd970c3b84e6f31a9699b6a15a2";

        private readonly ITestOutputHelper output;

        public HashTest(ITestOutputHelper output)
        {
            this.output = output;
        }
        ~HashTest()
        {
        }

        private void CreateFiles()
        {
            string str = Repeat(text, 10000 * 1);
            StreamWriter sw = new StreamWriter(fSmall);
            for (int i = 0; i < 10; i++) sw.WriteLine(str);
            sw.Close();

            sw = new StreamWriter(fBig);
            for (int i = 0; i < 100; i++) sw.WriteLine(str);
            sw.Close();

            sw = new StreamWriter(fHuge);
            for (int i = 0; i < 1000; i++) sw.WriteLine(str);
            sw.Close();
        }
        private void SetupFixture()
        {
            ZipFile.ExtractToDirectory(zipFile, dataPath);
        }
        private void TeardownFixture()
        {
            Directory.Delete(dataPath, true);
        }


        private void DeleteFiles()
        {
            if (File.Exists(fSmall)) File.Delete(fSmall);
            if (File.Exists(fBig)) File.Delete(fBig);
            if (File.Exists(fHuge)) File.Delete(fHuge);
        }


        public string Repeat(string value, int count)
        {
            return new StringBuilder(value.Length * count).Insert(0, value, count).ToString();
        }

        [Fact]
        public void FileTest()
        {
            output.WriteLine("start file testing...");
            try
            {
                SetupFixture();
                Doc doc = new Doc();
                string hash;

                hash = doc.cal_file(fSmall);
                output.WriteLine($"small hash = {hash}");
                Assert.Equal(hash, hashSmall);

                hash = doc.cal_file(fBig);
                output.WriteLine($"big hash = {hash}");
                Assert.Equal(hash, hashBig);

                hash = doc.cal_file(fHuge);
                output.WriteLine($"huge hash = {hash}");
                Assert.Equal(hash, hashHuge);
            }
            finally
            {
                TeardownFixture();
            }
        }

        [Fact]
        public void FolderTest()
        {
            output.WriteLine("start folder testing...");
            try
            {
                SetupFixture();
                Folder fd = new Folder();

                string strHash = "";
                long file_size = 0;
                int mtime = 0;
                int folders_count = 0;
                int files_count = 0;
                int files_cached_count = 0;

                fd.cal_folder($@"{dataPath}\folder1", ref file_size, ref mtime, ref strHash, ref folders_count, ref files_count, ref files_cached_count);
                output.WriteLine($"folder1 hash = {strHash}");
                Assert.Equal("ffc876d8e9536f8e67decd38ceb2fcd3", strHash);

                fd.cal_folder($@"{dataPath}\folder2", ref file_size, ref mtime, ref strHash, ref folders_count, ref files_count, ref files_cached_count);
                output.WriteLine($"folder2 hash = {strHash}");
                Assert.Equal("5edad417092e0513f558183db7ce5dd3", strHash);

                fd.cal_folder($@"{dataPath}\folder3", ref file_size, ref mtime, ref strHash, ref folders_count, ref files_count, ref files_cached_count);
                output.WriteLine($"folder3 hash = {strHash}");
                Assert.Equal("2d178bb45f756f3765b188ce3a396341", strHash);
            }
            finally
            {
                TeardownFixture();
            }
        }

    }
}