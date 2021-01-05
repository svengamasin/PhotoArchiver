using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaArchiver;
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
            var exifSearcher = new ExifData(testMediaFile);
            var result = exifSearcher.GetInfos();
            var dateTags = exifSearcher.GetAllDateTags();
            var bestGuess = exifSearcher.GetBestGuessRecordingDateTime();
            Assert.True(bestGuess.Date.Equals(new DateTime(2020,12,24)));
        }


        public void Dispose()
        {
            _logger?.Dispose();
            _testdata.Dispose();
            GC.Collect(); GC.WaitForPendingFinalizers();
        }
    }
}
