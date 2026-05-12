#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class VertigoMasterMenu
{
    [MenuItem("Vertigo/Build/Full Rebuild", priority = 0)]
    public static void FullRebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Vertigo Full Rebuild",
                "Exit Play mode before running Full Rebuild.", "OK");
            return;
        }

        try
        {
            WheelDistributionApplier.Apply();

            WheelSceneSetup.Build();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VertigoMasterMenu] Full Rebuild failed: {e}");
            throw;
        }
    }

    public static void BuildUILayoutOnly()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Vertigo UI Layout",
                "Exit Play mode before re-applying the UI layout.", "OK");
            return;
        }
        UILayoutBuilder.ApplyFinalLayout();
        AssetDatabase.SaveAssets();
    }

    public static void DiagnoseLayout() => UILayoutBuilder.DiagnoseLayout();
    public static void AuditRaycastTargets() => RaycastTargetAuditor.Audit();
}
#endif
