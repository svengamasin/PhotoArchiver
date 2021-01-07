using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaArchiver;
using MediaArchiver.Storage;
using Serilog;
using Serilog.Core;

namespace MediaArchiverTests
{
    public class TestDataSetup : IDisposable
    {
        private readonly ILogger _logger;
        private DirectoryInfo _dbsDir;

        public TestDataSetup(ILogger logger)
        {
            _logger = logger;

            var currentDir = new DirectoryInfo(Environment.CurrentDirectory);
            SourceDir = currentDir.CreateSubdirectory("source");
            _dbsDir = currentDir.CreateSubdirectory("dbs");

            using (var reader = new FileStream("testimage.jpg", FileMode.OpenOrCreate,FileAccess.Read))
            {
                using (Stream s = File.Create(Path.Combine(SourceDir.FullName, "testimage.jpg")))
                {
                    reader.CopyTo(s);
                }

            }

            SourceHashDb = new DbHashStore("SourceMedia", Path.Combine(_dbsDir.FullName,"source.db"),_logger);
            TargetHashDb = new DbHashStore("TargetMedia", Path.Combine(_dbsDir.FullName, "target.db"), _logger);
            TargetDir = currentDir.CreateSubdirectory("target");
        }

        public List<MediaFile> GetMediaFiles(Logger logger)
        {
            var mediaDir = new MediaArchiver.MediaReader(SourceDir, logger);
            var files = mediaDir.GetMediaFiles();
            return files.Select(x => new MediaFile(x, SourceHashDb, TargetHashDb,
                TargetDir, logger)).ToList();
        }

        public List<MediaFile> GetMediaFilesFast(Logger logger)
        {
            var mediaDir = new MediaArchiver.FastMediaReader(SourceHashDb,SourceDir, logger);
            var files = mediaDir.GetMediaFiles();
            return files.Select(x => new MediaFile(x, SourceHashDb, TargetHashDb,
                TargetDir, logger)).ToList();
        }

        public DirectoryInfo SourceDir { get; set; }

        public DirectoryInfo TargetDir { get; set; }

        public DbHashStore TargetHashDb { get; set; }

        public DbHashStore SourceHashDb { get; set; }

        public void Dispose()
        {
            //close dbs
            SourceHashDb.Dispose();
            TargetHashDb.Dispose();
            TargetDir.Delete(true);

            SourceDir.Delete(true);
            _dbsDir.Delete(true);
            GC.Collect(); GC.WaitForPendingFinalizers();
        }

    }
}