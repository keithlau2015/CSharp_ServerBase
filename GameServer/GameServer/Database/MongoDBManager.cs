using MongoDB.Driver;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Database
{
    public class MongoDBManager : DatabaseBase
    {
        private MongoClient dbClient;
        private readonly string connectionString;

        public MongoDBManager(string url)
        {
            connectionString = url ?? throw new ArgumentNullException(nameof(url));
            
            try
            {
                var settings = MongoClientSettings.FromConnectionString(url);
                settings.ServerApi = new ServerApi(ServerApiVersion.V1);
                settings.ConnectTimeout = TimeSpan.FromSeconds(10);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
                dbClient = new MongoClient(settings);

                // Test connection
                _ = dbClient.ListDatabaseNames();
                Debug.DebugUtility.DebugLog("MongoDB connection established successfully");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to connect to MongoDB: {ex.Message}");
                throw;
            }
        }

        public override async Task CRUD_Instance<T>(Action action, string dbName, T obj)
        {
            await GenericeCURDAsync<T>(action, dbName, "UID", obj);
        }

        public override async Task CRUD_Config<T>(Action action, string dbName, T obj)
        {
            await GenericeCURDAsync<T>(action, dbName, "ID", obj);
        }

        public override async Task CRUD_Log<T>(Action action, string dbName, T obj)
        {
            await GenericeCURDAsync<T>(action, dbName, "UID", obj);
        }

        protected override async Task GenericeCURDAsync<T>(Action action, string dbName, string keyField, T obj)
        {
            if (obj == null)
            {
                Debug.DebugUtility.ErrorLog("CRUD operation failed: object is NULL");
                return;
            }

            if (string.IsNullOrEmpty(dbName))
            {
                Debug.DebugUtility.ErrorLog("CRUD operation failed: database name is null or empty");
                return;
            }

            try
            {
                IMongoDatabase db = dbClient.GetDatabase(dbName);
                IMongoCollection<T> collection = db.GetCollection<T>(typeof(T).Name);

                var keyProperty = typeof(T).GetProperty(keyField);
                if (keyProperty == null)
                {
                    Debug.DebugUtility.ErrorLog($"CRUD operation failed: Property '{keyField}' not found on type {typeof(T).Name}");
                    return;
                }

                var keyValue = keyProperty.GetValue(obj);
                if (keyValue == null)
                {
                    Debug.DebugUtility.ErrorLog($"CRUD operation failed: Key field '{keyField}' value is null");
                    return;
                }

                // Create a proper filter expression
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, keyField);
                var constant = Expression.Constant(keyValue);
                var equality = Expression.Equal(property, constant);
                var filter = Expression.Lambda<Func<T, bool>>(equality, parameter);

                switch (action)
                {
                    case Action.Create:
                        await collection.InsertOneAsync(obj);
                        Debug.DebugUtility.DebugLog($"Created document in {typeof(T).Name} collection");
                        break;

                    case Action.Read:
                        var readResult = await collection.Find(filter).FirstOrDefaultAsync();
                        if (readResult != null)
                        {
                            // Copy properties from readResult to obj
                            CopyProperties(readResult, obj);
                            Debug.DebugUtility.DebugLog($"Read document from {typeof(T).Name} collection");
                        }
                        else
                        {
                            Debug.DebugUtility.WarningLog($"No document found in {typeof(T).Name} collection with {keyField}={keyValue}");
                        }
                        break;

                    case Action.Update:
                        var updateResult = await collection.ReplaceOneAsync(filter, obj);
                        if (updateResult.ModifiedCount > 0)
                        {
                            Debug.DebugUtility.DebugLog($"Updated document in {typeof(T).Name} collection");
                        }
                        else
                        {
                            Debug.DebugUtility.WarningLog($"No document updated in {typeof(T).Name} collection with {keyField}={keyValue}");
                        }
                        break;

                    case Action.Delete:
                        var deleteResult = await collection.DeleteOneAsync(filter);
                        if (deleteResult.DeletedCount > 0)
                        {
                            Debug.DebugUtility.DebugLog($"Deleted document from {typeof(T).Name} collection");
                        }
                        else
                        {
                            Debug.DebugUtility.WarningLog($"No document deleted from {typeof(T).Name} collection with {keyField}={keyValue}");
                        }
                        break;

                    default:
                        Debug.DebugUtility.ErrorLog($"Unknown CRUD action: {action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"CRUD operation failed: {ex.Message}");
                throw;
            }
        }

        // Helper method to copy properties from source to destination
        private void CopyProperties<T>(T source, T destination)
        {
            if (source == null || destination == null)
                return;

            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    var value = property.GetValue(source);
                    property.SetValue(destination, value);
                }
            }
        }

        // Additional helper methods for common operations
        public async Task<List<T>> GetAllAsync<T>(string dbName)
        {
            try
            {
                IMongoDatabase db = dbClient.GetDatabase(dbName);
                IMongoCollection<T> collection = db.GetCollection<T>(typeof(T).Name);
                return await collection.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to get all documents: {ex.Message}");
                return new List<T>();
            }
        }

        public async Task<T> GetByIdAsync<T>(string dbName, string keyField, object keyValue)
        {
            try
            {
                IMongoDatabase db = dbClient.GetDatabase(dbName);
                IMongoCollection<T> collection = db.GetCollection<T>(typeof(T).Name);

                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, keyField);
                var constant = Expression.Constant(keyValue);
                var equality = Expression.Equal(property, constant);
                var filter = Expression.Lambda<Func<T, bool>>(equality, parameter);

                return await collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to get document by ID: {ex.Message}");
                return default(T);
            }
        }

        public void Dispose()
        {
            dbClient = null;
        }
    }
}
