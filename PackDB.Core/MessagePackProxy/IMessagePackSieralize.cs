using System.IO;
using System.Threading.Tasks;

namespace PackDB.Core.MessagePackProxy
{
    public interface IMessagePackSerializer
    {
        Task Serialize<TDataType>(Stream stream, TDataType data);
        ValueTask<TDataType> Deserialize<TDataType>(Stream stream);
    }
}