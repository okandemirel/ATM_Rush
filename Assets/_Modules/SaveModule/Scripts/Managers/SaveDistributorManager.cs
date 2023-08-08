using _Modules.SaveModule.Scripts.Data;
using UnityEngine;

namespace _Modules.SaveModule.Scripts.Managers
{
    public class SaveDistributorManager : MonoBehaviour
    {
        private static GameData _gameData;
        private static readonly SaveManager SaveManager = new SaveManager();
        [SerializeField] private bool autoSave = true;

        private void Awake()
        {
            GetSaveData();
        }

        public static GameData GetSaveData()
        {
            GameData GetData()
            {
                return SaveManager.PreLoadData(new GameData());
            }

            if (_gameData is null)
            {
                _gameData = GetData();
            }

            return _gameData;
        }

        public static void SaveData()
        {
            if (_gameData is null) GetSaveData();
            SaveManager.PreSaveData(_gameData);
        }

#if UNITY_EDITOR

        private void OnApplicationQuit()
        {
            if (autoSave) SaveData();
        }
#endif

#if UNITY_ANDROID && UNITY_IOS
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus&&autoSave)SaveData();
        }
#endif
    }
}