using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using Serilog.Core;

namespace MediaArchiver
{
    public class MediaFileCopier : IMediaFileCopier
    {
        private readonly DirectoryInfo _targetDirectoryInfo;
        private readonly MediaFile _mediaFile;
        private readonly ILogger _logger;
        private ExifData _exifData;
        private List<FileInfo> _sequence;

        public MediaFileCopier(DirectoryInfo targetDirectoryInfo, MediaFile mediaFile, ILogger logger)
        {
            _targetDirectoryInfo = targetDirectoryInfo;
            _mediaFile = mediaFile;
            _logger = logger;
            _exifData = new ExifData(_mediaFile);
            _sequence = new List<FileInfo>();
        }

        private (string, string) GetRecordingMonthAndYear()
        {
            // get recording datetime
            var recordingDateTime = _exifData.GetBestGuessRecordingDateTime();
            var recordingYear = $"{recordingDateTime.Year:0000}";
            var recordingMonth = $"{recordingDateTime.Month:00}";
            return (recordingMonth, recordingYear);
        }


        public DirectoryInfo CreateOrGetTargetDirectory()
        {
            var (month, year) = GetRecordingMonthAndYear();
            _logger.Information("CreateOrGetTargetDirectory");
            object lockObj = new object();
            bool lockWasTaken = false;
            DirectoryInfo targetDirYearMonth;
            try
            {
                System.Threading.Monitor.Enter(lockObj, ref lockWasTaken);
                
                // get/create needed subdirectories
                var targetDirYear = !_targetDirectoryInfo.EnumerateDirectories(year).Any() ? 
                    _targetDirectoryInfo.CreateSubdirectory(year) : 
                    new DirectoryInfo(Path.Combine(_targetDirectoryInfo.FullName,year));
                targetDirYearMonth = !targetDirYear.EnumerateDirectories(month).Any()
                    ? targetDirYear.CreateSubdirectory(month)
                    : new DirectoryInfo(Path.Combine(targetDirYear.FullName,month));
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(lockObj);
            }

            return targetDirYearMonth;
        }




        private FileInfo GetArchiveFileInfo()
        {
            var recordingDateTime = _exifData.GetBestGuessRecordingDateTime();
            var targetDir = CreateOrGetTargetDirectory();
            // create new filename
            var filename =
                $"{recordingDateTime.Year}{recordingDateTime.Month:00}{recordingDateTime.Day:00}_" +
                $"{recordingDateTime.Hour:00}{recordingDateTime.Minute:00}{recordingDateTime.Second:00}_" +
                $"{recordingDateTime.Millisecond:00}{_mediaFile.MediaFileInfo.Extension}";

            var targetFileInfo = RenameInArchiveIfSequence(new FileInfo(Path.Combine(targetDir.FullName,filename)));
            _logger.Information($"TargetFile is: {targetFileInfo.FullName}");
            return targetFileInfo;
        }

        private FileInfo RenameInArchiveIfSequence (FileInfo targetFileInfo)
        {
            var sequenceFiles = targetFileInfo.Directory.GetFiles($"{targetFileInfo.NameWithoutExtension()}*");
            
            if (sequenceFiles.Length == 0) 
                // no sequence found
                return targetFileInfo;
            //sequence found
            _logger.Information($"Sequence found for: {targetFileInfo.FullName}");
            var i = 0;
            //renaming of existing sequence
            foreach (var file in sequenceFiles.OrderBy(x=> x.Name))
            {
                var newNameInSequence = $"{targetFileInfo.NameWithoutExtension()}_{++i:000}{targetFileInfo.Extension}";
                if (!file.Name.Equals(newNameInSequence)) file.MoveTo(Path.Combine(targetFileInfo.Directory.FullName,newNameInSequence));
            }
            // set sequence of renamed files
            _sequence = targetFileInfo.Directory.GetFiles($"{targetFileInfo.NameWithoutExtension()}*").OrderBy(x=> x.Name).ToList();

            //return filename of new file in sequence
            return new FileInfo($"{Path.Combine(targetFileInfo.Directory.FullName,targetFileInfo.NameWithoutExtension())}_{++i}{targetFileInfo.Extension}");
        }

        public bool RenamingInArchiveWasNecessary()
        {
            return _sequence.Any();
        }

        public IList<FileInfo> GetSequenceOfTouchedFiles()
        {
            return _sequence;
        }

        public void Copy()
        {
            _mediaFile.MediaFileInfo.CopyTo(GetArchiveFileInfo().FullName);
        }

    }
}