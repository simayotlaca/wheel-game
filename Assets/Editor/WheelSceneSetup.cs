#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class WheelSceneSetup
{
    internal const string SPRITES = "Assets/Sprites/Wheel";
    internal const string CONFIGS = "Assets/ScriptableObjects";
    const string PREFABS = "Assets/Prefabs";

    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Build Wheel Scene",
                "Exit Play mode before building the scene. The scene cannot be modified while playing.",
                "OK");
            return;
        }

        bool reimported = EnsureSpritesImported();
        if (reimported)
        {
            EditorApplication.delayCall += DoBuild;
            Debug.Log("[WheelSceneSetup] Sprites reimported. Building scene next frame...");
        }
        else
        {
            DoBuild();
        }
    }

    static void DoBuild()
    {
        EnsureDir(CONFIGS);
        EnsureDir(PREFABS);

        WheelConfig cfg = WheelConfigSceneBuilder.Build();

        GameObject slicePrefab = PrefabsSceneBuilder.MakeSliceViewPrefab();
        GameObject rewardItemPrefab = PrefabsSceneBuilder.MakeRewardItemPrefab();

        SpriteAtlasBuilder.Build();

        BuildScene(cfg, slicePrefab, rewardItemPrefab);

        UILayoutBuilder.ApplyFinalLayout();

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Sprite testSpr = Spr("ui_spin_bronze_base");
        if (testSpr == null)
            Debug.LogError("[WheelSceneSetup] Sprite still null after build! Check Assets/Sprites/Wheel folder.");
        else
            Debug.Log("[WheelSceneSetup] Done! Press Play to run.");
    }

    static void BuildScene(WheelConfig cfg,
        GameObject slicePrefab, GameObject rewardItemPrefab)
    {
        Destroy("WheelCanvas");
        Destroy("EventSystem");
        Destroy("WheelController");

        Destroy("ui_controller_wheel");
        Destroy("ui_sfx_binder");

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.10f, 1f);
        }

        var canvasGO = new GameObject("WheelCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var controllerGO = new GameObject("WheelController");
        var controller = controllerGO.AddComponent<WheelController>();

        var dynamicCanvasGO = new GameObject("ui_canvas_dynamic");
        dynamicCanvasGO.transform.SetParent(canvasGO.transform, false);
        FillParent(dynamicCanvasGO.AddComponent<RectTransform>());
        var dynamicCanvas = dynamicCanvasGO.AddComponent<Canvas>();
        dynamicCanvas.overrideSorting = true;
        dynamicCanvas.sortingOrder = 0;
        dynamicCanvasGO.AddComponent<GraphicRaycaster>();

        var overlayCanvasGO = new GameObject("ui_canvas_overlay");
        overlayCanvasGO.transform.SetParent(canvasGO.transform, false);
        FillParent(overlayCanvasGO.AddComponent<RectTransform>());
        var overlayCanvas = overlayCanvasGO.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = UICanvasOrders.OverlayLayer;
        overlayCanvasGO.AddComponent<GraphicRaycaster>();

        var staticCanvasGO = new GameObject("ui_canvas_static");
        staticCanvasGO.transform.SetParent(canvasGO.transform, false);
        FillParent(staticCanvasGO.AddComponent<RectTransform>());
        var staticCanvas = staticCanvasGO.AddComponent<Canvas>();
        staticCanvas.overrideSorting = true;
        staticCanvas.sortingOrder = 2;
        staticCanvasGO.AddComponent<GraphicRaycaster>();

        ZoneStripSceneBuilder.Build(staticCanvasGO);

        var wheelRefs = WheelCardSceneBuilder.Build(dynamicCanvasGO, slicePrefab, controller);

        UILayoutBuilder.Wire(controller, "config", cfg);
        UILayoutBuilder.Wire(controller, "wheelView", wheelRefs.wheelView);

        RewardListSceneBuilder.Build(staticCanvasGO, rewardItemPrefab, controller);
        DeathPopupSceneBuilder.Build(overlayCanvasGO, controller);

        var hudGO = new GameObject("ui_group_hud");
        hudGO.transform.SetParent(staticCanvasGO.transform, false);
        var hudRT = hudGO.AddComponent<RectTransform>();
        hudRT.anchorMin = new Vector2(0.5f, 0f);
        hudRT.anchorMax = new Vector2(0.5f, 0f);
        hudRT.pivot = new Vector2(0.5f, 0f);
        hudRT.sizeDelta = new Vector2(900, 60);
        hudRT.anchoredPosition = new Vector2(0, 20);

        var zoneHud = hudGO.AddComponent<ZoneHUD>();
        var spinHint = hudGO.AddComponent<SpinHintUI>();

        var spinButton = wheelRefs.centerBtn != null ? wheelRefs.centerBtn.GetComponent<Button>() : null;
        var spinAnimator = wheelRefs.centerBtn != null ? wheelRefs.centerBtn.GetComponent<SpinButtonAnimator>() : null;
        var staticCanvasGroup = staticCanvasGO.GetComponent<CanvasGroup>();
        if (staticCanvasGroup == null) staticCanvasGroup = staticCanvasGO.AddComponent<CanvasGroup>();

        UILayoutBuilder.Wire(zoneHud, "spinButton", spinButton);
        UILayoutBuilder.Wire(zoneHud, "spinAnimator", spinAnimator);
        UILayoutBuilder.Wire(zoneHud, "zoneGroup", staticCanvasGroup);
        UILayoutBuilder.Wire(zoneHud, "zoneCanvas", staticCanvas);

        UILayoutBuilder.Wire(spinHint, "spinButtonScaleRoot", wheelRefs.centerScale);

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    internal static GameObject Overlay(string name, Transform parent, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        FillParent(go.AddComponent<RectTransform>());

        var content = new GameObject("ui_overlay_content");
        content.transform.SetParent(go.transform, false);
        FillParent(content.AddComponent<RectTransform>());

        var backdrop = new GameObject("ui_image_overlay_backdrop");
        backdrop.transform.SetParent(content.transform, false);
        FillParent(backdrop.AddComponent<RectTransform>());
        backdrop.AddComponent<CanvasRenderer>();
        var bd = backdrop.AddComponent<Image>();
        bd.color = DeathOverlayStyle.MainBackdropDim;
        bd.raycastTarget = true;
        bd.maskable = false;

        var panelRoot = new GameObject("ui_group_panel_root");
        panelRoot.transform.SetParent(content.transform, false);
        var prRT = panelRoot.AddComponent<RectTransform>();
        prRT.anchorMin = new Vector2(0.5f, 0.5f);
        prRT.anchorMax = new Vector2(0.5f, 0.5f);
        prRT.pivot = new Vector2(0.5f, 0.5f);
        prRT.sizeDelta = new Vector2(w, h);
        prRT.localScale = Vector3.zero;
        panelRoot.AddComponent<CanvasRenderer>();
        var bg = panelRoot.AddComponent<Image>();
        bg.sprite = Spr("ui_card_frame_12px_neutral");
        bg.type = Image.Type.Sliced;
        bg.fillCenter = true;
        bg.color = DeathOverlayStyle.MainPanelFill;
        DisableRaycast(bg);

        var panelOutline = new GameObject("ui_image_panel_outline");
        panelOutline.transform.SetParent(panelRoot.transform, false);
        FillParent(panelOutline.AddComponent<RectTransform>());
        panelOutline.AddComponent<CanvasRenderer>();
        var outImg = panelOutline.AddComponent<Image>();
        outImg.sprite = Spr("ui_card_zone_map_frame");
        outImg.type = Image.Type.Sliced;
        outImg.color = DeathOverlayStyle.MainPanelOutline;
        DisableRaycast(outImg);

        content.SetActive(false);
        return go;
    }

    internal static GameObject Btn(string name, Transform parent, string label, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.28f, 1f);
        img.type = Image.Type.Sliced;

        img.maskable = false;
        go.AddComponent<Button>();

        var txtGO = new GameObject("ui_label_button_text");
        txtGO.transform.SetParent(go.transform, false);
        FillParent(txtGO.AddComponent<RectTransform>());
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 16;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.raycastTarget = false;
        return go;
    }

    internal static GameObject TMPGo(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        return go;
    }

    internal static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    internal static void FillParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    internal static void DisableRaycast(MaskableGraphic graphic)
    {
        if (graphic == null) return;
        graphic.raycastTarget = false;
        graphic.maskable = false;
    }

    internal static AnimationCurve BuildEaseOutCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f,    0f,    0f, 3.5f),
            new Keyframe(0.55f, 0.85f, 1f, 1f),
            new Keyframe(1f,    1f,    0f, 0f)
        );
    }

    static bool EnsureSpritesImported()
    {
        string[] guids = AssetDatabase.FindAssets("", new[] { SPRITES });
        bool any = false;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png") && !path.EndsWith(".jpg")) continue;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            any = true;
        }
        if (any) AssetDatabase.Refresh();
        return any;
    }

    internal static Sprite Spr(string name)
    {
        foreach (var ext in new[] { ".png", ".jpg" })
        {
            string path = $"{SPRITES}/{name}{ext}";
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sp) return sp;
        }
        Debug.LogWarning($"[WheelSceneSetup] Sprite not found: {name}");
        return null;
    }

    internal static T Load<T>(string path) where T : Object
        => AssetDatabase.LoadAssetAtPath<T>(path);

    internal static T Create<T>(string path) where T : ScriptableObject
    {
        var obj = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(obj, path);
        return obj;
    }

    internal static void Dirty(Object obj) => EditorUtility.SetDirty(obj);

    internal static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        if (!AssetDatabase.IsValidFolder(parent)) EnsureDir(parent);
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }

    static void Destroy(string name)
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == name)
                Object.DestroyImmediate(roots[i]);
        }
    }
}
#endif
