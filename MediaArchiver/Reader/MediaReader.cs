using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using MediaArchiver.Storage;
using Serilog;

namespace MediaArchiver
{
    public class MediaReader : IMediaReader
    {
        private IList<string> _extensionFilters;
        private bool _recursiveScanning;
        private DirectoryInfo _sourceDirInfo;
        private DirectoryInfo _targetDirInfo;
        private readonly IHashStore _sourceStore;
        private readonly IHashStore _targetStore;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceDirInfo">This is the source directory to be scanned.</param>
        /// <param name="targetDirInfo">This is the target (archive) directory</param>
        /// <param name="sourceStore"></param>
        /// <param name="targetStore"></param>
        /// <param name="extensionFilters">e.g. .mp3, .mp4, .mov .....</param>
        /// <param name="recursive">If this is true (default), all subdirectories will be scanned recursively.</param>
        public MediaReader(DirectoryInfo sourceDirInfo, IList<string> extensionFilters=null, bool recursive = true)
        {
            _sourceDirInfo = sourceDirInfo;
            _extensionFilters = extensionFilters ?? new List<string> {".jpg", ".mp4", ".raw", ".mkv"};
            _recursiveScanning = recursive;
        }

        public virtual IList<FileInfo> GetMediaFiles()
        {
            List<FileInfo> files;
            if (!_recursiveScanning)
                files = _sourceDirInfo.GetFiles().ToList();
            else
            {
                files = Directory.GetFiles(_sourceDirInfo.FullName, "*.*", SearchOption.AllDirectories)
                    .Select(filename => new FileInfo(filename)).ToList();
            }

            return files.Where(f => _extensionFilters.Contains(f.Extension.ToLowerInvariant())).ToList();
        }

    }
}