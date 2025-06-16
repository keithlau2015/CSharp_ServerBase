using System.Threading;
using System.Threading.Tasks;

namespace Database
{
    public abstract class DatabaseBase
    {
        public enum Action : int
        {
            Create = 1,
            Read = 2,
            Update = 3,
            Delete = 4
        }

        public abstract Task CRUD_Config<T>(Action action, string dbName, T obj);
        public abstract Task CRUD_Instance<T>(Action action, string dbName, T obj);
        public abstract Task CRUD_Log<T>(Action action, string dbName, T obj);
        protected abstract Task GenericeCURDAsync<T>(Action action, string dbName, string key, T obj);
    }

    public interface IPersistentDatabase
    {
        void SaveAllDataToDisk();
    }
}
