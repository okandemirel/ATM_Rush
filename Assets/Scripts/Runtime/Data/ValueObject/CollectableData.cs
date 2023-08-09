using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Data.ValueObject
{
    [Serializable]
    public struct CollectableData
    {
        public CollectableMeshData MeshData;
    }

    [Serializable]
    public struct CollectableMeshData
    {
        public List<Mesh> MeshList;
    }
}