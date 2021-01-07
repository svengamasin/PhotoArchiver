using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaArchiver.Storage;
using Serilog;

namespace MediaArchiver
{
    public class FastMediaReader : MediaReader
    {
        private IHashStore _sourceDb;

        public FastMediaReader(IHashStore sourceDb, DirectoryInfo sourceDirInfo, ILogger logger, IList<string> extensionFilters = null, bool recursive = true) : base(sourceDirInfo, logger, extensionFilters, recursive)
        {
            _sourceDb = sourceDb;
        }
        /// <summary>
        /// Returns only the new added files. Those are not element in sourceDb yet.
        /// </summary>
        /// <returns></returns>
        public override IList<FileInfo> GetMediaFiles()
        {
            var files = base.GetMediaFiles();
            var filesForProcessing = files.Where(x => !_sourceDb.Exists(x.FullName)).AsParallel().ToList();
            Logger.Information($"Got media files for fast processing: {filesForProcessing.Count}");
            return filesForProcessing;
        }
    }
}