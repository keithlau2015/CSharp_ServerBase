using System;

namespace Database
{
    public static class DatabaseFactory
    {
        public enum DatabaseType
        {
            MongoDB,
            EncryptedBinary,
            OptimizedEncryptedBinary
        }

        public static DatabaseBase CreateDatabase(DatabaseType type, string connectionString, string encryptionKey = null)
        {
            return type switch
            {
                DatabaseType.MongoDB => new MongoDBManager(connectionString),
                DatabaseType.EncryptedBinary => new EncryptedBinaryDBManager(connectionString, encryptionKey ?? "DefaultEncryptionKey"),
                DatabaseType.OptimizedEncryptedBinary => new OptimizedEncryptedDBManager(connectionString, encryptionKey ?? "DefaultEncryptionKey"),
                _ => throw new ArgumentException($"Unsupported database type: {type}")
            };
        }

        public static void SaveDatabaseOnShutdown(DatabaseBase database)
        {
            if (database is IPersistentDatabase persistentDb)
            {
                persistentDb.SaveAllDataToDisk();
            }
        }
    }
} 