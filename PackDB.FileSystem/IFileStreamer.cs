using System.Collections.Generic;
using System.Threading.Tasks;

namespace PackDB.FileSystem
{
    public interface IFileStreamer
    {
        Task<bool> GetLockForFile(string filename);
        Task UnlockFile(string filename);
        Task<bool> WriteDataToStream<TDataType>(string filename, TDataType data);
        Task<TDataType> ReadDataFromStream<TDataType>(string filename);
        Task<bool> CloseStream(string filename);
        Task DisposeOfStream(string filename);
        Task<bool> Exists(string filename);
        Task<bool> Delete(string filename);
        Task<bool> SoftDelete(string filename);
        Task<bool> Undelete(string filename);
        string[] GetAllFileNames(string folder, string fileExtension);
    }
}