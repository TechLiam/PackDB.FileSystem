using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PackDB.Core;

namespace PackDB.FileSystem.OS
{
    [ExcludeFromCodeCoverage]
    public class FileProxy : IFile
    {
        private readonly ILogger _logger;

        public FileProxy() : this(new EmptyLogger())
        {
        }
        
        public FileProxy(ILogger logger)
        {
            using (logger.BeginScope("{Operation}", nameof(FileProxy)))
            {
                _logger = logger;
                _logger.LogInformation("Created a file proxy");
            }
        }

        public Task<IStream> OpenWrite(string path)
        {
            using (_logger.BeginScope("{Operation} is {Action} {path}", nameof(FileProxy),
                "opening a write stream for", path))
            {
                var directory = path.Substring(0, path.LastIndexOf('\\'));
                Directory.CreateDirectory(directory);
                _logger.LogTrace("The file will be put in the the folder {folder}", directory);
                var stream = new StreamProxy(File.OpenWrite(path)) as IStream;
                _logger.LogInformation("Opened a write stream for the file");
                return Task.FromResult(stream);
            }
        }

        public Task<IStream> OpenRead(string path)
        {
            using (_logger.BeginScope("{Operation} is {Action} {path}", nameof(FileProxy),
                "opening a read stream for", path))
            {
                var stream = new StreamProxy(File.OpenRead(path)) as IStream;
                _logger.LogInformation("Opened a read stream for the file");
                return Task.FromResult(stream);
            }
        }

        public Task<bool> Exists(string path)
        {
            using (_logger.BeginScope("{Operation} is {Action} {path}", nameof(FileProxy),
                "checking if there is a file at", path))
            {
                var result = File.Exists(path);
                _logger.LogInformation(result ? "The file exists" : "The file doesn't exist");
                return Task.FromResult(result);
            }
        }

        public Task Delete(string path)
        {
            using (_logger.BeginScope("{Operation} is {Action} {path}", nameof(FileProxy),
                "deleting file at", path))
            {
                File.Delete(path);
                _logger.LogInformation("Deleted the file");
                return Task.CompletedTask;
            }
        }

        public Task Move(string path, string destination)
        {
            using (_logger.BeginScope("{Operation} is {Action} {path} to {destination}", nameof(FileProxy),
                "moving file from", path, destination))
            {
                File.Move(path, destination);
                _logger.LogInformation("Moved the file");
                return Task.CompletedTask;
            }
        }
    }
}