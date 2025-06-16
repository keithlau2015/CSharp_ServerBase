using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace Database
{
    public class EncryptedBinaryDBManager : DatabaseBase, IPersistentDatabase, IDisposable
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _cache;
        private readonly string _dataDirectory;
        private readonly string _encryptionKey;
        private readonly byte[] _keyBytes;
        private readonly byte[] _ivBytes;
        private bool _disposed = false;

        public EncryptedBinaryDBManager(string dataDirectory, string encryptionKey)
        {
            _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
            _encryptionKey = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));
            
            // Ensure directory exists
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
                Debug.DebugUtility.DebugLog($"Created data directory: {_dataDirectory}");
            }

            // Initialize encryption keys
            _keyBytes = DeriveKey(_encryptionKey, 32); // 256-bit key for AES
            _ivBytes = DeriveKey(_encryptionKey + "_IV", 16); // 128-bit IV for AES

            _cache = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();

            // Load existing data from disk
            LoadAllDataFromDisk();

            Debug.DebugUtility.DebugLog("EncryptedBinaryDBManager initialized successfully");
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
            if (obj == null)
            {
                Debug.DebugUtility.ErrorLog("CRUD operation failed: object is NULL");
                return;
            }

            if (string.IsNullOrEmpty(dbName))
            {
                Debug.DebugUtility.ErrorLog("CRUD operation failed: database name is null or empty");
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
            if (keyProperty == null)
            {
                Debug.DebugUtility.ErrorLog($"CRUD operation failed: Property '{keyField}' not found on type {typeof(T).Name}");
                return;
            }

            var keyValue = keyProperty.GetValue(obj)?.ToString();
            if (string.IsNullOrEmpty(keyValue))
            {
                Debug.DebugUtility.ErrorLog($"CRUD operation failed: Key field '{keyField}' value is null or empty");
                return;
            }

            var tableKey = $"{dbName}_{typeof(T).Name}";
            var table = _cache.GetOrAdd(tableKey, _ => new ConcurrentDictionary<string, object>());

            switch (action)
            {
                case Action.Create:
                    table.AddOrUpdate(keyValue, obj, (k, v) => obj);
                    Debug.DebugUtility.DebugLog($"Created/Updated document in {tableKey} with key {keyValue}");
                    break;

                case Action.Read:
                    if (table.TryGetValue(keyValue, out var readResult) && readResult is T typedResult)
                    {
                        CopyProperties(typedResult, obj);
                        Debug.DebugUtility.DebugLog($"Read document from {tableKey} with key {keyValue}");
                    }
                    else
                    {
                        Debug.DebugUtility.WarningLog($"No document found in {tableKey} with key {keyValue}");
                    }
                    break;

                case Action.Update:
                    if (table.ContainsKey(keyValue))
                    {
                        table[keyValue] = obj;
                        Debug.DebugUtility.DebugLog($"Updated document in {tableKey} with key {keyValue}");
                    }
                    else
                    {
                        Debug.DebugUtility.WarningLog($"No document to update in {tableKey} with key {keyValue}");
                    }
                    break;

                case Action.Delete:
                    if (table.TryRemove(keyValue, out _))
                    {
                        Debug.DebugUtility.DebugLog($"Deleted document from {tableKey} with key {keyValue}");
                    }
                    else
                    {
                        Debug.DebugUtility.WarningLog($"No document to delete in {tableKey} with key {keyValue}");
                    }
                    break;

                default:
                    Debug.DebugUtility.ErrorLog($"Unknown CRUD action: {action}");
                    break;
            }

            // Auto-save after each operation (you can optimize this with a dirty flag)
            SaveTableToDisk(tableKey, table);
        }

        private void CopyProperties<T>(T source, T destination)
        {
            if (source == null || destination == null)
                return;

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

        public async Task<List<T>> GetAllAsync<T>(string dbName)
        {
            return await Task.Run(() =>
            {
                var tableKey = $"{dbName}_{typeof(T).Name}";
                if (_cache.TryGetValue(tableKey, out var table))
                {
                    return table.Values.OfType<T>().ToList();
                }
                return new List<T>();
            });
        }

        public async Task<T> GetByIdAsync<T>(string dbName, string keyField, object keyValue)
        {
            return await Task.Run(() =>
            {
                var tableKey = $"{dbName}_{typeof(T).Name}";
                if (_cache.TryGetValue(tableKey, out var table) && 
                    table.TryGetValue(keyValue?.ToString(), out var obj) && 
                    obj is T typedObj)
                {
                    return typedObj;
                }
                return default(T);
            });
        }

        private void LoadAllDataFromDisk()
        {
            try
            {
                var files = Directory.GetFiles(_dataDirectory, "*.edb"); // Encrypted Database files
                
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
                if (!File.Exists(filePath))
                    return;

                var encryptedData = File.ReadAllBytes(filePath);
                var decryptedJson = Decrypt(encryptedData);
                
                var tableData = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedJson);
                if (tableData != null)
                {
                    var concurrentTable = new ConcurrentDictionary<string, object>();
                    foreach (var kvp in tableData)
                    {
                        concurrentTable[kvp.Key] = kvp.Value;
                    }
                    _cache[tableKey] = concurrentTable;
                    Debug.DebugUtility.DebugLog($"Loaded table {tableKey} with {tableData.Count} records");
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
                var json = JsonConvert.SerializeObject(table.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), Formatting.None);
                var encryptedData = Encrypt(json);
                
                File.WriteAllBytes(filePath, encryptedData);
                Debug.DebugUtility.DebugLog($"Saved table {tableKey} to disk");
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
                foreach (var table in _cache)
                {
                    SaveTableToDisk(table.Key, table.Value);
                }
                Debug.DebugUtility.DebugLog("All database tables saved to disk");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to save all data to disk: {ex.Message}");
            }
        }

        private byte[] Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _keyBytes;
                aes.IV = _ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

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
                aes.Padding = PaddingMode.PKCS7;

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
                Debug.DebugUtility.DebugLog("Saving all data before disposing EncryptedBinaryDBManager");
                SaveAllDataToDisk();
                _cache.Clear();
                _disposed = true;
            }
        }

        ~EncryptedBinaryDBManager()
        {
            Dispose();
        }
    }
} 