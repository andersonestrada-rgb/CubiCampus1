#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class TutorialEditorMenu
{
    [MenuItem("Tools/CubiCampus/Mostrar Tutorial en Escena")]
    static void CrearTutorial()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        TutorialDia1 existente = Object.FindAnyObjectByType<TutorialDia1>();
        if (existente != null)
        {
            existente.canvas = canvas;
            existente.cam = Camera.main;
            Selection.activeGameObject = existente.gameObject;
            EditorGUIUtility.PingObject(existente.gameObject);
            return;
        }

        GameObject go = new GameObject("TutorialDia1");
        go.hideFlags = HideFlags.None;
        TutorialDia1 tut = go.AddComponent<TutorialDia1>();
        tut.canvas = canvas;
        tut.cam = Camera.main;
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("TutorialDia1 creado. Ajusta posicionDialogo/posicionNPC en el Inspector.");
    }
}
#endif
