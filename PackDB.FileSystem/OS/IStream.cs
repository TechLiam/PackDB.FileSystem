using System.IO;

namespace PackDB.FileSystem.OS
{
    public interface IStream
    {
        Stream GetStream();
        void Close();
        void Dispose();
    }
}