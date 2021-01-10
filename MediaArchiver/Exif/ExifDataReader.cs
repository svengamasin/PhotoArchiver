using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using MetadataExtractor;
using Serilog;
using Directory = MetadataExtractor.Directory;

namespace MediaArchiver.Exif
{
    public class ExifDataReader
    {
        private readonly FileInfo _mediaFile;
        private readonly ILogger _logger;

        public ExifDataReader(FileInfo mediaFile, ILogger logger)
        {
            _mediaFile = mediaFile;
            _logger = logger;
        }
        public IReadOnlyList<Directory> GetInfos()
        {
            Console.WriteLine(_mediaFile.Name);
            return ImageMetadataReader.ReadMetadata(_mediaFile.FullName);
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
                dateTimes.Add(_mediaFile.LastWriteTime);
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
            // stupid last value, if all other things fail...
            return new DateTime(1990,1,1);
        }

 

    }

    public class JsonConfiguration
    {
        public readonly FileInfo ConfigFileInfo;
        public readonly DirectoryInfo ConfigDirectorInfo;
        public JsonElement ConfigurationDocument { get; private set; }

        public JsonConfiguration(DirectoryInfo directoryInfo, string configFileName = "")
        {
            ConfigFileInfo = configFileName == "" ? directoryInfo.GetFiles("*.json").FirstOrDefault() : 
            new FileInfo(Path.Combine(directoryInfo.FullName, configFileName));
            ConfigDirectorInfo = directoryInfo;
            InitializeConfigurationDictionary();
        }

        private void InitializeConfigurationDictionary()
        {
            var jsonBytes = File.ReadAllBytes(ConfigDirectorInfo.FullName);
            using var jsonDoc = JsonDocument.Parse(jsonBytes);
            ConfigurationDocument = jsonDoc.RootElement;
        }
    }
}