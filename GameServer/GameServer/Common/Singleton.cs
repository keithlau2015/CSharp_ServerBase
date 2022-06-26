
namespace Common
{
    public abstract class Singleton<T> where T : new()
    {
        private static readonly object locker = new object();
        private static T instance;

        public static T singleton
        {
            get
            {
                if(instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                        {
                            instance = new T();
                        }
                    }
                }

                return instance;
            }
        }
    }
}
