
using MongoDB.Driver;

namespace DataBase
{
    public class MongoDBManager : DatabaseBase
    {
        private MongoClient dbClient;
        
        public async async MongoDB(string url)
        {
            var settings = MongoClientSettings.FromConnectionString(url);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            dbClient = new MongoClient(settings);
            var database = client.GetDatabase("test");            

            var cursor = await dbClient.ListDatabaseAsync();
            await cursor.ForEachAsync(db => DebugUtility.DebugLog(db.Name));
        }


        public override async void CRUD<T>(Action action, T obj)
        {
            
        }
    }
}