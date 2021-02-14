namespace PackDB.FileSystem.OS
{
    public interface IFile
    {
        IStream OpenWrite(string path);
        IStream OpenRead(string path);
        bool Exists(string path);
        void Delete(string path);
        void Move(string path, string destination);
    }
}