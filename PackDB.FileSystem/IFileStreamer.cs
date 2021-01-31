using System.IO;
using System.Threading;
using PackDB.Core.Data;

namespace PackDB.FileSystem
{
    public interface IFileStreamer
    {
        bool GetLockForFile(string filename);
        void UnlockFile(string filename);
        bool WriteDataToStream<TDataType>(string filename, TDataType data) where TDataType : DataEntity;
        TDataType ReadDataFromStream<TDataType>(string filename) where TDataType : DataEntity;
        bool CloseStream(string filename);
        void DisposeOfStream(string filename);
        bool Exists(string filename);
        bool Delete(string filename);
        bool SoftDelete(string filename);
        bool Undelete(string filename);
    }
}