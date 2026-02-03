using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    SerializedProperty useMultipleStats, statMods, effects;
    SerializedProperty statType, operatorType, value, duration;
    SerializedProperty itemId;

    void OnEnable()
    {
        useMultipleStats = serializedObject.FindProperty("useMultipleStats");
        statMods = serializedObject.FindProperty("statMods");
        effects = serializedObject.FindProperty("effects");
        statType = serializedObject.FindProperty("statType");
        operatorType = serializedObject.FindProperty("operatorType");
        value = serializedObject.FindProperty("value");
        duration = serializedObject.FindProperty("duration");
        itemId = serializedObject.FindProperty("itemId");
    }

    public override void OnInspectorGUI()
    {
        // If properties fail to bind
        if (useMultipleStats == null || statMods == null || effects == null || statType == null || operatorType == null || value == null || duration == null || itemId == null)
        {
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "useMultipleStats",
            "statMods",
            "effects",
            "statType",
            "operatorType",
            "value",
            "duration",
            "itemId"
        );

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(useMultipleStats);
        if (useMultipleStats.boolValue)
        {
            EditorGUILayout.PropertyField(statMods, true);
        }
        else
        {
            EditorGUILayout.PropertyField(statType);
            EditorGUILayout.PropertyField(operatorType);
            EditorGUILayout.PropertyField(value);
            EditorGUILayout.PropertyField(duration);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(effects, true); // uses EffectSpecDrawer

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(itemId);

        serializedObject.ApplyModifiedProperties();
    }

}
