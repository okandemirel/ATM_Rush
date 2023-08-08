using System;
using Runtime.Extentions;
using UnityEngine.Events;

namespace Runtime.Signals
{
    public class UISignals : MonoSingleton<UISignals>
    {
        public UnityAction onSetIncomeLvlText = delegate { };
        public UnityAction onSetStackLvlText = delegate { };
        public UnityAction<byte> onSetNewLevelValue = delegate { };
        public UnityAction<int> onSetMoneyValue = delegate { };
        public Func<int> onGetMoveValue = delegate { return 0; };
    }
}