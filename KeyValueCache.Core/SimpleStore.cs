namespace KeyValueCache.Core;

    public class SimpleStore: IDisposable
    {
        // Хранилище данных в виде словаря
        private Dictionary<string, byte[]> _storage = new Dictionary<string, byte[]>();
        private readonly ReaderWriterLockSlim _lock = new();
        private long _setCount;
        private long _getCount;
        private long _deleteCount;

        /// Устанавливает значение по ключу
        public void Set(string key, byte[] value)
        {
            _lock.EnterWriteLock();
            try
            {
                _storage[key] = value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            Interlocked.Increment(ref _setCount);
        }

        /// Возвращает значение по ключу
        public byte[]? Get(string key)
        {
            _lock.EnterReadLock();
            try
            {
                if (_storage.TryGetValue(key, out var value))
                {
                    return value;
                }
                return null;
            }
            finally
            {
                _lock.ExitReadLock();
                Interlocked.Increment(ref _getCount);
            }
        }

        /// Удаляет значение по ключу
        public void Delete(string key)
        {
            _lock.EnterWriteLock();
            try
            {
                _storage.Remove(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            Interlocked.Increment(ref _deleteCount);
        }

        // Метод для получения статистики
        public (long setCount, long getCount, long deleteCount) GetStatistics()
        {
            return (_setCount, _getCount, _deleteCount);
        }

        public void Dispose()
        {
            _lock.Dispose();
        }        
    }
