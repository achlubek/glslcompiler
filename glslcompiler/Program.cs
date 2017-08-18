using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace glslcompiler
{
    class Program
    {
        static Dictionary<string, string> FilesMap;

        static void ScanDir(string dir)
        {
            string[] dirs = Directory.GetDirectories(dir);
            string[] files = Directory.GetFiles(dir);
            if(FilesMap == null)FilesMap = new Dictionary<string, string>();
            foreach (var file in files)
            {
                FilesMap[Path.GetFileName(file)] = dir + "/" + Path.GetFileName(file);
            }
            foreach (var d in dirs)
            {
                ScanDir(d);
            }
        }

        static string MD5(string input)
        {
            byte[] hash = System.Security.Cryptography.MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        static string ResolveIncludes(string file)
        {
            string[] contents = File.ReadAllLines(file);

            string header = "#include ";
            string guard = "#pragma once";
            bool encloseInGuard = false;
            for(int i=0;i<contents.Length;i++)
            {
                if(contents[i].StartsWith(header))
                {
                    string include = contents[i].Substring(header.Length);
                    contents[i] = ResolveIncludes(FilesMap[include]);
                }
                else if (contents[i].Contains(guard)) {
                    contents[i] = "";
                    encloseInGuard = true;
                }
            }
            string result = string.Join("\n", contents);
            if (encloseInGuard)
            {
                string guid = MD5(file);
                result = "#ifndef " + guid + "\n#define " + guid + "\n" + result + "\n#endif";
            }
            return result;
        }

        static string Prepare(string file)
        {
            if (!Directory.Exists("tmp")) Directory.CreateDirectory("tmp");
            string newfile = "tmp/" + Path.GetFileNameWithoutExtension(file) + ".tmp" + Path.GetExtension(file);

            string code = ResolveIncludes(file);
            File.WriteAllText(newfile, code);

            return newfile;
        }

        static void Main(string[] args)
        {
            //glslangValidator.exe
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
            ScanDir(Directory.GetCurrentDirectory());
            foreach (string file in files)
            {
                if (file.EndsWith(".frag") || file.EndsWith(".vert"))
                {
                    Console.WriteLine("Compiling " + file + " to " + file + ".spv");
                    string tmp = Prepare(file);
                    var pinfo = new ProcessStartInfo("glslangValidator.exe", "-V " + tmp + " -o " + file + ".spv");
                    pinfo.CreateNoWindow = true;
                    pinfo.UseShellExecute = false;
                    pinfo.RedirectStandardOutput = true;
                    var p = Process.Start(pinfo);
                    string o = p.StandardOutput.ReadToEnd();
                    Console.WriteLine(o);
                    Console.WriteLine();
                }
            }
        }
    }
}
