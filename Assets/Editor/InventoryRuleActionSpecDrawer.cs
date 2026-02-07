using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InventoryRuleActionSpec))]
public class InventoryRuleActionSpecDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position.height = EditorGUIUtility.singleLineHeight;

        var typeProp = property.FindPropertyRelative("type");
        var itemProp = property.FindPropertyRelative("item");
        var amountProp = property.FindPropertyRelative("amount");
        var otherItemProp = property.FindPropertyRelative("otherItem");


        // layout type, item, amount, otheritem
        float gap = 6f;
        float typeWidth = Mathf.Min(160f, position.width * 0.35f);

        var typeRect = new Rect(position.x, position.y, typeWidth, position.height);
        EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

        var actionType = (InventoryRuleActionType)typeProp.enumValueIndex;

       
        float x = typeRect.xMax + gap;
        float remaining = position.xMax - x;

        bool needsItem = actionType is
            InventoryRuleActionType.AddStacks or
            InventoryRuleActionType.RemoveStacks or
            InventoryRuleActionType.RemoveAllStacks or
            InventoryRuleActionType.SetCount or
            InventoryRuleActionType.TransformItem or
            InventoryRuleActionType.UnlockLootItem or
            InventoryRuleActionType.BlockLootItem;

        bool needsAmount = actionType is
            InventoryRuleActionType.AddStacks or
            InventoryRuleActionType.RemoveStacks or
            InventoryRuleActionType.SetCount or
            InventoryRuleActionType.TransformItem;

        bool needsOther = actionType is InventoryRuleActionType.TransformItem;

        
        float amountWidth = needsAmount ? 70f : 0f;
        float otherWidth = needsOther ? Mathf.Min(220f, remaining * 0.35f) : 0f;

        float itemWidth = remaining;
        if (needsAmount) itemWidth -= (amountWidth + gap);
        if (needsOther) itemWidth -= (otherWidth + gap);

        if (needsItem)
        {
            var itemRect = new Rect(x, position.y, Mathf.Max(80f, itemWidth), position.height);
            EditorGUI.PropertyField(itemRect, itemProp, GUIContent.none);
            x = itemRect.xMax + gap;
        }

        // Amount 
        if (needsAmount)
        {
            var amountRect = new Rect(x, position.y, amountWidth, position.height);

            int v = amountProp.intValue;
            v = actionType == InventoryRuleActionType.SetCount ? Mathf.Max(0, v) : Mathf.Max(1, v);

            // Draw as int field, but keep it compact
            v = EditorGUI.IntField(amountRect, v);
            amountProp.intValue = actionType == InventoryRuleActionType.SetCount ? Mathf.Max(0, v) : Mathf.Max(1, v);

            x = amountRect.xMax + gap;
        }

        // Other Item (Transform)
        if (needsOther)
        {
            var otherRect = new Rect(x, position.y, otherWidth, position.height);
            EditorGUI.PropertyField(otherRect, otherItemProp, GUIContent.none);
        }

        EditorGUI.EndProperty();



    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;
}
