using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using MediaArchiver.Storage;
using Serilog;
using Serilog.Core;

namespace MediaArchiver
{
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