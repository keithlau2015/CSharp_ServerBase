using System.Threading;
using System.Threading.Tasks;

namespace Database
{
    public abstract class DatabaseManagerBase
    {
        protected CancellationToken requestCancelToken = null;

        public enum Action : int
        {
            Create = 1,
            Read = 2,
            Update = 3,
            Delete = 4
        }

        public abstract async void CRUD<T>(Action action, T obj);
    }
}