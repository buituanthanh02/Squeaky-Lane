using System;
using UnityEngine;
using SqueakyLane.Core.Model;

namespace SqueakyLane.Game
{
    [CreateAssetMenu(menuName = "Squeaky Lane/Car Catalog")]
    public sealed class CarCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public TileColor color;
            public GameObject carPrefab;
            public GameObject truckPrefab;
            public Material material;
        }

        [SerializeField, Tooltip("Mỗi màu một dòng; hai prefab cùng dùng material/colormap của màu đó.")]
        private Entry[] entries = Array.Empty<Entry>();

        public Entry Find(TileColor color)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].color == color)
                {
                    return entries[i];
                }
            }
            return null;
        }

        public GameObject Prefab(TileColor color, bool truck)
        {
            var entry = Find(color);
            return entry == null ? null : truck ? entry.truckPrefab : entry.carPrefab;
        }

        private void OnValidate()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null)
                {
                    continue;
                }
                if (entries[i].color == TileColor.Water)
                {
                    Debug.LogError("CarCatalog: water chỉ dùng cho cổng, không phải màu xe.", this);
                }
                for (int j = 0; j < i; j++)
                {
                    if (entries[j] != null && entries[j].color == entries[i].color)
                    {
                        Debug.LogError("CarCatalog trùng màu " + entries[i].color, this);
                    }
                }
            }
        }
    }
}
