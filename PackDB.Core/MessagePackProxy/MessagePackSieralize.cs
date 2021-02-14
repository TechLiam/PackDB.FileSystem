using System.Diagnostics.CodeAnalysis;
using System.IO;
using MessagePack;

namespace PackDB.Core.MessagePackProxy
{
    [ExcludeFromCodeCoverage]
    public class MessagePackSerializer : IMessagePackSerializer
    {
        public MessagePackSerializer()
        {
            Options = MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4Block);
        }

        private MessagePackSerializerOptions Options { get; }

        public void Serialize<TDataType>(Stream stream, TDataType data)
        {
            MessagePack.MessagePackSerializer.Serialize(stream, data, Options);
        }

        public TDataType Deserialize<TDataType>(Stream stream)
        {
            return MessagePack.MessagePackSerializer.Deserialize<TDataType>(stream, Options);
        }
    }
}