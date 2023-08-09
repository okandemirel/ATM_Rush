using Runtime.Data.ValueObject;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Controllers.Collectables
{
    public class CollectableMeshController : MonoBehaviour
    {
        #region Self Variables

        #region Serialized Variables

        [SerializeField] private MeshFilter meshFilter;

        #endregion

        #region Private Variables

        [ShowInInspector] private CollectableMeshData _data;

        #endregion

        #endregion


        private void OnEnable()
        {
            ActivateMeshVisuals();
        }

        internal void SetMeshData(CollectableMeshData meshData)
        {
            _data = meshData;
        }

        private void ActivateMeshVisuals()
        {
            meshFilter.mesh = _data.MeshList[0];
        }

        internal void UpgradeCollectableVisual(int value)
        {
            meshFilter.mesh = _data.MeshList[value];
        }
    }
}