using System;
using System.Collections.Generic;
using UnityEngine;

namespace BunnyBlitz
{
    /// <summary>
    /// Very simple save system that just transform the save data into a JSON string and save it into
    /// PlayerPref. This won't handle multiple save or profile, but will be enough for this small demo
    /// </summary>
    public class SaveSystem
    {
        const string KeyName = "Unity_Platformer2d3d";

        [Serializable]
        public class SaveData : ISerializationCallbackReceiver
        {
            [Serializable]
            public class LevelData
            {
                public int Percentage;
            }

            //Notify Unity to not try to serialize this field, as Dictionary are not supported and this creates a 
            //warning in 6.5
            [NonSerialized]
            public Dictionary<string, LevelData> LevelsData = new();

            [NonSerialized]
            public int LiveCount = 0;
            [NonSerialized]
            public int ProjectileCount = -1;

            [SerializeField]
            private List<string> m_LevelsDataKeys = new();
            [SerializeField]
            private List<LevelData> m_LevelsDataValues = new();

            public void OnBeforeSerialize()
            {
                m_LevelsDataKeys.Clear();
                m_LevelsDataValues.Clear();
                foreach (var pair in LevelsData)
                {
                    m_LevelsDataKeys.Add(pair.Key);
                    m_LevelsDataValues.Add(pair.Value);
                }
            }

            public void OnAfterDeserialize()
            {
                LevelsData.Clear();
                for (int i = 0; i < m_LevelsDataKeys.Count; ++i)
                {
                    LevelsData.Add(m_LevelsDataKeys[i], m_LevelsDataValues[i]);
                }
            }
        }

        public SaveData Data { get; private set; } = new();

        public void Save()
        {
            PlayerPrefs.SetString(KeyName, JsonUtility.ToJson(Data));
        }

        public void Load()
        {
            var dataString = PlayerPrefs.GetString(KeyName);

            if (dataString != String.Empty)
            {
                Data = JsonUtility.FromJson<SaveData>(dataString);
            }
        }

        public SaveData.LevelData GetOrCreateLevelData(string levelName)
        {
            if (!Data.LevelsData.TryGetValue(levelName, out var levelData))
            {
                levelData = new SaveData.LevelData();
                levelData.Percentage = 0;
                Data.LevelsData.Add(levelName, levelData);
            }

            return levelData;
        }
    }
}