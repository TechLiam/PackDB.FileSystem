namespace PackDB.FileSystem.OS
{
    public interface IDirectory
    {
        string[] GetFiles(string path, string fileExtension);
    }
}