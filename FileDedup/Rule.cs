using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fdd
{
    class Rule
    {
        string hi;
        string low;
        public Rule(string hi, string low)
        {
            this.low = low;
            this.hi = hi;
        }

        public string[] filter(object[] files, ref string hi_file)
        {
            bool HasHiPath = false;
            bool HasLowPath = false;
            for (int i = 0; i < files.Length; i++)
            {
                string f = (string)files[i];
                if (f.StartsWith(hi))
                {
                    HasHiPath = true;
                }
                else
                {
                    HasLowPath = true;
                }
            }

            if (HasHiPath && HasLowPath)
            {
                List<string> LowFiles = new List<string>();
                for (int i = 0; i < files.Length; i++)
                {
                    string f = (string)files[i];
                    if (f.StartsWith(low))
                    {
                        LowFiles.Add(f);
                    }
                    if (f.StartsWith(hi))
                    {
                        hi_file = f;
                    }
                }
                return LowFiles.ToArray();
            }
            return null;
        }

    }
}
