using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [SerializeField]
    public string sceneToLoad;
    void Awake()
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
}
