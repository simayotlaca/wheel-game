using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class CanvasSetupPass
{
    public static void Apply()
    {
        ConfigureCanvasScaler("WheelCanvas");

        BackgroundBuilder.Build();

        UILayoutHelpers.StripComponent<AspectRatioFitter>("wheel_root");
    }

    private static void ConfigureCanvasScaler(string name)
    {
        var list = UILayoutHelpers.FindAllInScene(name);
        if (list.Count == 0) { Debug.LogWarning("[UILayoutBuilder] Not found: " + name); return; }
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            CanvasScaler scaler = go.GetComponentInParent<CanvasScaler>(true);
            if (scaler == null) { Debug.LogWarning("[UILayoutBuilder] No CanvasScaler on " + name + " or any ancestor"); continue; }
            Undo.RecordObject(scaler, UILayoutBuilder.UndoLabel);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            EditorUtility.SetDirty(scaler);
            Debug.Log("[CanvasScaler] " + name + " (scaler on '" + scaler.gameObject.name + "') → 1920x1080 ScaleWithScreenSize Expand");
        }
    }
}
