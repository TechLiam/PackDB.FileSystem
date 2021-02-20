using System.IO;
using System.Threading.Tasks;

namespace PackDB.FileSystem.OS
{
    public interface IStream
    {
        Stream GetStream();
        void Close();
        ValueTask Dispose();
    }
}