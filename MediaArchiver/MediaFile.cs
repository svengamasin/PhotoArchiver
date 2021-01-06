using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using MediaArchiver.Storage;
using MetadataExtractor;
using Serilog;
using Serilog.Core;
using Directory = MetadataExtractor.Directory;

namespace MediaArchiver
{
    public class ExifData
    {
        private readonly MediaFile _mediaFile;
        private readonly ILogger _logger;

        public ExifData(MediaFile mediaFile, ILogger logger)
        {
            _mediaFile = mediaFile;
            _logger = logger;
        }
        public IReadOnlyList<Directory> GetInfos()
        {
            Console.WriteLine(_mediaFile.MediaFileInfo.Name);
            return ImageMetadataReader.ReadMetadata(_mediaFile.MediaFileInfo.FullName);
        }

        public virtual List<Tag> GetAllDateTags()
        {
            var dirs = GetInfos();
            var tagsWithDates = dirs.SelectMany(x => x.Tags)
                .Where(x => x.HasName && x.Name.ToLowerInvariant().Contains("date")).ToList();
            return tagsWithDates;
        }

        public DateTime GetBestGuessRecordingDateTime()
        {
            try
            {
                var originDateTimes = GetAllDateTags();
                var dateTimes = originDateTimes.Select(x => x.Description).Where(x => !x?.Equals(string.Empty) ?? false)
                    .Select(date =>
                    {
                        var date2Parse = date;
                        DateTime dateTimeTaken;
                        var exifParsingSuccessful = DateTime.TryParseExact(date2Parse, "yyyy:MM:dd HH:mm:ss",
                            CultureInfo.CurrentCulture,
                            DateTimeStyles.None, out dateTimeTaken);
                        if (!exifParsingSuccessful)
                            DateTime.TryParse(date2Parse, out dateTimeTaken);
                        return dateTimeTaken;
                    }).ToList();

                // last chance: last write access time if no exif data is available
                dateTimes.Add(_mediaFile.MediaFileInfo.LastWriteTime);
                // delete all (false) timestamps that are older than any digicam :-)
                dateTimes = dateTimes.Where(x => x > new DateTime(1990, 1, 1)).ToList();

                // if datetimes are different, the date (day) with the most occurrences is taken. Then, at that day, the oldest one.
                var bestGuessCreationTime = dateTimes.GroupBy(x => x.Date.Date).OrderByDescending(x => x.Count())
                    .First().ToList()
                    .OrderBy(x => x).First();
                return bestGuessCreationTime;
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
            }
            return new DateTime(1990,1,1);
        }


}
    public class MediaFile : IDisposable
    {
        private string _md5Hash;
        public readonly FileInfo MediaFileInfo;
        private readonly IHashStore _sourceStore;
        private readonly IHashStore _targetStore;
        private readonly DirectoryInfo _targetDirectory;
        private readonly ILogger _logger;
        public string Md5Hash => _md5Hash == String.Empty ? CalculateMd5() : _md5Hash;

        public MediaFile(FileInfo fileInfo, IHashStore sourceStore, IHashStore targetStore, DirectoryInfo targetDirectory, ILogger logger)
        {
            MediaFileInfo = fileInfo;
            _sourceStore = sourceStore;
            _targetStore = targetStore;
            _targetDirectory = targetDirectory;
            _logger = logger;
            _md5Hash = String.Empty;
        }

        private string CalculateMd5()
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(MediaFileInfo.FullName))
                {
                    var hash = md5.ComputeHash(stream);
                    _md5Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            _logger.Information($"Calculated md5 ({_md5Hash}) for {MediaFileInfo.Name}");
            return _md5Hash;
        }



        public void ToArchive()
        {
            AddToSourceStore();
            CopyToArchive();
        }

        private void CopyToArchive()
        {
            try
            {
                if (_targetStore.TryAdd(this)) 
                    new MediaFileCopier(_targetDirectory,this,_logger).Copy();
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
                _sourceStore.TryPurge(this);
                _targetStore.TryPurge(this);
            }
        }

        private void AddToSourceStore()
        {
            try
            {
                _sourceStore.TryAdd(this);
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
                _sourceStore.TryPurge(this);
            }
        }

        public void Dispose()
        {
            

        }
    }
}