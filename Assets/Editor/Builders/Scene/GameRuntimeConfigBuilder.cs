using UnityEditor;
using UnityEngine;

internal static class GameRuntimeConfigBuilder
{
    private const string GameObjectName = "GameRuntimeConfig";

    public static void Build()
    {
        var existing = Object.FindObjectsOfType<GameRuntimeConfig>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null) Undo.DestroyObjectImmediate(existing[i].gameObject);
        }

        GameObject go = new GameObject(GameObjectName);
        Undo.RegisterCreatedObjectUndo(go, UILayoutBuilder.UndoLabel);
        Undo.AddComponent<GameRuntimeConfig>(go);
    }
}
