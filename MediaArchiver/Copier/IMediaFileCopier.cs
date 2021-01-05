using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;

namespace MediaArchiver
{
    /// <summary>
    /// Copies given Mediafile to archive
    /// Renames based on rules and renames in archive for a sequence if necessary
    /// </summary>
    public interface IMediaFileCopier
    {
        DirectoryInfo CreateOrGetTargetDirectory();
        IList<FileInfo> GetSequenceOfTouchedFiles();
        void Copy();
    }
}
