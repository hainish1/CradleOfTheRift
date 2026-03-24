// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah.
//   </para>
// </summary>

using UnityEditor;

[CustomEditor(typeof(TutorialObject))]
public class TutorialObjectEditor : Editor
{
    /// <summary>
    ///   <para>
    ///     Shows the current state of the GUILayout whenever a TutorialObject property is modified in the inspector.
    ///   </para>
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // _objectType
        SerializedProperty objectTypeProperty = serializedObject.FindProperty("_objectType");
        EditorGUILayout.PropertyField(objectTypeProperty);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("EventName")); // EventName

        if ((TutorialObjectType) objectTypeProperty.enumValueIndex == TutorialObjectType.Touchable)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_singleActivation")); // _singleActivation

            // _allowedTargets
            SerializedProperty allowedTargetsProperty = serializedObject.FindProperty("_allowedTargets");
            EditorGUILayout.PropertyField(allowedTargetsProperty);

            if ((TutorialTargetExclusionType) allowedTargetsProperty.enumValueIndex == TutorialTargetExclusionType.Exclusive)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_triggerTargets")); // _triggerTargets
        }

        serializedObject.ApplyModifiedProperties();
    }
}
