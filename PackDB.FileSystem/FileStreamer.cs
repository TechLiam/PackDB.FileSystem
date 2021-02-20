using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using PackDB.Core.Locks;
using PackDB.Core.MessagePackProxy;
using PackDB.FileSystem.OS;

namespace PackDB.FileSystem
{
    public class FileStreamer : IFileStreamer
    {
        private readonly IDirectory _directory;
        private readonly IFile _file;

        private readonly IDictionary<string, ISemaphore> _fileLocks = new Dictionary<string, ISemaphore>();
        private readonly IDictionary<string, IStream> _fileStreams = new Dictionary<string, IStream>();
        private readonly TimeSpan _lockWaitTimeout = new TimeSpan(0, 0, 1, 0, 0);
        private readonly IMessagePackSerializer _messagePackSerializer;
        private readonly ISemaphoreFactory _semaphoreFactory;

        [ExcludeFromCodeCoverage]
        public FileStreamer()
        {
            _messagePackSerializer = new MessagePackSerializer();
            _file = new FileProxy();
            _directory = new DirectoryProxy();
            _semaphoreFactory = new SemaphoreFactory();
        }

        public FileStreamer(IMessagePackSerializer messagePackSerializer, IFile file,
            ISemaphoreFactory semaphoreFactory, IDirectory directory)
        {
            _messagePackSerializer = messagePackSerializer;
            _file = file;
            _directory = directory;
            _semaphoreFactory = semaphoreFactory;
        }

        public Task<bool> GetLockForFile(string filename)
        {
            lock (_fileLocks)
            {
                if (!_fileLocks.ContainsKey(filename)) _fileLocks.Add(filename, _semaphoreFactory.Create(1, 1));
                return Task.FromResult(_fileLocks[filename].Wait(_lockWaitTimeout));
            }
        }

        public Task UnlockFile(string filename)
        {
            lock (_fileLocks)
            {
                if (!_fileLocks.ContainsKey(filename)) return Task.CompletedTask;
                _fileLocks[filename].Release();
            }
            return Task.CompletedTask;
        }

        public async Task<bool> WriteDataToStream<TDataType>(string filename, TDataType data)
        {
            if (!_fileStreams.ContainsKey(filename)) _fileStreams.Add(filename, await _file.OpenWrite(filename));
            await _messagePackSerializer.Serialize(_fileStreams[filename].GetStream(), data);
            return true;
        }

        public async Task<TDataType> ReadDataFromStream<TDataType>(string filename)
        {
            if (!_fileStreams.ContainsKey(filename)) _fileStreams.Add(filename, await _file.OpenRead(filename));
            var result = await _messagePackSerializer.Deserialize<TDataType>(_fileStreams[filename].GetStream());
            await CloseStream(filename);
            return result;
        }

        public async Task<bool> CloseStream(string filename)
        {
            if (!_fileStreams.ContainsKey(filename)) return false;
            _fileStreams[filename].Close();
            await DisposeOfStream(filename);
            return true;
        }

        public async Task DisposeOfStream(string filename)
        {
            if (!_fileStreams.ContainsKey(filename)) return;
            await _fileStreams[filename].Dispose();
            _fileStreams.Remove(filename);
        }

        public Task<bool> Exists(string filename)
        {
            return _file.Exists(filename);
        }

        public async Task<bool> Delete(string filename)
        {
            await _file.Delete(filename);
            return true;
        }

        public async Task<bool> SoftDelete(string filename)
        {
            await _file.Move(filename, filename + ".deleted");
            return true;
        }

        public async Task<bool> Undelete(string filename)
        {
            if (await _file.Exists(filename + ".deleted"))
            {
                await _file.Move(filename + ".deleted", filename);
            }
            return true;
        }

        public string[] GetAllFileNames(string folder, string fileExtension)
        {
            return _directory.GetFiles(folder, fileExtension)
                .Select(x => x.Split("\\")
                    .Last()
                    .Split(".")
                    .First()
                )
                .ToArray();
        }
    }
}