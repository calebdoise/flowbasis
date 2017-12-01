using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FlowBasis.Expressions.Extensions
{
    public class FileSystemExpressionCallable : IExpressionCallable
    {
        private Func<string> basePathProvider;

        public FileSystemExpressionCallable(Func<string> basePathProvider)
        {
            this.basePathProvider = basePathProvider;
        }

        public object EvaluateCall(object[] args)
        {
            if (args.Length == 1)
            {
                string filename = args[0] as string;
                if (filename != null)
                {
                    string fullPath = this.EvaluateFullFilePath(filename);
                    return new FileReferenceMemberProvider(fullPath);
                }
            }

            return null;
        }

        private string EvaluateFullFilePath(string path)
        {
            string fullPath;
            if (Path.IsPathRooted(path))
            {
                fullPath = path;
            }
            else
            {
                string basePath = this.basePathProvider();
                fullPath = Path.Combine(basePath, path);
            }

            return fullPath;
        }
    }

    public class FileReferenceMemberProvider : IExpressionMemberProvider
    {
        private string fullPath;

        public FileReferenceMemberProvider(string fullPath)
        {
            this.fullPath = fullPath;
        }

        public object EvaluateMember(string name)
        {
            switch (name)
            {
                case "exists":
                    {
                        return File.Exists(this.fullPath);
                    }

                case "firstLine":
                    {
                        string firstLine = this.GetFileFirstLine();
                        return firstLine;
                    }

                case "firstLineIfExists":
                    {
                        if (File.Exists(this.fullPath))
                        {
                            string firstLine = this.GetFileFirstLine();
                            return firstLine;
                        }
                        else
                        {
                            return null;
                        }
                    }

                case "allText":
                    {
                        string allText = this.GetFileAllText();
                        return allText;
                    }

                case "allTextIfExists":
                    {
                        if (File.Exists(this.fullPath))
                        {
                            string allText = this.GetFileAllText();
                            return allText;
                        }
                        else
                        {
                            return null;
                        }
                    }

                default:
                    {
                        throw new Exception($"Undefined member: {name}");
                    }
            }
        }

        private string GetFileFirstLine()
        {
            using (var fs = OpenFileForSharedRead())
            using (var reader = new StreamReader(fs))
            {
                string line = reader.ReadLine();
                return line;
            }
        }

        private string GetFileAllText()
        {
            using (var fs = OpenFileForSharedRead())
            using (var reader = new StreamReader(fs))
            {
                string allText = reader.ReadToEnd();
                return allText;
            }
        }

        private FileStream OpenFileForSharedRead()
        {            
            return new FileStream(this.fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }
}
