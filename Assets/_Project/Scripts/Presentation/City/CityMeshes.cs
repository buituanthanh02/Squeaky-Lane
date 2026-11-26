using System;
using UnityEngine;

namespace SqueakyLane.Presentation
{
    // Kết quả CityBuilder.Build: một mesh nền phẳng, cụm gần bãi và bốn vùng cảnh xa.
    public sealed class CityMeshes
    {
        public Mesh Ground { get; }
        public Mesh Near { get; }
        public Mesh North { get; }
        public Mesh South { get; }
        public Mesh West { get; }
        public Mesh East { get; }
        public CityMeshes(Mesh ground, Mesh near, Mesh north, Mesh south, Mesh west, Mesh east)
        {
            Ground = ground; Near = near; North = north; South = south; West = west; East = east;
        }
        public Mesh[] Far => new[] { North, South, West, East };
    }
}
