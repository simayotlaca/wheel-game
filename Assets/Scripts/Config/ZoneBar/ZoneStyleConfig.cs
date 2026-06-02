using UnityEngine;

namespace VertigoWheel
{
[CreateAssetMenu(fileName = "ZoneStyleConfig", menuName = "Vertigo Wheel/Config/Zone Style Config")]
public class ZoneStyleConfig : ScriptableObject
{
    [Header("Sprites")]
    public Sprite spriteNeutral;
    public Sprite spriteSafe;
    public Sprite spriteSuper;

    [Header("Body Tints")]
    public Color tintNormal = new Color(0.78f, 0.78f, 0.82f, 1f);
    public Color tintSafe   = new Color(0.55f, 1.00f, 0.40f, 1f);
    public Color tintSuper  = new Color(1.00f, 0.92f, 0.30f, 1f);

    [Header("Label Colors")]
    public Color labelNormal = new Color(0.05f, 0.05f, 0.08f, 1f);
    public Color labelSafe   = new Color(0.78f, 1f, 0.65f, 1f);
    public Color labelSuper  = Color.white;

    [Header("Item Colors")]
    public Color colorPast   = new Color(0.5f, 0.5f, 0.55f, 1f);
    public Color colorFuture = Color.white;

    [Header("Track Animation")]
    [Min(0)] public int activeSlotIndex = 6;
    [Min(0f)] public float slideDuration = 0.24f;
    [Min(0f)] public float slideAmount = 68f;

    [Header("Slot Past Fade")]
    [Range(0f, 1f)] public float pastAlphaBase = 0.75f;
    [Range(0f, 1f)] public float pastAlphaStep = 0.18f;
    [Range(0f, 1f)] public float pastAlphaMin = 0.18f;
}
}
