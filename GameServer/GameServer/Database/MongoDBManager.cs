
using MongoDB.Driver;
using System.Collections;
using System.Collections.Generic;

namespace Database
{
    public class MongoDBManager : DatabaseBase
    {
        private MongoClient dbClient;
        public MongoDBManager(string url)
        {
            var settings = MongoClientSettings.FromConnectionString(url);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            dbClient = new MongoClient(settings);
        }

        public override void CRUD_Instance<T>(Action action, string dbName, T obj)
        {
            GenericeCURD<T>(action, dbName, "UID", obj);
        }

        public override void CRUD_Config<T>(Action action, string dbName, T obj)
        {
            GenericeCURD<T>(action, dbName, "ID", obj);
        }

        public override void CRUD_Log<T>(Action action, string dbName, T obj)
        {
            GenericeCURD<T>(action, dbName, "UID", obj);
        }

        protected override async void GenericeCURD<T>(Action action, string dbName, string keyField, T obj)
        {
            if (obj == null)
                Debug.DebugUtility.ErrorLog("obj is NULL");

            IMongoDatabase db = dbClient.GetDatabase(dbName);
            if (db == null)
            {
                Debug.DebugUtility.ErrorLog($"CURD Invalid DB Name[{dbName}], action {action}");
                return;
            }

            IMongoCollection<T> collection = db.GetCollection<T>(typeof(T).Name);
            if (collection == null)
            {
                Debug.DebugUtility.ErrorLog($"CRUD Invalid Collection[{typeof(T).Name}], action {action}");
                return;
            }

            if (action == Action.Create)
            {
                await collection.InsertOneAsync(obj);
            }
            else if (action == Action.Delete)
            {
                await collection.FindOneAndDeleteAsync(o => typeof(T).GetProperty(keyField).GetValue(o).Equals(typeof(T).GetProperty(keyField).GetValue(obj)));
            }
            else if (action == Action.Read)
            {
                var findTask = await collection.FindAsync(o => typeof(T).GetProperty(keyField).GetValue(o).Equals(typeof(T).GetProperty(keyField).GetValue(obj)));
                obj = findTask.FirstOrDefault();
            }
            else if (action == Action.Update)
            {
                await collection.FindOneAndReplaceAsync<T>(o => typeof(T).GetProperty(keyField).GetValue(o).Equals(typeof(T).GetProperty(keyField).GetValue(obj)), obj);
            }
            else
            {
                Debug.DebugUtility.ErrorLog("unknow action");
            }
        }
    }
}
