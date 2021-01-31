using System.IO;
using PackDB.Core.Data;

namespace PackDB.Core.MessagePackProxy
{
    public interface IMessagePackSerializer
    {
        void Serialize<TDataType>(Stream stream, TDataType data) where TDataType : DataEntity;
        TDataType Deserialize<TDataType>(Stream stream) where TDataType : DataEntity;
    }
}