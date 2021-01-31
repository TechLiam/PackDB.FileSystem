﻿using System.Diagnostics.CodeAnalysis;
using System.IO;
using MessagePack;
using PackDB.Core.Data;

namespace PackDB.Core.MessagePackProxy
{
    [ExcludeFromCodeCoverage]
    public class MessagePackSerializer : IMessagePackSerializer
    {

        private MessagePackSerializerOptions Options { get; }
        
        public MessagePackSerializer()
        {
            Options = MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4Block);
        }
        
        public void Serialize<TDataType>(Stream stream, TDataType data)
        {
            MessagePack.MessagePackSerializer.Serialize(stream,data,Options);
        }

        public TDataType Deserialize<TDataType>(Stream stream)
        {
            return MessagePack.MessagePackSerializer.Deserialize<TDataType>(stream,Options);
        }
        
    }
}