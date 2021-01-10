using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaArchiver;
using MediaArchiver.Exif;
using MetadataExtractor;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace MediaArchiverTests
{
    public class ExifTests : IDisposable
    {
        private Logger _logger;
        private TestDataSetup _testdata;

        public ExifTests(ITestOutputHelper output)
        {
            _logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo
                .TestOutput(output, LogEventLevel.Information).CreateLogger();
            _testdata = new TestDataSetup(_logger);
        }

        [Fact]
        public void RecordingTimeFromExifIsCorrect()
        {
            var testMediaFile = _testdata.GetMediaFiles(_logger).First();
            var exifSearcher = new ExifDataReader(testMediaFile.MediaFileInfo,_logger);
            var result = exifSearcher.GetInfos();
            var dateTags = exifSearcher.GetAllDateTags();
            var bestGuess = exifSearcher.GetBestGuessRecordingDateTime();
            Assert.True(bestGuess.Date.Equals(new DateTime(2020,12,24)));
        }

        [Fact]
        public void ShouldWorkIfExifDataIsEmpty()
        {
            var testMediaFile = _testdata.GetMediaFiles(_logger).First();
            var exifDataMock = new Mock<ExifDataReader>(testMediaFile.MediaFileInfo,_logger);
            exifDataMock.Setup(m => m.GetAllDateTags()).Returns(new List<Tag>());
            //var exifSearcher = new ExifData(testMediaFile);
            Assert.True(exifDataMock.Object.GetBestGuessRecordingDateTime().Day > 0 );
        }

        [Fact]
        public void WriteToExifAndReadAgain()
        {
            var testMediaFile = _testdata.GetMediaFiles(_logger).First();
            var exifWrite = new ExifDataWriter(testMediaFile.MediaFileInfo, _logger);
            var newFullName = exifWrite.SetTagAndDescription("#meinTag","meineBeschreibung").SaveAs("blubb.jpg");

            var exifRead = new ExifDataReader(new FileInfo(newFullName), _logger );
            var userCommentTags = exifRead.GetInfos().SelectMany(x => x.Tags)
                .Where(x => x.HasName && x.Name.Contains("User Comment")).ToList();
            Assert.True(userCommentTags.First().Description.Contains("meineBeschreibung"));
        }


        public void Dispose()
        {
            _logger?.Dispose();
            _testdata.Dispose();
            GC.Collect(); GC.WaitForPendingFinalizers();
        }
    }
}
