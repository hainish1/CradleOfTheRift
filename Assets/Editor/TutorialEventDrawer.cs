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

[CustomPropertyDrawer(typeof(TutorialEvent))]
public class TutorialEventDrawer : PropertyDrawer
{
    private float _lineHeight = EditorGUIUtility.singleLineHeight;
    private float _spacing = EditorGUIUtility.standardVerticalSpacing;
    private bool _includeChildren = true;

    /// <summary>
    ///   <para>
    ///     Gets the total height of the TutorialEvent property inside the inspector.
    ///   </para>
    /// </summary>
    /// <param name="property"> The TutorialEvent property. </param>
    /// <param name="label"> The property label. </param>
    /// <returns> A float value of the total height. </returns>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int staticProperties = 1; // Properties that are always showing.
        float currHeight = staticProperties * (_lineHeight + _spacing);
        
        // Get height of the destruct group list.
        SerializedProperty destructGroupProperty = property.FindPropertyRelative("DestructGroup");
        float destructGroupListHeight = EditorGUI.GetPropertyHeight(destructGroupProperty, _includeChildren);

        currHeight += destructGroupListHeight;

        // Get height of the tasks list.
        SerializedProperty tasksProperty = property.FindPropertyRelative("Tasks");
        float tasksListHeight = EditorGUI.GetPropertyHeight(tasksProperty, _includeChildren);

        currHeight += tasksListHeight;

        float bottomPadding = _lineHeight / 2;
        return currHeight + bottomPadding;
    }

    /// <summary>
    ///   <para>
    ///     Shows the current state of the GUI whenever a TutorialEvent property is modified in the inspector.
    ///   </para>
    /// </summary>
    /// <param name="position"> Position of the TutorialEvent property. </param>
    /// <param name="property"> The TutorialEvent property. </param>
    /// <param name="label"> The property label. </param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float currY = position.y;

        // EventName
        Rect eventNamePosition = new(position.x, currY, position.width, _lineHeight);
        EditorGUI.PropertyField(eventNamePosition, property.FindPropertyRelative("EventName"));

        currY += _lineHeight + _spacing;

        // DestructGroup
        Rect destructGroupPosition = new(position.x, currY, position.width, _lineHeight);
        SerializedProperty destructGroupProperty = property.FindPropertyRelative("DestructGroup");
        EditorGUI.PropertyField(destructGroupPosition, destructGroupProperty, _includeChildren);
        float destructGroupListHeight = EditorGUI.GetPropertyHeight(destructGroupProperty, _includeChildren);

        currY += destructGroupListHeight + _spacing;

        // Tasks
        Rect tasksPosition = new(position.x, currY, position.width, _lineHeight);
        EditorGUI.PropertyField(tasksPosition, property.FindPropertyRelative("Tasks"), _includeChildren);

        EditorGUI.EndProperty();
    }
}
