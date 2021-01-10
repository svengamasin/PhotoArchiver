using System.IO;
using ExifLibrary;
using Serilog;

namespace MediaArchiver.Exif
{
    public class ExifDataWriter
    {
        private readonly FileInfo _mediaFile;
        private readonly ILogger _logger;
        private ImageFile _imageFile;

        public ExifDataWriter(FileInfo mediaFile, ILogger logger)
        {
            _mediaFile = mediaFile;
            _logger = logger;
            _imageFile = ImageFile.FromFile(_mediaFile.FullName);
        }

        public ExifDataWriter SetTagAndDescription(string tag, string description, bool overwrite = true)
        {
            var value = overwrite ? $"{tag}, {description}" :
                _imageFile.Properties.Get<ExifEncodedString>(ExifTag.UserComment) + $"{tag}, {description}";
            _imageFile.Properties.Set(ExifTag.UserComment, value);
            return this;
        }

        public void Save()
        {
            _imageFile.Save(_mediaFile.FullName);
        }

        public void SaveAs(FileInfo fileInfo)
        {
            _imageFile.Save(fileInfo.FullName);
        }

        public string SaveAs(string fileName)
        {
            var newFullName = Path.Combine(Path.GetDirectoryName(_mediaFile.FullName), fileName);
            _imageFile.Save(newFullName);
            return newFullName;
        }


    }
}