using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.VR.Saving
{
    public class PhotonVRValueSaver : MonoBehaviour
    {

        [Serializable]
        private class Entries
        {
            public List<string> Keys = new List<string>();
            public List<string> Values = new List<string>();
        }

        public static void SaveDictionary(string location, Dictionary<string, string> value)
        {
            if (string.IsNullOrEmpty(location))
                return;

            Entries entries = new Entries();
            if (value != null)
            {
                foreach (KeyValuePair<string, string> kv in value)
                {
                    if (kv.Key == null)
                        continue;
                    entries.Keys.Add(kv.Key);
                    entries.Values.Add(kv.Value ?? string.Empty);
                }
            }

            PlayerPrefs.SetString(location, JsonUtility.ToJson(entries));
        }

        public static Dictionary<string, string> GetDictionary(string location)
        {
            Dictionary<string, string> value = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(location))
                return value;

            string raw = PlayerPrefs.GetString(location);
            if (string.IsNullOrEmpty(raw))
                return value;

            Entries entries;
            try
            {
                entries = JsonUtility.FromJson<Entries>(raw);
            }
            catch (Exception)
            {

                return value;
            }

            if (entries == null || entries.Keys == null || entries.Values == null)
                return value;

            int count = Math.Min(entries.Keys.Count, entries.Values.Count);
            for (int i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(entries.Keys[i]))
                    value[entries.Keys[i]] = entries.Values[i];
            }

            return value;
        }
    }
}
