using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Serilog;

namespace MediaArchiver.Storage
{
    public class DbHashStore : IHashStore,IDisposable
    {
        private readonly ILogger _logger;
        private LiteDatabase _db;
        private ILiteCollection<BsonDocument> _collection;

        public DbHashStore(string collectionName, string dbFileFullName, ILogger logger)
        {
            _logger = logger;
            _logger.Information($"Creating db: {dbFileFullName} / Collectionname: {collectionName}");
            _db = new LiteDatabase(dbFileFullName);
            _collection = _db.GetCollection<BsonDocument>(collectionName);
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            _collection?.EnsureIndex(x => x["FullName"]);
            _collection?.EnsureIndex(x => x["Md5Hash"]);
        }
        
        public bool TryAdd(string fullFileName, string md5Hash)
        {
            if (HashFound(md5Hash)) return false;
            _collection.Insert(BsonFrom(fullFileName,md5Hash));
            return true;
        }

        private BsonDocument BsonFrom(string fullFileName, string md5Hash)
        {
            var doc = new BsonDocument
            {
                ["_id"] = ObjectId.NewObjectId(),
                ["FullName"] = fullFileName,
                ["Md5Hash"] = md5Hash
            };
            return doc;
        }

        public bool HashFound(string md5Hash)
        {
            return _collection.FindOne(x => x["Md5Hash"] == md5Hash) != null;
        }

        public bool Exists(string fullFileName)
        {
            return _collection.FindOne(x => x["FullName"] == fullFileName) != null;
        }

        public bool TryDeleteHash(string md5Hash)
        {
            if (HashFound(md5Hash))
            {
                var count = _collection.DeleteMany(x => x["Md5Hash"] == md5Hash);
                return count > 0 ? true : false;
            }

            return false;
        }

        public bool TryRemove(string fullFileName)
        {
            if (Exists(fullFileName))
            {
                var count = _collection.DeleteMany(x => x["FullName"] == fullFileName);
                return count > 0 ? true : false;
            }

            return false;
        }

        public void Dispose()
        {
            _collection = null;
            _db?.Dispose();
        }
    }
}