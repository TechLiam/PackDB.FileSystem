using System.IO;

namespace PackDB.Core.MessagePackProxy
{
    public interface IMessagePackSerializer
    {
        void Serialize<TDataType>(Stream stream, TDataType data);
        TDataType Deserialize<TDataType>(Stream stream);
    }
}