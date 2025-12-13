#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Twinny.UI
{
    [CustomEditor(typeof(ButtonActionXR))]
    public class ButtonActionXREditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // atualiza o serializedObject
            serializedObject.Update();

            // botão só aparece em Playmode
            if (Application.isPlaying)
            {
                if (GUILayout.Button("CLICK"))
                {
                    ButtonActionXR buttonActionXR = (ButtonActionXR)target;
                    buttonActionXR.OnRelease();
                }
            }

            // aplica alterações
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif