﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiztinGUIsh.loadsave
{
    abstract class ProjectSerializer
    {
        public const string Watermark = "DiztinGUIsh";

        public abstract byte[] Save(Project project);
        public abstract Project Load(byte[] data);

        public void SaveToFile(Project project, string filename)
        {
            File.WriteAllBytes(filename, Save(project));
        }
        
        public Project LoadFromFile(string filename)
        {
            return Load(File.ReadAllBytes(filename));
        }

        protected static void DebugVerifyProjectEquality(Project project1, Project project2, bool deepcut = true)
        {
            if (deepcut)
            {
                for (int i = 0; i < project1.Data.RomBytes.Count; ++i)
                {
                    Debug.Assert(project1.Data.RomBytes[i].Equals(project2.Data.RomBytes[i]));
                }

                Debug.Assert(project1.Data.RomBytes.Equals(project2.Data.RomBytes));
                Debug.Assert(project1.Data.Equals(project2.Data));
            }
            Debug.Assert(project1.Equals(project2));
        }
    }
}
