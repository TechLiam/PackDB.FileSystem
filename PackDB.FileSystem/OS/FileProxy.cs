using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace PackDB.FileSystem.OS
{
    [ExcludeFromCodeCoverage]
    public class FileProxy : IFile
    {
        public Task<IStream> OpenWrite(string path)
        {
            var directory = path.Substring(0, path.LastIndexOf('\\'));
            Directory.CreateDirectory(directory);
            var stream = new StreamProxy(File.OpenWrite(path)) as IStream;
            return Task.FromResult(stream);
        }

        public Task<IStream> OpenRead(string path)
        {
            var stream = new StreamProxy(File.OpenRead(path)) as IStream;
            return Task.FromResult(stream);
        }

        public Task<bool> Exists(string path)
        {
            return Task.FromResult(File.Exists(path));
        }

        public Task Delete(string path)
        {
            File.Delete(path);
            return Task.CompletedTask;
        }

        public Task Move(string path, string destination)
        {
            File.Move(path, destination);
            return Task.CompletedTask;
        }
    }
}