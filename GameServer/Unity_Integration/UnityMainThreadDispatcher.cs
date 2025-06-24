using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityClient
{
    /// <summary>
    /// Unity Main Thread Dispatcher for handling network callbacks
    /// Ensures network events are processed on Unity's main thread
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        
        public static UnityMainThreadDispatcher Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[MainThreadDispatcher] Initialized");
            }
            else
            {
                Debug.LogWarning("[MainThreadDispatcher] Duplicate instance detected, destroying");
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            // Process all queued actions on the main thread
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    try
                    {
                        _executionQueue.Dequeue().Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[MainThreadDispatcher] Error executing queued action: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Enqueue an action to be executed on the main thread
        /// </summary>
        /// <param name="action">Action to execute</param>
        public void Enqueue(Action action)
        {
            if (action == null)
            {
                Debug.LogWarning("[MainThreadDispatcher] Attempted to enqueue null action");
                return;
            }
            
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }
        
        /// <summary>
        /// Check if we're currently on the main thread
        /// </summary>
        public static bool IsMainThread()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId == 1;
        }
        
        /// <summary>
        /// Execute action immediately if on main thread, otherwise enqueue it
        /// </summary>
        /// <param name="action">Action to execute</param>
        public static void ExecuteOnMainThread(Action action)
        {
            if (action == null) return;
            
            if (IsMainThread())
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainThreadDispatcher] Error executing immediate action: {ex.Message}");
                }
            }
            else
            {
                Instance?.Enqueue(action);
            }
        }
        
        /// <summary>
        /// Get the current queue size (for debugging)
        /// </summary>
        public int GetQueueSize()
        {
            lock (_executionQueue)
            {
                return _executionQueue.Count;
            }
        }
        
        /// <summary>
        /// Clear all queued actions (use with caution)
        /// </summary>
        public void ClearQueue()
        {
            lock (_executionQueue)
            {
                int count = _executionQueue.Count;
                _executionQueue.Clear();
                Debug.LogWarning($"[MainThreadDispatcher] Cleared {count} queued actions");
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Debug.Log("[MainThreadDispatcher] Destroyed");
                Instance = null;
            }
        }
        
        #region Automatic Setup
        
        /// <summary>
        /// Ensure the MainThreadDispatcher exists in the scene
        /// Call this from any script that needs main thread dispatching
        /// </summary>
        public static void EnsureExists()
        {
            if (Instance == null)
            {
                GameObject dispatcherObject = new GameObject("UnityMainThreadDispatcher");
                dispatcherObject.AddComponent<UnityMainThreadDispatcher>();
                Debug.Log("[MainThreadDispatcher] Auto-created dispatcher GameObject");
            }
        }
        
        #endregion
    }
} 