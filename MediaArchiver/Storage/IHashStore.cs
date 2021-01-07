

using System.Threading.Tasks;

namespace MediaArchiver.Storage
{
    public interface IHashStore
    {
        bool TryAdd(string fullFileName, string md5Hash);
        bool Exists(string fullFileName);
        bool HashFound(string md5Hash);
        bool TryRemove(string fullFileName);
        bool TryDeleteHash(string md5Hash);
    }
}