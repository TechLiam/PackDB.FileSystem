using System.Threading.Tasks;

namespace PackDB.FileSystem.OS
{
    public interface IFile
    {
        Task<IStream> OpenWrite(string path);
        Task<IStream> OpenRead(string path);
        Task<bool> Exists(string path);
        Task Delete(string path);
        Task Move(string path, string destination);
    }
}