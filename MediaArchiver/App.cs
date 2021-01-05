using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaArchiver.Storage;
using Serilog;

namespace MediaArchiver
{
    public class App
    {
        private readonly DirectoryInfo _targetDir;
        private readonly IHashStore _sourceHashStore;
        private readonly IHashStore _targetHashStore;
        private readonly IMediaReader _mediaReader;
        private readonly ILogger _logger;

        public App(DirectoryInfo targetDir, IHashStore sourceHashStore, IHashStore targetHashStore, IMediaReader mediaReader, ILogger logger)
        {
            _targetDir = targetDir;
            _sourceHashStore = sourceHashStore;
            _targetHashStore = targetHashStore;
            _mediaReader = mediaReader;
            _logger = logger;
        }

        public async Task Run()
        {
            var files = _mediaReader.GetMediaFiles();
            files.Select(x=> new MediaFile(x,_sourceHashStore,_targetHashStore,_targetDir,_logger))
                .ToList().ForEach(x=> x.ToArchive());
        }
    }
}
