using System.Threading;

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

        public abstract void CRUD_Config<T>(Action action, string dbName, T obj);
        public abstract void CRUD_Instance<T>(Action action, string dbName, T obj);
        public abstract void CRUD_Log<T>(Action action, string dbName, T obj);
        protected abstract void GenericeCURD<T>(Action action, string dbName, string key, T obj);
    }
}
