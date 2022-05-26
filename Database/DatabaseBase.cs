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

        public abstract async void CRUD<T>(Action action, T obj);
    }
}