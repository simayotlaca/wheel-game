#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

internal static class WheelCardSceneBuilder
{
    internal struct Refs
    {
        public GameObject centerBtn;
        public RectTransform centerScale;
        public WheelView wheelView;
    }

    public static Refs Build(GameObject dynamicCanvasGO, GameObject slicePrefab, WheelController controller)
    {
        var wheelCardGO = new GameObject("ui_card_wheel");
        wheelCardGO.transform.SetParent(dynamicCanvasGO.transform, false);
        var wcRT = wheelCardGO.AddComponent<RectTransform>();
        wcRT.anchorMin = new Vector2(0.5f, 1f);
        wcRT.anchorMax = new Vector2(0.5f, 1f);
        wcRT.pivot = new Vector2(0.5f, 1f);
        wcRT.sizeDelta = new Vector2(1100, 1300);
        wcRT.anchoredPosition = new Vector2(0, -174);

        var wcBgGO = new GameObject("ui_image_wheel_card_bg");
        wcBgGO.transform.SetParent(wheelCardGO.transform, false);
        var wcBgRT = wcBgGO.AddComponent<RectTransform>();
        WheelSceneSetup.FillParent(wcBgRT);
        wcBgGO.AddComponent<CanvasRenderer>();
        var wcBgImg = wcBgGO.AddComponent<Image>();
        wcBgImg.sprite = WheelSceneSetup.Spr("ui_card_frame_12px_neutral");
        wcBgImg.type = Image.Type.Sliced;
        wcBgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        WheelSceneSetup.DisableRaycast(wcBgImg);

        var wheelViewGO = new GameObject("wheel_root");
        wheelViewGO.transform.SetParent(wheelCardGO.transform, false);
        var wvRT = wheelViewGO.AddComponent<RectTransform>();
        wvRT.anchorMin = new Vector2(0.5f, 1f);
        wvRT.anchorMax = new Vector2(0.5f, 1f);
        wvRT.pivot = new Vector2(0.5f, 1f);
        wvRT.sizeDelta = new Vector2(WheelPointerStyle.WheelDiameter, WheelPointerStyle.WheelDiameter);
        wvRT.anchoredPosition = new Vector2(0, -130);

        var wheelARF = wheelViewGO.AddComponent<AspectRatioFitter>();
        wheelARF.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        wheelARF.aspectRatio = 1f;

        var wheelView = wheelViewGO.AddComponent<WheelView>();

        var rotatingLayerGO = new GameObject("rotating_reward_layer");
        rotatingLayerGO.transform.SetParent(wheelViewGO.transform, false);
        var rotatingLayerRT = rotatingLayerGO.AddComponent<RectTransform>();
        WheelSceneSetup.FillParent(rotatingLayerRT);

        var baseGO = new GameObject("wheel_background");
        baseGO.transform.SetParent(rotatingLayerGO.transform, false);
        var baseRT = baseGO.AddComponent<RectTransform>();
        WheelSceneSetup.FillParent(baseRT);
        baseGO.AddComponent<CanvasRenderer>();
        var baseImg = baseGO.AddComponent<Image>();
        baseImg.sprite = WheelSceneSetup.Spr("ui_spin_bronze_base");
        baseImg.preserveAspect = true;
        WheelSceneSetup.DisableRaycast(baseImg);

        var frameGO = new GameObject("wheel_frame");
        frameGO.transform.SetParent(wheelViewGO.transform, false);
        var frameRT = frameGO.AddComponent<RectTransform>();
        WheelSceneSetup.FillParent(frameRT);
        frameGO.AddComponent<CanvasRenderer>();
        var frameImg = frameGO.AddComponent<Image>();
        frameImg.sprite = null;
        frameImg.enabled = false;
        frameImg.preserveAspect = true;
        WheelSceneSetup.DisableRaycast(frameImg);

        var arrowGO = new GameObject("wheel_pointer");
        arrowGO.transform.SetParent(wheelViewGO.transform, false);
        arrowGO.AddComponent<CanvasRenderer>();
        var arrRT = arrowGO.AddComponent<RectTransform>();
        arrRT.anchorMin = WheelPointerStyle.AnchorMin;
        arrRT.anchorMax = WheelPointerStyle.AnchorMax;
        arrRT.pivot     = WheelPointerStyle.Pivot;
        arrRT.sizeDelta = WheelPointerStyle.Size;
        arrRT.anchoredPosition = WheelPointerStyle.Position;
        var arrImg = arrowGO.AddComponent<Image>();
        arrImg.sprite = WheelSceneSetup.Spr(WheelPointerStyle.SpriteName);
        arrImg.preserveAspect = WheelPointerStyle.PreserveAspect;
        WheelSceneSetup.DisableRaycast(arrImg);
        arrowGO.AddComponent<IndicatorPulse>();

        var centerGlowGO = new GameObject("ui_image_center_glow");
        centerGlowGO.transform.SetParent(wheelViewGO.transform, false);
        centerGlowGO.AddComponent<CanvasRenderer>();
        var cgRT = centerGlowGO.AddComponent<RectTransform>();
        cgRT.anchorMin = new Vector2(0.5f, 0.5f);
        cgRT.anchorMax = new Vector2(0.5f, 0.5f);
        cgRT.pivot = new Vector2(0.5f, 0.5f);
        cgRT.sizeDelta = new Vector2(380, 380);
        cgRT.anchoredPosition = Vector2.zero;
        var cgImg = centerGlowGO.AddComponent<Image>();
        cgImg.sprite = WheelSceneSetup.Spr("star_glow_alpha");
        cgImg.color = new Color(1f, 0.9f, 0.5f, 0.55f);
        cgImg.preserveAspect = true;
        WheelSceneSetup.DisableRaycast(cgImg);

        var centerBtnGO = WheelSceneSetup.Btn("ui_button_spin_center", wheelViewGO.transform, "SPIN", new Vector2(220, 110));
        var centerBtnRT = centerBtnGO.GetComponent<RectTransform>();
        centerBtnRT.anchorMin = new Vector2(0.5f, 0.5f);
        centerBtnRT.anchorMax = new Vector2(0.5f, 0.5f);
        centerBtnRT.pivot = new Vector2(0.5f, 0.5f);
        centerBtnRT.anchoredPosition = Vector2.zero;
        var centerBtnImg = centerBtnGO.GetComponent<Image>();
        centerBtnImg.sprite = WheelSceneSetup.Spr("ui_spin_generic_button");
        centerBtnImg.type = Image.Type.Sliced;
        centerBtnImg.color = Color.white;
        centerBtnImg.preserveAspect = true;
        var centerBtnTxt = centerBtnGO.GetComponentInChildren<TextMeshProUGUI>();
        centerBtnTxt.text = string.Empty;
        centerBtnTxt.enabled = false;

        var spinAnim = centerBtnGO.GetComponent<SpinButtonAnimator>();
        if (spinAnim == null) spinAnim = centerBtnGO.AddComponent<SpinButtonAnimator>();

        var centerScaleGO = new GameObject("ui_group_scale_root");
        centerScaleGO.transform.SetParent(centerBtnGO.transform, false);
        var centerScaleRT = centerScaleGO.AddComponent<RectTransform>();
        WheelSceneSetup.FillParent(centerScaleRT);
        var centerExistingLabel = centerBtnGO.transform.Find("ui_label_button_text");
        if (centerExistingLabel != null) centerExistingLabel.SetParent(centerScaleGO.transform, false);

        var spinAnimator = wheelViewGO.GetComponent<WheelSpinAnimator>();
        if (spinAnimator == null) spinAnimator = wheelViewGO.AddComponent<WheelSpinAnimator>();

        var slicePrefabComp = slicePrefab != null ? slicePrefab.GetComponent<SliceView>() : null;

        UILayoutBuilder.Wire(wheelView, "rotatingRewardLayer", rotatingLayerRT);
        UILayoutBuilder.Wire(wheelView, "wheelBaseImage", baseImg);
        UILayoutBuilder.Wire(wheelView, "wheelFrameImage", frameImg);
        UILayoutBuilder.Wire(wheelView, "wheelIndicatorImage", arrImg);
        UILayoutBuilder.Wire(wheelView, "slicePrefab", slicePrefabComp);
        UILayoutBuilder.Wire(wheelView, "spinAnimator", spinAnimator);

        var indicatorPulse = arrowGO.GetComponent<IndicatorPulse>();
        UILayoutBuilder.Wire(spinAnimator, "rotating_reward_layer", rotatingLayerRT);
        UILayoutBuilder.Wire(spinAnimator, "indicator_pulse", indicatorPulse);

        return new Refs
        {
            centerBtn = centerBtnGO,
            centerScale = centerScaleRT,
            wheelView = wheelView,
        };
    }
}
#endif
