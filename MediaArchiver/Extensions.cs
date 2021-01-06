using System.IO;
using System.Linq;
using MediaArchiver.Storage;
using Serilog;

namespace MediaArchiver
{
    public static class FileInfoExtensions
    {
        public static string NameWithoutExtension(this FileInfo fi)
        {
            return Path.GetFileNameWithoutExtension(fi.Name);
        }
    }

    public static class MediaReaderExtensions
    {
        public static void Run(this MediaReader mediaReader, IHashStore sourceStore, IHashStore targetStore,
            DirectoryInfo targetDir, ILogger logger)
        {
            var files = mediaReader.GetMediaFiles();
            foreach (var mediaFile in files.Select(x =>
                new MediaFile(x, sourceStore, targetStore, targetDir, logger)
            ))
            {
                mediaFile.ToArchive();
            }
        }

    }

    public static class HashStoreExtensions
    {
        public static bool TryAdd(this IHashStore hashStore, MediaFile mediaFile)
        {
            return hashStore.TryAdd(mediaFile.MediaFileInfo.FullName, mediaFile.Md5Hash);
        }

        public static bool TryRemove(this IHashStore hashStore, MediaFile mediaFile)
        {
            return hashStore.TryRemove(mediaFile.MediaFileInfo.FullName);
        }

        public static bool TryPurge(this IHashStore hashStore, MediaFile mediaFile)
        {
            return hashStore.TryRemove(mediaFile.MediaFileInfo.FullName) &&
                   hashStore.TryDeleteHash(mediaFile.Md5Hash);
        }
    }

}