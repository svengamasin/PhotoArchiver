using System;
using System.IO;
using System.Linq;
using MediaArchiver;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace MediaArchiverTests
{
    public class ArchiveTests : IDisposable
    {
        private Logger _logger;
        private TestDataSetup _testdata;

        public ArchiveTests(ITestOutputHelper output)
        {
            _logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo
                .TestOutput(output, LogEventLevel.Information).CreateLogger();
            _testdata = new TestDataSetup(_logger);
        }

        [Fact]
        public void CopyingMediaFileToArchiveIsCorrect()
        {
            _logger.Information("Copying file to target.");
            var mediaFile = _testdata.GetMediaFiles(_logger).First();
            mediaFile.ToArchive();
            // directory "2020/12" created?
            var testTargetDir = new DirectoryInfo(Path.Combine(new[]
            {
                _testdata.TargetDir.FullName,
                "2020",
                "12"
            }));
            Assert.True(testTargetDir.Exists);
            // mediafile was copied and renamed?
            Assert.Contains(testTargetDir.GetFiles(),
                x => x.Name.StartsWith("20201224"));
        }

        [Fact]
        public void CorrectSequenceRenamingTest()
        {
            var mediaFiles = _testdata.GetMediaFiles(_logger);
            MediaFileCopier copier;
            for (int i = 0; i < 10; i++)
            {
                copier = new MediaFileCopier(_testdata.TargetDir,mediaFiles.First(),_logger);
                copier.Copy();
            }
            var testTargetDir = new DirectoryInfo(Path.Combine(new[]
            {
                _testdata.TargetDir.FullName,
                "2020",
                "12"
            }));
            Assert.True(testTargetDir.Exists);
            Assert.True(testTargetDir.GetFiles().Length == 10);
            Assert.True(
                testTargetDir.GetFiles().Where(x => x.NameWithoutExtension().Contains("_")).ToList().Count == 10);
        }

        [Fact]
        public void EqualFilesMd5WillNotBeCopiedToArchive()
        {
            //Arrange sample data (equal files)
            var mediaFile = _testdata.GetMediaFiles(_logger).First();
            for (int i = 0; i < 10; i++)
            {
                mediaFile.MediaFileInfo.CopyTo(
                    $"{Path.Combine(mediaFile.MediaFileInfo.Directory.FullName, mediaFile.MediaFileInfo.NameWithoutExtension())}_{i:000}{mediaFile.MediaFileInfo.Extension}");
            }

            var mediaFiles = new MediaArchiver.MediaReader(_testdata.SourceDir, _logger).GetMediaFiles().Select(x=> new MediaFile(x,_testdata.SourceHashDb,
                _testdata.TargetHashDb, _testdata.TargetDir, _logger)).ToList();
            // Test
            mediaFiles.ForEach(x=> x.ToArchive());

            var targetDir = new MediaFileCopier(_testdata.TargetDir,mediaFiles.First(),_logger).CreateOrGetTargetDirectory();
            Assert.True(_testdata.SourceDir.GetFiles().Length == 11);
            Assert.True(mediaFiles.All(x=> x.Md5Hash.Length>5));
            Assert.True(targetDir.GetFiles().Length==1);
        }


        public void Dispose()
        {
            _logger?.Dispose();
            _testdata.Dispose();
            GC.Collect(); GC.WaitForPendingFinalizers();
        }
    }
}