using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace EazyEdu.Web.Data
{
    /// <summary>
    /// Tiny append-only JSON-lines store. Keeps everything in memory for reads and
    /// mirrors it to App_Data so a recycled app pool does not lose the history.
    /// Swap this for a table if the deployment has SQL Server available.
    /// </summary>
    public class JsonLineStore<T> where T : class
    {
        private readonly object _gate = new object();
        private readonly List<T> _items = new List<T>();
        private readonly string _path;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public JsonLineStore(string path)
        {
            _path = path;
            _serializer.MaxJsonLength = 8 * 1024 * 1024;
            Load();
        }

        private void Load()
        {
            try
            {
                if (string.IsNullOrEmpty(_path) || !File.Exists(_path)) return;

                foreach (string line in File.ReadAllLines(_path, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        T item = _serializer.Deserialize<T>(line);
                        if (item != null) _items.Add(item);
                    }
                    catch (ArgumentException)
                    {
                        // A half-written trailing line is expected after an abrupt shutdown; skip it.
                    }
                }
            }
            catch (IOException)
            {
                // Running without write access to App_Data is fine — the store stays in memory.
            }
        }

        public void Add(T item)
        {
            if (item == null) return;

            lock (_gate)
            {
                _items.Add(item);
                Append(item);
            }
        }

        private void Append(T item)
        {
            if (string.IsNullOrEmpty(_path)) return;

            try
            {
                string directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.AppendAllText(_path, _serializer.Serialize(item) + Environment.NewLine, Encoding.UTF8);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>Runs <paramref name="action"/> under the store lock and rewrites the file.</summary>
        public void Mutate(Action<List<T>> action)
        {
            if (action == null) return;

            lock (_gate)
            {
                action(_items);
                Rewrite();
            }
        }

        private void Rewrite()
        {
            if (string.IsNullOrEmpty(_path)) return;

            try
            {
                var builder = new StringBuilder();
                foreach (T item in _items) builder.AppendLine(_serializer.Serialize(item));
                File.WriteAllText(_path, builder.ToString(), Encoding.UTF8);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>Snapshot copy, safe to enumerate outside the lock.</summary>
        public IList<T> Snapshot()
        {
            lock (_gate)
            {
                return new List<T>(_items);
            }
        }

        public int Count
        {
            get { lock (_gate) { return _items.Count; } }
        }
    }
}
