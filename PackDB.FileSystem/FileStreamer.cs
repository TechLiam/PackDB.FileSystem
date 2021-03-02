using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PackDB.Core;
using PackDB.Core.Locks;
using PackDB.Core.MessagePackProxy;
using PackDB.FileSystem.OS;

namespace PackDB.FileSystem
{
    public class FileStreamer : IFileStreamer
    {
        private readonly IDirectory _directory;
        private readonly IFile _file;
        private readonly ILogger _logger;

        private readonly IDictionary<string, ISemaphore> _fileLocks = new Dictionary<string, ISemaphore>();
        private readonly IDictionary<string, IStream> _fileStreams = new Dictionary<string, IStream>();
        private readonly TimeSpan _lockWaitTimeout = new TimeSpan(0, 0, 1, 0, 0);
        private readonly IMessagePackSerializer _messagePackSerializer;
        private readonly ISemaphoreFactory _semaphoreFactory;

        [ExcludeFromCodeCoverage]
        public FileStreamer() : this(new EmptyLogger())
        {
        }
        
        [ExcludeFromCodeCoverage]
        public FileStreamer(ILogger logger) : this(new MessagePackSerializer(),new FileProxy(logger), new SemaphoreFactory(), new DirectoryProxy(), logger)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileStreamer(IMessagePackSerializer messagePackSerializer, IFile file, ISemaphoreFactory semaphoreFactory, IDirectory directory) : this(messagePackSerializer,file,semaphoreFactory,directory,new EmptyLogger())
        {
        }
        
        public FileStreamer(IMessagePackSerializer messagePackSerializer, IFile file, ISemaphoreFactory semaphoreFactory, IDirectory directory, ILogger logger)
        {
            using (logger.BeginScope("{Operation}", nameof(FileStreamer)))
            {
                _messagePackSerializer = messagePackSerializer;
                _file = file;
                _semaphoreFactory = semaphoreFactory;
                _directory = directory;
                _logger = logger;
                _logger.LogInformation("Created file streamer");
            }
        }

        public Task<bool> GetLockForFile(string filename)
        {
            using (_logger.BeginScope("{Operation} is {Action} {filename}", nameof(FileStreamer), "getting a lock for the file", filename))
            {
                lock (_fileLocks)
                {
                    if (!_fileLocks.ContainsKey(filename))
                    {
                        _fileLocks.Add(filename, _semaphoreFactory.Create(1, 1));
                        _logger.LogTrace("Created new semaphore for file");
                    }
                    var result = Task.FromResult(_fileLocks[filename].Wait(_lockWaitTimeout));
                    _logger.LogInformation("Got lock for file");
                    return result;
                }
            }
        }

        public Task UnlockFile(string filename)
        {
            using (_logger.BeginScope("{Operation} is {Action} {filename}", nameof(FileStreamer),
                "unlocking file", filename))
            {
                lock (_fileLocks)
                {
                    if (!_fileLocks.ContainsKey(filename))
                    {
                        _logger.LogWarning("There is no lock for the file");
                        return Task.CompletedTask;
                    }
                    _fileLocks[filename].Release();
                    _logger.LogInformation("Released semaphore for file");
                }
                return Task.CompletedTask;
            }
        }

        public async Task<bool> WriteDataToStream<TDataType>(string filename, TDataType data)
        {
            using (_logger.BeginScope("{Operation} is {Action} {filename}", nameof(FileStreamer),
                "writing data to file", filename))
            {
                if (!_fileStreams.ContainsKey(filename))
                {
                    _logger.LogTrace("Getting new file stream");
                    _fileStreams.Add(filename, await _file.OpenWrite(filename));
                }
                await _messagePackSerializer.Serialize(_fileStreams[filename].GetStream(), data);
                _logger.LogInformation("Data written to file stream");
                return true;
            }
        }

        public async Task<TDataType> ReadDataFromStream<TDataType>(string filename)
        {
            using (_logger.BeginScope("{Operation} is {Action} {filename}", nameof(FileStreamer),
                "reading data from file", filename))
            {
                if (!_fileStreams.ContainsKey(filename))
                {
                    _logger.LogTrace("Getting new file stream");
                    _fileStreams.Add(filename, await _file.OpenRead(filename));
                }
                var result = await _messagePackSerializer.Deserialize<TDataType>(_fileStreams[filename].GetStream());
                _logger.LogTrace("Read and deserialized data from file stream");
                await CloseStream(filename);
                _logger.LogInformation("Read data from stream");
                return result;
            }
        }

        public async Task<bool> CloseStream(string filename)
        {
            using (_logger.BeginScope("{Operation} is {Action} {filename}", nameof(FileStreamer),
                "closing file stream for", filename))
            {
                if (!_fileStreams.ContainsKey(filename))
                {
                    _logger.LogWarning("There is no file stream to close");
                    return false;
                }
                _fileStreams[filename].Close();
                _logger.LogTrace("File stream closed");
                await DisposeOfStream(filename);
                _logger.LogInformation("Fully closed file stream");
                return true;
            }
        }

        public async Task DisposeOfStream(string filename)
        {
            using (_logger.BeginScope("{Operation} is {Action} {filename}", nameof(FileStreamer),
                "disposing of file stream for", filename))
            {
                if (!_fileStreams.ContainsKey(filename))
                {
                    _logger.LogWarning("There is no file stream to dispose of");
                    return;
                }
                await _fileStreams[filename].Dispose();
                _logger.LogTrace("Disposed of file stream");
                _fileStreams.Remove(filename);
                _logger.LogInformation("Fully disposed of file stream");
            }
        }

        public Task<bool> Exists(string filename)
        {
            using (_logger.BeginScope("{Operation} is {Action} {filename}", nameof(FileStreamer),
                "checking if file exists for", filename))
            {
                var result = _file.Exists(filename);
                _logger.LogInformation("The result of checking if a file exists is {result}", result);
                return result;
            }
        }

        public async Task<bool> Delete(string filename)
        {
            using (_logger.BeginScope("{Operation} is {Action} {filename}", nameof(FileStreamer),
                "deleting file", filename))
            {
                await _file.Delete(filename);
                _logger.LogInformation("Deleted file");
                return true;
            }
        }

        public async Task<bool> SoftDelete(string filename)
        {
            using (_logger.BeginScope("{Operation} is {Action} {filename}", nameof(FileStreamer),
                "soft deleting file", filename))
            {
                await _file.Move(filename, filename + ".deleted");
                _logger.LogInformation("Soft deleted file");
                return true;
            }
        }

        public async Task<bool> Undelete(string filename)
        {
            using (_logger.BeginScope("{Operation} is {Action} {filename}", nameof(FileStreamer),
                "undeleting file", filename))
            {
                if (await _file.Exists(filename + ".deleted"))
                {
                    await _file.Move(filename + ".deleted", filename);
                    _logger.LogInformation("Undeleted file");
                    return true;
                }
                _logger.LogWarning("File is not soft deleted");
                return true;
            }
        }

        public string[] GetAllFileNames(string folder, string fileExtension)
        {
            using (_logger.BeginScope("{Operation} is {Action} {folder} with extension of {extension}", nameof(FileStreamer),
                "getting names of files in folder", folder, fileExtension))
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
}