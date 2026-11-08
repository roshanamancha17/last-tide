using UnityEngine;

namespace LastTide
{
    /// <summary>
    /// Stops drawing small scenery once it is tiny on screen, to save phone draw calls.
    /// Size-based, so large objects such as the terrain, palms and quest structures are never hidden.
    /// </summary>
    public static class DetailCulling
    {
        /// <summary>Objects bigger than this are always drawn.</summary>
        public const float LargestCulledSize = 4f;

        /// <summary>
        /// The fraction of the screen's height below which an object of this size stops being drawn, or 0 for never.
        /// Small things vanish sooner (relative to their size) because they matter less.
        /// </summary>
        public static float CullScreenHeight(float size)
        {
            if (size <= 0f || size > LargestCulledSize)
            {
                return 0f;
            }

            return size <= 0.8f ? 0.02f : 0.012f;
        }

        /// <summary>Adds distance culling to each direct child of a scenery group. Returns how many were set up.</summary>
        public static int Apply(Transform group)
        {
            int count = 0;
            foreach (Transform child in group)
            {
                if (child.GetComponentInChildren<LODGroup>() != null)
                {
                    continue;
                }

                Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    continue;
                }

                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                float height = CullScreenHeight(Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z));
                if (height <= 0f)
                {
                    continue;
                }

                LODGroup lod = child.gameObject.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(height, renderers) });
                lod.RecalculateBounds();
                count++;
            }

            return count;
        }
    }
}
