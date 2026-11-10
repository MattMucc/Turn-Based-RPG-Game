using UnityEditor;
using UnityEngine;
using Game.GridPrototype;

// SerializedProperty keeps Undo, prefab overrides and shared-library editing on Unity's normal path.
[CustomPropertyDrawer(typeof(BattleActionOption))]
public sealed class BattleActionOptionDrawer : PropertyDrawer
{
    const float Gap = 2;
    static bool Custom(SerializedProperty p) => p.FindPropertyRelative("Pattern").enumValueIndex >= (int)BattleTargetPattern.CustomSingle;
    static bool Area(SerializedProperty p) => p.FindPropertyRelative("Pattern").enumValueIndex == (int)BattleTargetPattern.CustomArea;
    static int Radius(SerializedProperty p) => Mathf.Max(0, p.FindPropertyRelative(Area(p) ? "Radius" : "Reach").intValue);
    static float GridSize(SerializedProperty p) => Mathf.Min(240, (Radius(p) * 2 + 1) * 26);
    static bool Hidden(SerializedProperty p, SerializedProperty child) => child.name == "DisabledPatternCells" ||
        (child.name == "RotateAreaTowardTarget" && !Area(p));

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight + Gap;
        if (!property.isExpanded) return height;
        var child = property.Copy(); var end = property.GetEndProperty();
        for (bool enter = true; child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end); enter = false)
            if (!Hidden(property, child)) height += EditorGUI.GetPropertyHeight(child, true) + Gap;
        if (Custom(property)) height += GridSize(property) + 84;
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
        if (property.isExpanded)
        {
            row.y += row.height + Gap;
            EditorGUI.indentLevel++;
            var child = property.Copy(); var end = property.GetEndProperty();
            for (bool enter = true; child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end); enter = false)
            {
                if (Hidden(property, child)) continue;
                row.height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(row, child, true); row.y += row.height + Gap;
                if (child.name == "Pattern" && Custom(property)) DrawGrid(ref row, property);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }

    static void DrawGrid(ref Rect row, SerializedProperty property)
    {
        int radius = Radius(property), side = radius * 2 + 1;
        var disabled = property.FindPropertyRelative("DisabledPatternCells");
        row.height = 18;
        EditorGUI.LabelField(row, Area(property) ? "AoE footprint • center = aimed tile" : "Aim pattern • center = acting character"); row.y += 20;
        EditorGUI.LabelField(row, "Blue = on · gray = off · top = +Z / forward"); row.y += 20;
        float size = Mathf.Min(GridSize(property), row.width), tile = size / side;
        for (int y = radius; y >= -radius; y--) for (int x = -radius; x <= radius; x++)
        {
            var cell = new Vector2Int(x, y); int index = IndexOf(disabled, cell);
            var rect = new Rect(row.x + (x + radius) * tile, row.y + (radius - y) * tile, tile, tile);
            EditorGUI.DrawRect(rect, Color.black);
            EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, tile - 2, tile - 2), index < 0 ? new Color(.15f, .55f, 1) : new Color(.32f, .32f, .32f));
            if (x == 0 && y == 0) GUI.Label(rect, "•", EditorStyles.centeredGreyMiniLabel);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                if (index >= 0) disabled.DeleteArrayElementAtIndex(index);
                else { int i = disabled.arraySize++; disabled.GetArrayElementAtIndex(i).vector2IntValue = cell; }
                GUI.changed = true; Event.current.Use();
            }
        }
        row.y += GridSize(property) + 4; row.height = 20;
        if (GUI.Button(new Rect(row.x, row.y, 75, 20), "All on")) { disabled.ClearArray(); GUI.changed = true; }
        if (GUI.Button(new Rect(row.x + 80, row.y, 75, 20), "All off"))
        {
            disabled.ClearArray();
            for (int y = -radius; y <= radius; y++) for (int x = -radius; x <= radius; x++)
            { int i = disabled.arraySize++; disabled.GetArrayElementAtIndex(i).vector2IntValue = new Vector2Int(x, y); }
            GUI.changed = true;
        }
        row.y += 40;
    }

    static int IndexOf(SerializedProperty cells, Vector2Int cell)
    {
        for (int i = 0; i < cells.arraySize; i++) if (cells.GetArrayElementAtIndex(i).vector2IntValue == cell) return i;
        return -1;
    }
}
