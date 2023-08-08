using Runtime.Extentions;
using UnityEngine.Events;

namespace Runtime.Signals
{
    public class AtmSignals : MonoSingleton<AtmSignals>
    {
        public UnityAction<int> onSetAtmScoreText = delegate { };
    }
}