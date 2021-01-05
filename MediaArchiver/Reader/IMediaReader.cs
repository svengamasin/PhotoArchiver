using System.Collections.Generic;
using System.IO;

namespace MediaArchiver
{
    public interface IMediaReader
    {
        IList<FileInfo> GetMediaFiles();
    }
}