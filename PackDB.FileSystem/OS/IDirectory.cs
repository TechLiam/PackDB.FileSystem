using System.Collections.Generic;

namespace PackDB.FileSystem.OS
{
    public interface IDirectory
    {
        string[] GetFiles(string path, string fileExtension);
    }
}