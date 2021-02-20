using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
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

        public Task Serialize<TDataType>(Stream stream, TDataType data)
        {
            return MessagePack.MessagePackSerializer.SerializeAsync(stream, data, Options);
        }

        public async ValueTask<TDataType> Deserialize<TDataType>(Stream stream)
        {
            return await MessagePack.MessagePackSerializer.DeserializeAsync<TDataType>(stream, Options);
        }
    }
}