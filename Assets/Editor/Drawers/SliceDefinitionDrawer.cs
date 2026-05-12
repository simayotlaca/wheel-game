using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ZoneConfig))]
public class ZoneConfigEditor : Editor
{
    SerializedProperty typeProp;
    SerializedProperty headerLabelProp;
    SerializedProperty headerColorProp;
    SerializedProperty subtitleProp;
    SerializedProperty wheelBaseProp;
    SerializedProperty wheelFrameProp;
    SerializedProperty wheelIndicatorProp;
    SerializedProperty frameTintProp;
    SerializedProperty slicesProp;

    void OnEnable()
    {
        typeProp = serializedObject.FindProperty("type");
        headerLabelProp = serializedObject.FindProperty("headerLabel");
        headerColorProp = serializedObject.FindProperty("headerColor");
        subtitleProp = serializedObject.FindProperty("subtitle");
        wheelBaseProp = serializedObject.FindProperty("wheelBase");
        wheelFrameProp = serializedObject.FindProperty("wheelFrame");
        wheelIndicatorProp = serializedObject.FindProperty("wheelIndicator");
        frameTintProp = serializedObject.FindProperty("frameTint");
        slicesProp = serializedObject.FindProperty("slices");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(typeProp);
        EditorGUILayout.PropertyField(headerLabelProp);
        EditorGUILayout.PropertyField(headerColorProp);
        EditorGUILayout.PropertyField(subtitleProp);
        EditorGUILayout.PropertyField(wheelBaseProp);
        EditorGUILayout.PropertyField(wheelFrameProp);
        EditorGUILayout.PropertyField(wheelIndicatorProp);
        EditorGUILayout.PropertyField(frameTintProp);

        EditorGUILayout.Space(10);
        DrawSlicesHeader();
        DrawSlicesList();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawSlicesHeader()
    {
        float totalWeight = 0f;
        int validCount = 0;
        for (int i = 0; i < slicesProp.arraySize; i++)
        {
            var s = slicesProp.GetArrayElementAtIndex(i).objectReferenceValue as SliceDefinition;
            if (s != null) { totalWeight += s.weight; validCount++; }
        }
        var rect = EditorGUILayout.GetControlRect();
        EditorGUI.LabelField(
            rect,
            $"Slices  ({validCount}/{slicesProp.arraySize})   Σ weight: {totalWeight:0.##}",
            EditorStyles.boldLabel);
    }

    void DrawSlicesList()
    {
        for (int i = 0; i < slicesProp.arraySize; i++)
        {
            DrawSliceRow(i);
        }

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ Add Slice", GUILayout.Width(110)))
            {
                int newIdx = slicesProp.arraySize;
                slicesProp.arraySize++;
                slicesProp.GetArrayElementAtIndex(newIdx).objectReferenceValue = null;
            }
            GUILayout.FlexibleSpace();
        }
    }

    void DrawSliceRow(int index)
    {
        var element = slicesProp.GetArrayElementAtIndex(index);
        var slice = element.objectReferenceValue as SliceDefinition;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"#{index}", GUILayout.Width(28));

                Rect iconRect = GUILayoutUtility.GetRect(40, 40,
                    GUILayout.Width(40), GUILayout.Height(40));
                DrawIconPreview(iconRect, slice);

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.PropertyField(element, GUIContent.none);
                    if (slice != null) DrawInlineSliceFields(slice);
                }

                if (GUILayout.Button("x", GUILayout.Width(22), GUILayout.Height(22)))
                {

                    if (element.objectReferenceValue != null)
                        element.objectReferenceValue = null;
                    slicesProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                    return;
                }
            }

            DrawValidationChips(slice, index);
        }
    }

    void DrawIconPreview(Rect rect, SliceDefinition slice)
    {
        EditorGUI.DrawRect(rect, new Color(0.14f, 0.14f, 0.17f, 1f));

        if (slice == null || slice.reward == null) return;

        var sprite = slice.reward.icon;
        if (sprite != null)
        {
            var preview = AssetPreview.GetAssetPreview(sprite);
            var tex = preview != null ? preview : sprite.texture;
            if (tex != null)
            {
                var inset = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
                GUI.DrawTexture(inset, tex, ScaleMode.ScaleToFit);
            }
        }

        if (slice.reward.isDeath)
        {
            var badge = new Rect(rect.x, rect.yMax - 12, rect.width, 12);
            EditorGUI.DrawRect(badge, new Color(0.78f, 0.15f, 0.18f, 0.92f));
            GUI.Label(badge, "BOMB", BombBadgeStyle);
        }
    }

    static GUIStyle _bombBadgeStyle;
    public static GUIStyle BombBadgeStyle
    {
        get
        {
            if (_bombBadgeStyle == null)
            {
                _bombBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 8
                };
                _bombBadgeStyle.normal.textColor = Color.white;
            }
            return _bombBadgeStyle;
        }
    }

    void DrawInlineSliceFields(SliceDefinition slice)
    {
        var so = new SerializedObject(slice);
        so.Update();
        var rewardProp = so.FindProperty("reward");
        var amountProp = so.FindProperty("amount");
        var weightProp = so.FindProperty("weight");

        EditorGUILayout.PropertyField(rewardProp, GUIContent.none);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Amount", GUILayout.Width(50));
            EditorGUILayout.PropertyField(amountProp, GUIContent.none, GUILayout.Width(60));
            GUILayout.Label("Weight", GUILayout.Width(46));
            EditorGUILayout.PropertyField(weightProp, GUIContent.none, GUILayout.Width(60));
            GUILayout.FlexibleSpace();

            if (slice.reward != null)
            {
                string label = !string.IsNullOrEmpty(slice.reward.displayName)
                    ? slice.reward.displayName
                    : slice.reward.name;
                GUILayout.Label(label, EditorStyles.miniLabel);
            }
        }

        so.ApplyModifiedProperties();
    }

    void DrawValidationChips(SliceDefinition slice, int index)
    {
        if (slice == null)
        {
            DrawChip($"slice slot is empty");
            return;
        }
        if (slice.reward == null) DrawChip("reward not assigned");
        if (slice.weight <= 0f) DrawChip("weight must be > 0");
        if (slice.amount < 0) DrawChip("amount must be >= 0");
    }

    void DrawChip(string text)
    {
        var content = new GUIContent("  ! " + text);
        var rect = GUILayoutUtility.GetRect(content, EditorStyles.miniLabel);
        EditorGUI.DrawRect(rect, new Color(0.78f, 0.18f, 0.18f, 0.35f));
        GUI.Label(rect, content, EditorStyles.miniLabel);
    }
}

[CustomEditor(typeof(SliceDefinition))]
public class SliceDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var slice = (SliceDefinition)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            Rect iconRect = GUILayoutUtility.GetRect(48, 48,
                GUILayout.Width(48), GUILayout.Height(48));
            EditorGUI.DrawRect(iconRect, new Color(0.14f, 0.14f, 0.17f, 1f));

            if (slice.reward != null && slice.reward.icon != null)
            {
                var sprite = slice.reward.icon;
                var preview = AssetPreview.GetAssetPreview(sprite);
                var tex = preview != null ? preview : sprite.texture;
                if (tex != null)
                {
                    var inset = new Rect(iconRect.x + 2, iconRect.y + 2,
                        iconRect.width - 4, iconRect.height - 4);
                    GUI.DrawTexture(inset, tex, ScaleMode.ScaleToFit);
                }

                if (slice.reward.isDeath)
                {
                    var badge = new Rect(iconRect.x, iconRect.yMax - 14, iconRect.width, 14);
                    EditorGUI.DrawRect(badge, new Color(0.78f, 0.15f, 0.18f, 0.92f));
                    GUI.Label(badge, "BOMB", ZoneConfigEditor.BombBadgeStyle);
                }
            }

            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label(slice.name, EditorStyles.boldLabel);
                if (slice.reward != null)
                {
                    string label = !string.IsNullOrEmpty(slice.reward.displayName)
                        ? slice.reward.displayName
                        : slice.reward.name;
                    GUILayout.Label(label, EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label("(no reward assigned)", EditorStyles.miniLabel);
                }
            }
        }

        EditorGUILayout.Space(6);
        DrawDefaultInspector();
    }
}
