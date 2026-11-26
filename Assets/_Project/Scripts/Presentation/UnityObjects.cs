using UnityEngine;

namespace SqueakyLane.Presentation
{
    // Hủy object Unity đúng cách cả trong Play lẫn ngoài Play (test EditMode, preview của Level Editor).
    internal static class UnityObjects
    {
        public static void Release(Object value)
        {
            if (value == null) { return; }
            if (Application.isPlaying) { Object.Destroy(value); }
            else { Object.DestroyImmediate(value); }
        }
    }
}
