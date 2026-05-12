using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal static class RunExitControllerBuilder
{
    private const string GameObjectName = "RunExitController";

    public static void Build()
    {

        var existing = Object.FindObjectsOfType<RunExitController>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null) Undo.DestroyObjectImmediate(existing[i].gameObject);
        }

        WheelController wheel = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<WheelController>());
        Transform parent = wheel != null ? wheel.transform : null;

        GameObject go = new GameObject(GameObjectName);
        Undo.RegisterCreatedObjectUndo(go, UILayoutBuilder.UndoLabel);
        if (parent != null) Undo.SetTransformParent(go.transform, parent, UILayoutBuilder.UndoLabel);

        RunExitController controller = go.GetComponent<RunExitController>();
        if (controller == null) controller = Undo.AddComponent<RunExitController>(go);

        ExitConfirmPanel exitPanel  = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<ExitConfirmPanel>());
        DeathGameOverPanel deathPanel = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<DeathGameOverPanel>());
        DeathConfirmPanel deathConfirmPanel = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<DeathConfirmPanel>());
        RewardCollectAnimator anim  = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<RewardCollectAnimator>());
        GameObject exitBtnGO = UILayoutBuilder.FindFirstInScene("ui_panel_reward_list_exit_header");
        Button exitBtn = exitBtnGO != null ? exitBtnGO.GetComponent<Button>() : null;

        UILayoutBuilder.Wire(controller, "wheel", wheel);
        UILayoutBuilder.Wire(controller, "exitConfirmPanel", exitPanel);
        UILayoutBuilder.Wire(controller, "deathPanel", deathPanel);
        UILayoutBuilder.Wire(controller, "deathConfirmPanel", deathConfirmPanel);
        UILayoutBuilder.Wire(controller, "collectAnimator", anim);
        UILayoutBuilder.Wire(controller, "exitButton", exitBtn);

        ZoneHUD zoneHUD = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<ZoneHUD>());
        BlurBackgroundOverlay blur = UILayoutBuilder.FirstSceneInstance(Resources.FindObjectsOfTypeAll<BlurBackgroundOverlay>());

        MonoBehaviour[] panelsNeedingController = { exitPanel, deathPanel, deathConfirmPanel, zoneHUD, blur };
        for (int i = 0; i < panelsNeedingController.Length; i++)
        {
            MonoBehaviour panel = panelsNeedingController[i];
            if (panel != null) UILayoutBuilder.Wire(panel, "exitController", controller);
        }

        int wiredBack = 0;
        for (int i = 0; i < panelsNeedingController.Length; i++)
            if (panelsNeedingController[i] != null) wiredBack++;

        Debug.Log(string.Format(
            "[RunExitControllerBuilder] wheel={0} exitPanel={1} deathPanel={2} deathConfirm={3} anim={4} exitBtn={5}",
            wheel != null, exitPanel != null, deathPanel != null, deathConfirmPanel != null, anim != null, exitBtn != null));
        Debug.Log(string.Format(
            "[RunExitControllerBuilder] back-pointers wired: {0}/{1} (exitPanel, deathPanel, deathConfirm, zoneHUD, blur)",
            wiredBack, panelsNeedingController.Length));
    }
}
