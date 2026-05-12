using System.Diagnostics;
using UnityEngine;

public static class DebugLogger
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string msg) => UnityEngine.Debug.Log(msg);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string msg, Object context) => UnityEngine.Debug.Log(msg, context);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string msg) => UnityEngine.Debug.LogWarning(msg);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string msg, Object context) => UnityEngine.Debug.LogWarning(msg, context);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string msg) => UnityEngine.Debug.LogError(msg);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string msg, Object context) => UnityEngine.Debug.LogError(msg, context);
}
