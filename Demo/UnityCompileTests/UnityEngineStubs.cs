using System;
using System.Collections;

namespace UnityEngine
{
    public class Object
    {
        public static T FindObjectOfType<T>() where T : Object { return null; }
    }
    public class Component : Object { }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour
    {
        protected Coroutine StartCoroutine(IEnumerator routine) { return new Coroutine(); }
        protected void StopAllCoroutines() { }
    }
    public class ScriptableObject : Object { }
    public class Coroutine { }
    public class GameObject : Object
    {
        public GameObject(string name) { }
        public T AddComponent<T>() where T : Component, new() { return new T(); }
    }
    public class Texture2D : Object { }
    public class Font : Object { }
    public class WaitForSeconds { public WaitForSeconds(float seconds) { } }
    public static class Resources { public static T Load<T>(string path) where T : Object { return null; } }
    public static class Debug { public static void Log(object message) { } public static void LogError(object message) { } }
    public static class Application { public static string persistentDataPath { get { return "."; } } }
    public static class Mathf { public static int FloorToInt(float value) { return (int)Math.Floor(value); } }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }
    public struct Rect
    {
        public Rect(float x, float y, float width, float height) { }
    }
    public struct Color { public static Color white { get { return new Color(); } } }
    public enum ScaleMode { StretchToFill, ScaleToFit }
    public enum TextAnchor { MiddleCenter }
    public enum FontStyle { Normal, Bold }
    public enum EventType { MouseDown }
    public sealed class Event
    {
        public static Event current { get { return new Event(); } }
        public EventType type; public int button; public Vector2 mousePosition;
    }
    public sealed class GUIStyleState { public Color textColor; }
    public sealed class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public int fontSize; public FontStyle fontStyle; public TextAnchor alignment; public Font font;
        public GUIStyleState normal { get; } = new GUIStyleState();
    }
    public sealed class GUISkin { public GUIStyle label { get; } = new GUIStyle(); }
    public sealed class GUIContent { public static GUIContent none { get; } = new GUIContent(); }
    public static class GUI
    {
        public static GUISkin skin { get; } = new GUISkin();
        public static void DrawTexture(Rect rect, Texture2D texture, ScaleMode mode) { }
        public static void Label(Rect rect, string text) { }
        public static void Label(Rect rect, string text, GUIStyle style) { }
        public static bool Button(Rect rect, string text) { return false; }
        public static void Box(Rect rect, GUIContent content) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float value) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute { public string menuName; public string fileName; }
    public enum RuntimeInitializeLoadType { AfterSceneLoad }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }
}
