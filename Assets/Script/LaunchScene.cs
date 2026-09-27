#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor-only utility: makes pressing Play always start from scene index
/// 0 (the main menu) regardless of which scene is currently open, so
/// there's no need to manually switch scenes before testing. Also
/// prompts to save any unsaved changes before entering Play mode.
/// </summary>
[InitializeOnLoadAttribute]
public static class DefaultSceneLoader
{
    static DefaultSceneLoader(){
        EditorApplication.playModeStateChanged += LoadDefaultScene;
    }

    static void LoadDefaultScene(PlayModeStateChange state){
        if (state == PlayModeStateChange.ExitingEditMode) {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ();
        }

        if (state == PlayModeStateChange.EnteredPlayMode) {
            // Assumes the main menu is scene index 0 in Build Settings.
            EditorSceneManager.LoadScene (0);
        }
    }
}
#endif