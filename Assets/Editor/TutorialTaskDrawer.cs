// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah.
//   </para>
// </summary>

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TutorialTask))]
public class TutorialTaskDrawer : PropertyDrawer
{
    private float _lineHeight = EditorGUIUtility.singleLineHeight;
    private float _spacing = EditorGUIUtility.standardVerticalSpacing;

    /// <summary>
    ///   <para>
    ///     Gets the total height of the TutorialTask property inside the inspector.
    ///   </para>
    /// </summary>
    /// <param name="property"> The TutorialTask property. </param>
    /// <param name="label"> The property label. </param>
    /// <returns> A float value of the total height. </returns>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int staticProperties = 2; // Properties that are always showing.
        float currHeight = staticProperties * (_lineHeight + _spacing);

        // Get dynamic height of teleport task type.
        TutorialTaskType taskType = (TutorialTaskType) property.FindPropertyRelative("TaskType").enumValueIndex;
        if (taskType == TutorialTaskType.TeleportObject)
        {
            currHeight += _lineHeight + _spacing;

            // Get dynamic height of teleport entry type.
            TutorialTeleportType teleportType = (TutorialTeleportType)property.FindPropertyRelative("TeleportType").enumValueIndex;
            if (teleportType == TutorialTeleportType.Manual)
                currHeight += (_lineHeight + _spacing) * 2;
            else if (teleportType == TutorialTeleportType.Object)
                currHeight += _lineHeight + _spacing;
        }

        float bottomPadding = _lineHeight / 2;
        return currHeight + bottomPadding;
    }

    /// <summary>
    ///   <para>
    ///     Shows the current state of the GUI whenever a TutorialTask property is modified in the inspector.
    ///   </para>
    /// </summary>
    /// <param name="position"> Position of the TutorialTask property. </param>
    /// <param name="property"> The TutorialTask property. </param>
    /// <param name="label"> The property label. </param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        float currY = position.y;

        // TaskType
        Rect taskTypePosition = new(position.x, currY, position.width, _lineHeight);
        SerializedProperty taskTypeProperty = property.FindPropertyRelative("TaskType");
        EditorGUI.PropertyField(taskTypePosition, taskTypeProperty);

        currY += _lineHeight + _spacing;

        // TargetObject
        Rect targetObjectPosition = new(position.x, currY, position.width, _lineHeight);
        EditorGUI.PropertyField(targetObjectPosition, property.FindPropertyRelative("TargetObject"));

        // Show the corresponding properties if teleport task type is selected.
        if ((TutorialTaskType) taskTypeProperty.enumValueIndex == TutorialTaskType.TeleportObject)
            TeleportTaskType(position, currY, property);

        EditorGUI.EndProperty();
    }

    /// <summary>
    ///   <para>
    ///     Shows the current GUI for the teleport task type.
    ///   </para>
    /// </summary>
    /// <param name="position"> Position of the TutorialTask property. </param>
    /// <param name="property"> The TutorialTask property. </param>
    private void TeleportTaskType(Rect position, float currY, SerializedProperty property)
    {
        currY += _lineHeight + _spacing;

        // TeleportType
        Rect teleportTypePosition = new(position.x, currY, position.width, _lineHeight);
        SerializedProperty teleportTypeProperty = property.FindPropertyRelative("TeleportType");
        EditorGUI.PropertyField(teleportTypePosition, teleportTypeProperty);

        currY += _lineHeight + _spacing;

        TutorialTeleportType teleportType = (TutorialTeleportType) teleportTypeProperty.enumValueIndex;
        if (teleportType == TutorialTeleportType.Manual)
        {
            // TargetPosition
            Rect targetManualPosition = new(position.x, currY, position.width, _lineHeight);
            EditorGUI.PropertyField(targetManualPosition, property.FindPropertyRelative("TargetPosition"));

            currY += _lineHeight + _spacing;

            // TargetOrientation
            Rect targetOrientationPosition = new(position.x, currY, position.width, _lineHeight);
            EditorGUI.PropertyField(targetOrientationPosition, property.FindPropertyRelative("TargetOrientation"));
        }
        else if (teleportType == TutorialTeleportType.Object)
        {
            // TargetTransform
            Rect targetTransformPosition = new(position.x, currY, position.width, _lineHeight);
            EditorGUI.PropertyField(targetTransformPosition, property.FindPropertyRelative("TargetTransform"));
        }
    }
}
