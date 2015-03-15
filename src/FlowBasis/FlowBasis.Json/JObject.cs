using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Collections;
using System.IO;

namespace FlowBasis.Json
{
    
    public class JObject : DynamicObject, IDictionary, IDictionary<string, object>
    {
        private IDictionary<string, object> values;

        public JObject()
        {
            this.values = new Dictionary<string, object>();
        }

        public JObject(IDictionary<string, object> values)
        {
            this.values = values;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this.values.Keys;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this.values[binder.Name] = value;
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!this.values.ContainsKey(binder.Name))
            {
                result = null;
                return true;
            }

            result = this.values[binder.Name];
            return true;
        }

        public static object Parse(string json)
        {
            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
            serializer.FloatParseHandling = Newtonsoft.Json.FloatParseHandling.Decimal;
            serializer.MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore;
            serializer.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            serializer.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;

            object deserializedObject;

            using (var reader = new StringReader(json))
            using (var jsonReader = new Newtonsoft.Json.JsonTextReader(reader))
            {
                deserializedObject = serializer.Deserialize<dynamic>(jsonReader);
            }

            object processedValue = PostProcessValue(deserializedObject);
            return processedValue;            
        }

        private static object PostProcessValue(object value)
        {
            if (value is Array)
            {
                // Convert array to ArrayList.
                var valueArray = (Array)value;                
                var valueList = new ArrayList(valueArray.Length);
                foreach (var entry in valueArray)
                {
                    object processedValue = PostProcessValue(entry);
                    valueList.Add(processedValue);
                }

                return valueList;
            }
            else if (value is ArrayList)
            {
                ArrayList list = (ArrayList)value;
                for (int co = 0; co < list.Count; co++)
                {
                    object entry = list[co];
                    object processedEntry = PostProcessValue(entry);
                    list[co] = entry;
                }

                return list;
            }
            else if (value is Newtonsoft.Json.Linq.JArray)
            {
                // Convert array to List<object>.
                var jArray = (Newtonsoft.Json.Linq.JArray)value;
                var valueList = new ArrayList(jArray.Count);
                foreach (var entry in jArray)
                {
                    object processedValue = PostProcessValue(entry);
                    valueList.Add(processedValue);
                }

                return valueList;
            }
            else if (value is Dictionary<string, object>)
            {
                Dictionary<string, object> processedDictionary = new Dictionary<string, object>();

                foreach (var entry in (IDictionary<string, object>)value)
                {
                    string entryName = entry.Key;
                    object processedEntryValue = PostProcessValue(entry.Value);
                    processedDictionary[entryName] = processedEntryValue;
                }

                return new JObject(processedDictionary);
            }
            else if (value is Newtonsoft.Json.Linq.JObject)
            {
                Dictionary<string, object> processedDictionary = new Dictionary<string, object>();

                Newtonsoft.Json.Linq.JObject jObject = (Newtonsoft.Json.Linq.JObject)value;
                foreach (var prop in jObject.Properties())
                {                    
                    string entryName = prop.Name;
                    object processedEntryValue = PostProcessValue(prop.Value);
                    processedDictionary[entryName] = processedEntryValue;
                }

                return new JObject(processedDictionary);
            }
            else if (value is Newtonsoft.Json.Linq.JValue)
            {
                Newtonsoft.Json.Linq.JValue jValue = (Newtonsoft.Json.Linq.JValue)value;
                return jValue.Value;
            }
            else
            {
                return value;
            }
        }

        public static string Stringify(object value)
        {
            JObjectRootMapper rootMapper = new JObjectRootMapper();
            JsonSerializationService serializer = new JsonSerializationService(() => rootMapper);

            string json = serializer.Stringify(value);
            return json;
        }

        #region IDictionary Members

        public void Add(object key, object value)
        {
            this.values.Add((string)key, value);
        }

        public void Clear()
        {
            this.values.Clear();
        }

        public bool Contains(object key)
        {
            if (key is string)
                return this.values.ContainsKey((string)key);
            else
                return false;
        }

        private class JObjectDictionaryEnumerator : IDictionaryEnumerator
        {
            private JObject obj;
            private IEnumerator<string> valueEnumerator;

            public JObjectDictionaryEnumerator(JObject obj)
            {
                this.obj = obj;
                this.valueEnumerator = this.obj.GetDynamicMemberNames().GetEnumerator();
            }

            #region IDictionaryEnumerator Members

            public DictionaryEntry Entry
            {
                get { return new DictionaryEntry(this.Key, this.Value); }
            }

            public object Key
            {
                get { return this.valueEnumerator.Current; }
            }

            public object Value
            {
                get { return this.obj.values[(string)this.Key]; }
            }

            #endregion

            #region IEnumerator Members

            public object Current
            {
                get { return this.Entry; }
            }

            public bool MoveNext()
            {
                return this.valueEnumerator.MoveNext();
            }

            public void Reset()
            {
                this.valueEnumerator.Reset();
            }

            #endregion
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return new JObjectDictionaryEnumerator(this);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public ICollection Keys
        {
            get { return this.values.Keys.ToArray(); }
        }

        public void Remove(object key)
        {
            this.values.Remove((string)key);
        }

        public ICollection Values
        {
            get { return this.values.ToArray(); }
        }

        public object this[object key]
        {
            get
            {
                return this[(string)key];
            }
            set
            {
                this[(string)key] = value;
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return this.values.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this.values; }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IDictionary<string,object> Members

        public void Add(string key, object value)
        {
            this.values.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return this.values.ContainsKey(key);
        }

        ICollection<string> IDictionary<string, object>.Keys
        {
            get { return this.values.Keys; }
        }

        public bool Remove(string key)
        {
            return this.values.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return this.values.TryGetValue(key, out value);
        }

        ICollection<object> IDictionary<string, object>.Values
        {
            get { return this.values.Values; }
        }

        public object this[string key]
        {
            get
            {
                object value;
                if (this.values.TryGetValue(key, out value))
                {
                    return value;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                this.values[key] = value;
            }
        }

        #endregion

        #region ICollection<KeyValuePair<string,object>> Members

        public void Add(KeyValuePair<string, object> item)
        {
            this.values.Add(item);
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return this.values.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            this.values.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return this.values.Remove(item);
        }

        #endregion

        #region IEnumerable<KeyValuePair<string,object>> Members

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return this.values.GetEnumerator();
        }

        #endregion
    }
    
}