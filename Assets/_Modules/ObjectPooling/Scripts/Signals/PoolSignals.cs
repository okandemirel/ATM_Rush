using System;
using _Modules.ObjectPooling.Scripts.Enums;
using Runtime.Extentions;
using UnityEngine;
using UnityEngine.Events;

namespace _Modules.ObjectPooling.Scripts.Signals
{
    public class PoolSignals : MonoSingleton<PoolSignals>
    {
        public Func<PoolType, GameObject> onDequeuePoolableGameObject = delegate { return null; };
        public UnityAction<GameObject, PoolType> onEnqueuePooledGameObject = delegate { };
    }
}