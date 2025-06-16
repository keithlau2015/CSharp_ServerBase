using System;

namespace Common
{
    public abstract class Singleton<T> where T : class, new()
    {
        // Use Lazy<T> for thread-safe lazy initialization
        private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());

        public static T Instance => _instance.Value;

        // Keep the old property name for backward compatibility
        public static T singleton => Instance;

        // Prevent external instantiation
        protected Singleton() { }
    }
}
