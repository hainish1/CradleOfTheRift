using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [SerializeField]
    public string sceneToLoad;
    void Awake()
    {
        if (Application.isPlaying)
        {
            try
            {
                // Check if the scene is already loaded or not. This prevents double loading scenes. Hopefully.
                if (SceneManager.GetSceneByName(sceneToLoad).IsValid())
                {
                    print("Scene is already loaded! Doing nothing.");
                }
                else
                {
                    print($"Scene {sceneToLoad} is loading!");
                    SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{e}. Failed to load in player! If in doubt, ask Marshall!");
            }
            print("Running!");
        }
    }
}
