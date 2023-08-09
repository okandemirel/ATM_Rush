using Runtime.Extentions;
using UnityEngine.Events;

namespace Runtime.Signals
{
    public class SaveSignals : MonoSingleton<SaveSignals>
    {
        public UnityAction onSaveGameData = delegate { };
    }
}