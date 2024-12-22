using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Threading.Tasks;

namespace CCengine
{
    /// <summary>
    /// Useful network static function
    /// </summary>

    public class NetworkTool
    {
        #region Added functions
        /// <summary>
        /// Serializes or deserializes an array of INetworkSerializable objects.
        /// </summary>
        /// <typeparam name="TData">The type of data to serialize.</typeparam>
        /// <typeparam name="TSerializer">The type of serializer.</typeparam>
        /// <param name="serializer">The BufferSerializer instance.</param>
        /// <param name="array">The array to serialize or deserialize.</param>
        public static void NetSerializeArray<TData, TSerializer>(BufferSerializer<TSerializer> serializer, ref TData[] array)
            where TData : INetworkSerializable, new()
            where TSerializer : IReaderWriter
        {
            if (serializer.IsReader)
            {
                int size = 0;
                serializer.SerializeValue(ref size);
                Debug.Log($"Deserializing array of size: {size}");
                array = new TData[size];
                for (int i = 0; i < size; i++)
                {
                    array[i] = new TData();
                    array[i].NetworkSerialize(serializer);
                }
            }
            else // Writer
            {
                if (array == null)
                {
                    Debug.LogWarning("Serializing a null array. Serializing as empty array.");
                    array = new TData[0];
                }

                int size = array.Length;
                serializer.SerializeValue(ref size);
                Debug.Log($"Serializing array of size: {size}");
                for (int i = 0; i < size; i++)
                {
                    if (array[i] == null)
                    {
                        Debug.LogWarning($"Array element at index {i} is null during serialization. Instantiating a new {typeof(TData).Name}.");
                        array[i] = new TData();
                    }
                    array[i].NetworkSerialize(serializer);
                }
            }
        }

        /// <summary>
        /// Serializes or deserializes an array of strings.
        /// </summary>
        /// <typeparam name="TSerializer">The type of serializer.</typeparam>
        /// <param name="serializer">The BufferSerializer instance.</param>
        /// <param name="array">The string array to serialize or deserialize.</param>
        public static void NetSerializeStringArray<TSerializer>(BufferSerializer<TSerializer> serializer, ref string[] array)
            where TSerializer : IReaderWriter
        {
            if (serializer.IsReader)
            {
                int size = 0;
                serializer.SerializeValue(ref size);
                Debug.Log($"Deserializing string array of size: {size}");
                array = new string[size];
                for (int i = 0; i < size; i++)
                {
                    string val = null;
                    serializer.SerializeValue(ref val);
                    array[i] = val;
                }
            }
            else // Writer
            {
                if (array == null)
                {
                    Debug.LogWarning("Serializing a null string array. Serializing as empty array.");
                    array = new string[0];
                }

                int size = array.Length;
                serializer.SerializeValue(ref size);
                Debug.Log($"Serializing string array of size: {size}");
                for (int i = 0; i < size; i++)
                {
                    string val = array[i];
                    serializer.SerializeValue(ref val);
                }
            }
        }


        public static void NetSerializeList<T, U>(BufferSerializer<T> serializer, ref List<U> list)
            where T : IReaderWriter
            where U : INetworkSerializable, new()
        {
            bool isReader = !serializer.IsWriter;
            int length = (list == null) ? 0 : list.Count;
            serializer.SerializeValue(ref length);

            if (isReader)
                list = new List<U>(length);

            if (isReader)
            {
                for (int i = 0; i < length; i++)
                {
                    U item = new U();
                    item.NetworkSerialize(serializer);
                    list.Add(item);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    list[i].NetworkSerialize(serializer);
                }
            }
        }

        public static void NetSerializeDictionary<T, K, V>(BufferSerializer<T> serializer, ref Dictionary<K, V> dict)
            where T : IReaderWriter
            where K : INetworkSerializable, new()
            where V : INetworkSerializable, new()
        {
            bool isReader = !serializer.IsWriter;
            int length = dict == null ? 0 : dict.Count;
            serializer.SerializeValue(ref length);

            if (isReader)
                dict = new Dictionary<K, V>(length);

            if (serializer.IsWriter)
            {
                foreach (var kvp in dict)
                {
                    K key = kvp.Key;
                    key.NetworkSerialize(serializer);

                    V value = kvp.Value;
                    value.NetworkSerialize(serializer);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    K key = new K();
                    key.NetworkSerialize(serializer);
                    V value = new V();
                    value.NetworkSerialize(serializer);
                    dict.Add(key, value);
                }
            }
        }

        public static void NetSerializeDictionary<T, K, V>(BufferSerializer<T> serializer, ref Dictionary<string, V> dict)
            where T : IReaderWriter
            where V : INetworkSerializable, new()
        {
            bool isReader = !serializer.IsWriter;
            int length = dict == null ? 0 : dict.Count;
            serializer.SerializeValue(ref length);

            if (isReader)
                dict = new Dictionary<string, V>(length);

            if (serializer.IsWriter)
            {
                foreach (var kvp in dict)
                {
                    string key = kvp.Key;
                    serializer.SerializeValue(ref key);

                    V value = kvp.Value;
                    value.NetworkSerialize(serializer);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    string key = "";
                    serializer.SerializeValue(ref key);

                    V value = new V();
                    value.NetworkSerialize(serializer);
                    dict.Add(key, value);
                }
            }
        }
        #endregion

        //Serialize a [System.Serializable] into bytes
        public static byte[] Serialize<T>(T obj) where T : class
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                bf.Serialize(ms, obj);
                byte[] bytes = ms.ToArray();
                ms.Close();
                return bytes;
            }
            catch (Exception e)
            {
                Debug.LogError("Serialization error: " + e.Message);
                return new byte[0];
            }
        }

        //Deserialize a [System.Serializable] from bytes
        public static T Deserialize<T>(byte[] bytes) where T : class
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);
                T obj = (T)bf.Deserialize(ms);
                ms.Close();
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogError("Deserialization error: " + e.Message);
                return null;
            }
        }

        //Serialize a INetworkSerializable to bytes
        public static byte[] NetSerialize<T>(T obj, int size = 128) where T : INetworkSerializable, new()
        {
            if (obj == null)
                return new byte[0];

            try
            {
                FastBufferWriter writer = new FastBufferWriter(size, Allocator.Temp, TcgNetwork.MsgSizeMax);
                writer.WriteNetworkSerializable(obj);
                byte[] bytes = writer.ToArray();
                writer.Dispose();
                return bytes;
            }
            catch (Exception e)
            {
                Debug.LogError("Serialization error: " + e.Message);
                return new byte[0];
            }
        }

        //Deserialize a INetworkSerializable from bytes
        public static T NetDeserialize<T>(byte[] bytes) where T : INetworkSerializable, new()
        {
            if (bytes == null || bytes.Length == 0)
                return default(T);

            try
            {
                FastBufferReader reader = new FastBufferReader(bytes, Allocator.Temp);
                reader.ReadNetworkSerializable(out T obj);
                reader.Dispose();
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogError("Deserialization error: " + e.Message);
                return default(T);
            }
        }

        //Serialize an array using Netcode
        // public static void NetSerializeArray<TS>(BufferSerializer<TS> serializer, ref string[] array) where TS : IReaderWriter
        // {
        //     if (serializer.IsReader)
        //     {
        //         int size = 0;
        //         serializer.SerializeValue(ref size);
        //         array = new string[size];
        //         for (int i = 0; i < size; i++)
        //         {
        //             string val = "";
        //             serializer.SerializeValue(ref val);
        //             array[i] = val;
        //         }
        //     }

        //     if (serializer.IsWriter)
        //     {
        //         int size = array.Length;
        //         serializer.SerializeValue(ref size);
        //         for (int i = 0; i < size; i++)
        //             serializer.SerializeValue(ref array[i]);
        //     }
        // }

        public static byte[] SerializeInt32(int data)
        {
            return System.BitConverter.GetBytes(data);
        }

        public static int DeserializeInt32(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
                return System.BitConverter.ToInt32(bytes, 0);
            return 0;
        }

        public static byte[] SerializeUInt64(ulong data)
        {
            return System.BitConverter.GetBytes(data);
        }

        public static ulong DeserializeUInt64(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
                return System.BitConverter.ToUInt64(bytes, 0);
            return 0;
        }

        public static byte[] SerializeString(string data)
        {
            if (data != null)
                return System.Text.Encoding.UTF8.GetBytes(data);
            return new byte[0];
        }

        public static string DeserializeString(byte[] bytes)
        {
            if (bytes != null)
                return System.Text.Encoding.UTF8.GetString(bytes);
            return null;
        }

        public static string SerializeToString<T>(T obj) where T : class
        {
            byte[] bytes = Serialize<T>(obj);
            return Convert.ToBase64String(bytes);
        }

        public static T DeserializeFromString<T>(string str) where T : class
        {
            byte[] bytes = Convert.FromBase64String(str);
            return Deserialize<T>(bytes);
        }

        public static void SerializeObject<T, T1>(BufferSerializer<T> serializer, ref T1 data) where T : IReaderWriter where T1 : class
        {
            string sdata = "";
            if (serializer.IsWriter)
            {
                sdata = SerializeToString(data);
            }
            serializer.SerializeValue(ref sdata, true);
            if (serializer.IsReader)
            {
                data = DeserializeFromString<T1>(sdata);
            }
        }

        public static void SerializeDictionary<T, T1, T2>(BufferSerializer<T> serializer, ref Dictionary<T1, T2> data)
            where T : IReaderWriter where T1 : unmanaged, IComparable, IConvertible, IComparable<T1>, IEquatable<T1> where T2 : unmanaged, IComparable, IConvertible, IComparable<T2>, IEquatable<T2>
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<T1, T2> pair in data)
                {
                    T1 key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = new T1();
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                    data.Add(key, val);
                }
            }
        }

        public static void SerializeDictionaryEnum<T, T1, T2>(BufferSerializer<T> serializer, ref Dictionary<T1, T2> data)
            where T : IReaderWriter where T1 : unmanaged, Enum where T2 : unmanaged, IComparable, IConvertible, IComparable<T2>, IEquatable<T2>
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<T1, T2> pair in data)
                {
                    T1 key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = new T1();
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                    data.Add(key, val);
                }
            }
        }

        public static void SerializeDictionary<T, T2>(BufferSerializer<T> serializer, ref Dictionary<string, T2> data)
            where T : IReaderWriter where T2 : unmanaged, IComparable, IConvertible, IComparable<T2>, IEquatable<T2>
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<string, T2> pair in data)
                {
                    string key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<string, T2>();
                for (int i = 0; i < count; i++)
                {
                    string key = "";
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                    data.Add(key, val);
                }
            }
        }

        public static void SerializeDictionary<T>(BufferSerializer<T> serializer, ref Dictionary<string, string> data)
            where T : IReaderWriter
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<string, string> pair in data)
                {
                    string key = pair.Key;
                    string val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<string, string>();
                for (int i = 0; i < count; i++)
                {
                    string key = "";
                    string val = "";
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                    data.Add(key, val);
                }
            }
        }

        public static void SerializeDictionaryNetObject<T, T2>(BufferSerializer<T> serializer, ref Dictionary<string, T2> data)
            where T : IReaderWriter where T2 : INetworkSerializable, new()
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<string, T2> pair in data)
                {
                    string key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeNetworkSerializable(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<string, T2>();
                for (int i = 0; i < count; i++)
                {
                    string key = "";
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    serializer.SerializeNetworkSerializable(ref val);
                    data.Add(key, val);
                }
            }
        }

        public static void SerializeDictionaryNetObject<T, T1, T2>(BufferSerializer<T> serializer, ref Dictionary<T1, T2> data)
            where T : IReaderWriter where T1 : unmanaged, IComparable, IConvertible, IComparable<T1>, IEquatable<T1> where T2 : INetworkSerializable, new()
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<T1, T2> pair in data)
                {
                    T1 key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeNetworkSerializable(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = new T1();
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    serializer.SerializeNetworkSerializable(ref val);
                    data.Add(key, val);
                }
            }
        }

        public static void SerializeDictionaryObject<T, T2>(BufferSerializer<T> serializer, ref Dictionary<string, T2> data)
            where T : IReaderWriter where T2 : class, new()
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<string, T2> pair in data)
                {
                    string key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<string, T2>();
                for (int i = 0; i < count; i++)
                {
                    string key = "";
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                    data.Add(key, val);
                }
            }
        }

        public static void SerializeDictionaryObject<T, T1, T2>(BufferSerializer<T> serializer, ref Dictionary<T1, T2> data)
            where T : IReaderWriter where T1 : unmanaged, IComparable, IConvertible, IComparable<T1>, IEquatable<T1> where T2 : class, new()
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<T1, T2> pair in data)
                {
                    T1 key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = new T1();
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                    data.Add(key, val);
                }
            }
        }

        public static void SerializeDictionaryEnumObject<T, T1, T2>(BufferSerializer<T> serializer, ref Dictionary<T1, T2> data)
            where T : IReaderWriter where T1 : unmanaged, Enum where T2 : class, new()
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<T1, T2> pair in data)
                {
                    T1 key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = new T1();
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                    data.Add(key, val);
                }
            }
        }

        public static ushort Hash16(string string_id)
        {
            return (ushort)string_id.GetHashCode();
        }

        public static uint Hash32(string string_id)
        {
            return (uint)string_id.GetHashCode();
        }

        public static ulong Hash64(string string_id)
        {
            string s1 = string_id.Substring(0, string_id.Length / 2);
            string s2 = string_id.Substring(string_id.Length / 2);
            ulong id = (uint)s1.GetHashCode();
            id = id << 32;
            id = id | (uint)s2.GetHashCode();
            return id;
        }

        public static IPAddress ResolveDns(string url)
        {
#if !UNITY_WEBGL
            IPAddress[] ips = Dns.GetHostAddresses(url);
            if (ips != null && ips.Length > 0)
                return ips[0];
#else
            Debug.LogWarning("Dns.GetHostAddresses not working on WebGL, try using direct IP address instead of host url");
#endif
            return null;
        }

        //Converts a host (either domain or IP) into an IP
        public static string HostToIP(string host)
        {
            bool success = IPAddress.TryParse(host, out IPAddress address);
            if (success)
                return address.ToString(); //Already an IP
            IPAddress ip = ResolveDns(host); //Not an IP, resolve DNS
            if (ip != null)
                return ip.ToString();
            return "";
        }

        public static string GetLocalIp()
        {
            //Get Internal IP
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "";
        }

        public static async Task<string> GetOnlineIp()
        {
            //Get External IP
            WebResponse res = await WebTool.SendRequest("https://api.ipify.org");
            if (res.success)
                return res.data;
            else
                return null;
        }
    }
}
