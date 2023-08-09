using Runtime.Data.ValueObject;
using UnityEngine;

namespace Runtime.Data.UnityObject
{
    [CreateAssetMenu(fileName = "CD_Collectable", menuName = "ATM_Rush/CD_Collectable", order = 0)]
    public class CD_Collectable : ScriptableObject
    {
        public CollectableData Data;
    }
}