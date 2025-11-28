using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [SerializeField]
    public string sceneToLoad;
    void Awake()
    {
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
    }
}
