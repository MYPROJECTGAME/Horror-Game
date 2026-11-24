using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {

    
    public bool isStartScene;
    [HideInInspector]
    public string sceneName;
    [Tooltip("Name of Main Menu scene")]
    public string mainMenuSceneName;
    public Image loadingBar;

    private void Awake()
    {
        if (!isStartScene)
        {
            sceneName = PlayerPrefs.GetString("GameLevel", "MainMenu");
        }
        else
        {
            sceneName = string.IsNullOrEmpty(mainMenuSceneName) ? "MainMenu" : mainMenuSceneName;
        }
    }

    private void Start()
    {
        StartCoroutine(SceneLoad());       
    }

    IEnumerator SceneLoad()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = "MainMenu";
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (loadingBar != null)
            {
                loadingBar.fillAmount = progress;
            }
            yield return null;
        }
    }

}
