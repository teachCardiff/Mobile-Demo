//using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Defines which regions of a UI cat image are favorable or unfavorable to pet.
/// This attaches to the same GameObject as the cat's RectTransform.
///
/// Zones are defined as RectTransforms in the scene:
/// - Create empty UI RectTransforms as children of the cat image to visualize zones.
///
/// You can tweak these in the inspector to match your cat art.
/// </summary>
[DisallowMultipleComponent]
public class CatPettingZones : MonoBehaviour
{
    public enum PetZoneType
    {
        Neutral,
        Favorable,
        Unfavorable
    }

    [System.Serializable]
    public struct Zone
    {
        public PetZoneType type;

        [Tooltip("A RectTransform that defines this zone (typically an empty UI object under the cat image).")]
        public RectTransform zoneRect;

        [Range(0f, 5f)]
        public float scoreMultiplier;

        [Range(0f, 5f)]
        public float irritationMultiplier;
    }

    [Header("Zones")]
    [Tooltip("Order matters: first matching zone wins. Create empty UI RectTransforms as children of the cat image to visualize zones.")]
    [SerializeField] private Zone[] zones;

    [Header("Defaults")]
    [SerializeField] private float defaultScoreMultiplier = 1f;
    [SerializeField] private float defaultIrritationMultiplier = 1f;

    void Start()
    {
        foreach(Zone zone in zones)
        {
            Image img = zone.zoneRect.GetComponent<Image>();
            if (img != null)
                zone.zoneRect.GetComponent<Image>().enabled = false;
        }
    }

    /// <summary>
    /// Tries to find the first configured zone that contains the point.
    /// List order determines priority (index 0 wins).
    /// </summary>
    public bool TryGetZone(RectTransform catRect, Vector2 normalizedPoint, out Zone zone)
    {
        zone = default;

        if (catRect != null && zones != null)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                var z = zones[i];
                if (z.zoneRect == null)
                    continue;

                if (IsPointInsideZone(catRect, normalizedPoint, z.zoneRect))
                {
                    zone = z;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns multipliers for score and irritation based on a normalized point (0..1) on the cat rect.
    /// </summary>
    public void GetMultipliers(RectTransform catRect, Vector2 normalizedPoint, out float scoreMult, out float irritationMult)
    {
        if (TryGetZone(catRect, normalizedPoint, out var z))
        {
            scoreMult = z.scoreMultiplier <= 0f ? 1f : z.scoreMultiplier;
            irritationMult = z.irritationMultiplier <= 0f ? 1f : z.irritationMultiplier;
            return;
        }

        scoreMult = defaultScoreMultiplier;
        irritationMult = defaultIrritationMultiplier;
    }

    private static bool IsPointInsideZone(RectTransform catRect, Vector2 catNormalizedPoint, RectTransform zoneRect)
    {
        // Convert the cat normalized point (0..1) to a world point.
        Vector2 catLocal;
        var r = catRect.rect;
        catLocal.x = Mathf.Lerp(r.xMin, r.xMax, catNormalizedPoint.x);
        catLocal.y = Mathf.Lerp(r.yMin, r.yMax, catNormalizedPoint.y);

        Vector3 worldPoint = catRect.TransformPoint(catLocal);

        // Check if that world point is inside the zone rect (in the zone's local space).
        Vector2 zoneLocal = zoneRect.InverseTransformPoint(worldPoint);
        return zoneRect.rect.Contains(zoneLocal);
    }
}
