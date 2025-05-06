using UnityEngine;
using UnityEditor;


//[CustomPropertyDrawer(typeof(SubtitleLine))]
public class SubtitleLineDrawer : PropertyDrawer
{
    //cant get this stolen code to work
    /*
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Space for: main row + preview line + spacing
        return EditorGUIUtility.singleLineHeight * 2.5f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property == null)
        {
            EditorGUI.LabelField(position, "Property is null");
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 4f;

        Rect labelRect = new Rect(position.x, position.y, position.width, lineHeight);
        Rect idRect = new Rect(position.x, position.y + lineHeight + spacing, position.width * 0.4f, lineHeight);
        Rect toggleRect = new Rect(position.x + position.width * 0.42f, position.y + lineHeight + spacing, 20f, lineHeight);
        Rect durRect = new Rect(position.x + position.width * 0.46f, position.y + lineHeight + spacing, position.width * 0.2f, lineHeight);
        Rect previewRect = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);

        // Get properties
        var idProp = property.FindPropertyRelative("subtitleID");
        var durToggleProp = property.FindPropertyRelative("overrideDurationEnabled");
        var durProp = property.FindPropertyRelative("overrideDuration");

        EditorGUI.LabelField(labelRect, label);

        // Editable fields
        EditorGUI.PropertyField(idRect, idProp, GUIContent.none);
        durToggleProp.boolValue = EditorGUI.Toggle(toggleRect, durToggleProp.boolValue);
        if (durToggleProp.boolValue)
        {
            EditorGUI.PropertyField(durRect, durProp, GUIContent.none);
        }
        else
        {
            durProp.floatValue = -1f; // Sentinel for "use TSV value"
        }

        // Preview subtitle text if available
        string previewText = "<ID not found>";
        if (!string.IsNullOrWhiteSpace(idProp.stringValue) && SubtitleManager.Instance != null)
        {
            var data = SubtitleManager.Instance.GetSubtitleData(idProp.stringValue);
            if (data != null)
            {
                previewText = data.subtitleText;
            }
        }

        GUIStyle previewStyle = EditorStyles.helpBox;
        previewStyle.wordWrap = true;
        EditorGUI.LabelField(previewRect, previewText, previewStyle);

        EditorGUI.EndProperty();
    }
    */
}