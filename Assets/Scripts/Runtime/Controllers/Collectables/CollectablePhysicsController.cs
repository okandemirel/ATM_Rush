using Runtime.Managers;
using Runtime.Signals;
using UnityEngine;

namespace Runtime.Controllers.Collectables
{
    public class CollectablePhysicsController : MonoBehaviour
    {
        #region Self Variables

        #region Serialized Variables

        [SerializeField] private CollectableManager manager;

        #endregion

        #region Private Variables

        private readonly string _collectable = "Collectable";
        private readonly string _collected = "Collected";
        private readonly string _gate = "Gate";
        private readonly string _atm = "ATM";
        private readonly string _obstacle = "Obstacle";
        private readonly string _conveyor = "Conveyor";

        #endregion

        #endregion

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_collectable) && CompareTag(_collected))
            {
                other.tag = _collected;
                manager.InteractionWithCollectable(other.transform.parent.gameObject);
            }

            if (other.CompareTag(_gate) && CompareTag(_collected))
            {
                manager.CollectableUpgrade(manager.GetCurrentValue());
            }

            if (other.CompareTag(_atm) && CompareTag(_collected))
            {
                manager.InteractionWithAtm(transform.parent.gameObject);
            }

            if (other.CompareTag(_obstacle) && CompareTag(_collected))
            {
                manager.InteractionWithObstacle(transform.parent.gameObject);
            }

            if (other.CompareTag(_conveyor) && CompareTag(_collected))
            {
                manager.InteractionWithConveyor();
            }
        }
    }
}