using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace PackDB.FileSystem.OS
{
    [ExcludeFromCodeCoverage]
    public class DirectoryProxy : IDirectory
    {
        public string[] GetFiles(string path, string fileExtension)
        {
            return Directory.Exists(path) ? Directory.GetFiles(path, "*." + fileExtension, SearchOption.TopDirectoryOnly) : new string[0];
        }
    }
}