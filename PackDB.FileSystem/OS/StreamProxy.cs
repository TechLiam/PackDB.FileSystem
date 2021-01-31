using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace PackDB.FileSystem.OS
{
    [ExcludeFromCodeCoverage]
    public abstract class StreamProxy : FileStream, IStream
    {
        public Stream GetStream()
        {
            return this;
        }

        public StreamProxy(SafeFileHandle handle, FileAccess access) : base(handle, access)
        {
        }

        public StreamProxy(SafeFileHandle handle, FileAccess access, int bufferSize) : base(handle, access, bufferSize)
        {
        }

        public StreamProxy(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync) : base(handle, access, bufferSize, isAsync)
        {
        }

#pragma warning disable 618
        public StreamProxy(IntPtr handle, FileAccess access) : base(handle, access)
#pragma warning restore 618
        {
        }
#pragma warning disable 618
        public StreamProxy(IntPtr handle, FileAccess access, bool ownsHandle) : base(handle, access, ownsHandle)
#pragma warning restore 618
        {
        }
#pragma warning disable 618
        public StreamProxy(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize) : base(handle, access, ownsHandle, bufferSize)
#pragma warning disable 618
        {
        }
#pragma warning disable 618
        public StreamProxy(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize, bool isAsync) : base(handle, access, ownsHandle, bufferSize, isAsync)
#pragma warning disable 618
        {
        }

        public StreamProxy(string path, FileMode mode) : base(path, mode)
        {
        }

        public StreamProxy(string path, FileMode mode, FileAccess access) : base(path, mode, access)
        {
        }

        public StreamProxy(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
        {
        }

        public StreamProxy(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize) : base(path, mode, access, share, bufferSize)
        {
        }

        public StreamProxy(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) : base(path, mode, access, share, bufferSize, useAsync)
        {
        }

        public StreamProxy(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options) : base(path, mode, access, share, bufferSize, options)
        {
        }
    }
}