using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace PackDB.FileSystem.OS
{
    [ExcludeFromCodeCoverage]
    public class FileProxy : IFile
    {
        public IStream OpenWrite(string path)
        {
            return (StreamProxy) File.OpenWrite(path);
        }

        public IStream OpenRead(string path)
        {
            return (StreamProxy) File.OpenRead(path);
        }

        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public void Move(string path, string destination)
        {
            File.Move(path,destination);
        }
    }
}