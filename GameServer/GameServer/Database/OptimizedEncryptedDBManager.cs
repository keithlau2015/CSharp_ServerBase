using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace Database
{
    public class OptimizedEncryptedDBManager : DatabaseBase, IPersistentDatabase, IDisposable
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _cache;
        private readonly ConcurrentDictionary<string, bool> _dirtyTables;
        private readonly string _dataDirectory;
        private readonly byte[] _keyBytes;
        private readonly byte[] _ivBytes;
        private readonly Timer _autoSaveTimer;
        private readonly SemaphoreSlim _saveSemaphore;
        private readonly int _autoSaveIntervalMs;
        private bool _disposed = false;

        public OptimizedEncryptedDBManager(string dataDirectory, string encryptionKey, int autoSaveIntervalSeconds = 30)
        {
            _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
            _autoSaveIntervalMs = autoSaveIntervalSeconds * 1000;
            
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
                Debug.DebugUtility.DebugLog($"Created data directory: {_dataDirectory}");
            }

            _keyBytes = DeriveKey(encryptionKey, 32);
            _ivBytes = DeriveKey(encryptionKey + "_IV", 16);

            _cache = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();
            _dirtyTables = new ConcurrentDictionary<string, bool>();
            _saveSemaphore = new SemaphoreSlim(1, 1);

            LoadAllDataFromDisk();
            _autoSaveTimer = new Timer(AutoSaveCallback, null, _autoSaveIntervalMs, _autoSaveIntervalMs);

            Debug.DebugUtility.DebugLog($"OptimizedEncryptedDBManager initialized with {autoSaveIntervalSeconds}s auto-save");
        }

        private async void AutoSaveCallback(object state)
        {
            await SaveDirtyTablesAsync();
        }

        private async Task SaveDirtyTablesAsync()
        {
            if (_disposed) return;

            await _saveSemaphore.WaitAsync();
            try
            {
                var dirtyTableKeys = _dirtyTables.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
                
                if (dirtyTableKeys.Count > 0)
                {
                    var tasks = dirtyTableKeys.Select(async tableKey =>
                    {
                        if (_cache.TryGetValue(tableKey, out var table))
                        {
                            await Task.Run(() => SaveTableToDisk(tableKey, table));
                            _dirtyTables[tableKey] = false;
                        }
                    });

                    await Task.WhenAll(tasks);
                    Debug.DebugUtility.DebugLog($"Auto-saved {dirtyTableKeys.Count} dirty tables");
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Auto-save failed: {ex.Message}");
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }

        private byte[] DeriveKey(string input, int length)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                var result = new byte[length];
                Array.Copy(hash, result, Math.Min(hash.Length, length));
                return result;
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
            if (obj == null || string.IsNullOrEmpty(dbName))
            {
                Debug.DebugUtility.ErrorLog("CRUD operation failed: invalid parameters");
                return;
            }

            try
            {
                await Task.Run(() => PerformCRUDOperation(action, dbName, keyField, obj));
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"CRUD operation failed: {ex.Message}");
                throw;
            }
        }

        private void PerformCRUDOperation<T>(Action action, string dbName, string keyField, T obj)
        {
            var keyProperty = typeof(T).GetProperty(keyField);
            if (keyProperty == null) return;

            var keyValue = keyProperty.GetValue(obj)?.ToString();
            if (string.IsNullOrEmpty(keyValue)) return;

            var tableKey = $"{dbName}_{typeof(T).Name}";
            var table = _cache.GetOrAdd(tableKey, _ => new ConcurrentDictionary<string, object>());

            bool modified = false;

            switch (action)
            {
                case Action.Create:
                    table.AddOrUpdate(keyValue, obj, (k, v) => obj);
                    modified = true;
                    break;

                case Action.Read:
                    if (table.TryGetValue(keyValue, out var readResult) && readResult is T typedResult)
                    {
                        CopyProperties(typedResult, obj);
                    }
                    break;

                case Action.Update:
                    if (table.ContainsKey(keyValue))
                    {
                        table[keyValue] = obj;
                        modified = true;
                    }
                    break;

                case Action.Delete:
                    if (table.TryRemove(keyValue, out _))
                    {
                        modified = true;
                    }
                    break;
            }

            if (modified)
            {
                _dirtyTables[tableKey] = true;
            }
        }

        private void CopyProperties<T>(T source, T destination)
        {
            if (source == null || destination == null) return;

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

        private void LoadAllDataFromDisk()
        {
            try
            {
                var files = Directory.GetFiles(_dataDirectory, "*.edb");
                
                foreach (var file in files)
                {
                    var tableKey = Path.GetFileNameWithoutExtension(file);
                    LoadTableFromDisk(tableKey);
                }

                Debug.DebugUtility.DebugLog($"Loaded {files.Length} database tables from disk");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to load data from disk: {ex.Message}");
            }
        }

        private void LoadTableFromDisk(string tableKey)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, $"{tableKey}.edb");
                if (!File.Exists(filePath)) return;

                var encryptedData = File.ReadAllBytes(filePath);
                var decryptedJson = Decrypt(encryptedData);
                
                var tableData = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedJson);
                if (tableData != null)
                {
                    var concurrentTable = new ConcurrentDictionary<string, object>(tableData);
                    _cache[tableKey] = concurrentTable;
                    _dirtyTables[tableKey] = false;
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to load table {tableKey}: {ex.Message}");
            }
        }

        private void SaveTableToDisk(string tableKey, ConcurrentDictionary<string, object> table)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, $"{tableKey}.edb");
                var json = JsonConvert.SerializeObject(table.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                var encryptedData = Encrypt(json);
                
                File.WriteAllBytes(filePath, encryptedData);
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to save table {tableKey}: {ex.Message}");
            }
        }

        public void SaveAllDataToDisk()
        {
            try
            {
                _saveSemaphore.Wait();
                
                foreach (var table in _cache)
                {
                    SaveTableToDisk(table.Key, table.Value);
                    _dirtyTables[table.Key] = false;
                }
                
                Debug.DebugUtility.DebugLog("All database tables saved to disk");
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }

        private byte[] Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _keyBytes;
                aes.IV = _ivBytes;
                aes.Mode = CipherMode.CBC;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cs))
                {
                    writer.Write(plainText);
                    writer.Flush();
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        private string Decrypt(byte[] cipherText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _keyBytes;
                aes.IV = _ivBytes;
                aes.Mode = CipherMode.CBC;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherText))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _autoSaveTimer?.Dispose();
                SaveAllDataToDisk();
                _cache.Clear();
                _saveSemaphore?.Dispose();
                _disposed = true;
            }
        }
    }
} 