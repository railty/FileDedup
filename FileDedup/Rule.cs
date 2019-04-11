using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fdd
{
    class Rule
    {
        string PriorityPath;
        public Rule(string PriorityPath)
        {
            this.PriorityPath = PriorityPath;
        }

        public string[] filter(object[] files)
        {
            bool HasPriorityPath = false;
            bool HasNonPriorityPath = false;
            for (int i = 0; i < files.Length; i++)
            {
                string f = (string)files[i];
                if (f.StartsWith(PriorityPath))
                {
                    HasPriorityPath = true;
                }
                else
                {
                    HasNonPriorityPath = true;
                }
            }

            if (HasPriorityPath && HasNonPriorityPath)
            {
                List<string> NonPriorityFiles = new List<string>();
                for (int i = 0; i < files.Length; i++)
                {
                    string f = (string)files[i];
                    if (!f.StartsWith(PriorityPath))
                    {
                        NonPriorityFiles.Add(f);
                    }
                }
                return NonPriorityFiles.ToArray();
            }
            return null;
        }

    }
}
