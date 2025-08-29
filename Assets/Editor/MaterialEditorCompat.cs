// Assets/Editor/MaterialEditorCompat.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

public static class MaterialEditorCompat
{
#if !UNITY_2021_2_OR_NEWER
    // Unity 2021.2 미만에서 MaterialEditor.PopupShaderProperty가 없을 때 사용하는 대체 구현
    public static int PopupShaderProperty(this MaterialEditor materialEditor,
                                          MaterialProperty prop,
                                          GUIContent label,
                                          string[] options)
    {
        if (prop == null) return 0;
        if (options == null) options = Array.Empty<string>();

        int current = Mathf.Clamp(Mathf.RoundToInt(prop.floatValue), 0, Mathf.Max(0, options.Length - 1));
        string lbl = (label != null) ? label.text : (prop != null ? prop.displayName : "Option");

        EditorGUI.BeginChangeCheck();
        int next = EditorGUILayout.Popup(lbl, current, options);
        if (EditorGUI.EndChangeCheck())
        {
            materialEditor.RegisterPropertyChangeUndo(lbl);
            prop.floatValue = next;
        }
        return next;
    }

    public static int PopupShaderProperty(this MaterialEditor materialEditor,
                                          MaterialProperty prop,
                                          string label,
                                          string[] options)
    {
        return PopupShaderProperty(materialEditor, prop, new GUIContent(label), options);
    }
#endif
}
#endif
