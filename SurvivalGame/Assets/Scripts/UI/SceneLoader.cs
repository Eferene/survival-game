using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    private Input playerInputActions;

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI loadingInfoText;

    bool isLoading = false;

    private void OnEnable()
    {
        // Ensure the loading screen is hidden at the start
        loadingScreen.SetActive(false);
        mainMenu.SetActive(true);
        loadingInfoText.gameObject.SetActive(false);
        progressBar.value = 0f;
        loadingText.text = "0%";

        playerInputActions = new Input();
        playerInputActions.UI.Enable();
        playerInputActions.UI.LoadScene.performed += ctx => OnLoadSceneInput();
    }

    private void OnDisable()
    {
        playerInputActions.UI.Disable();
        playerInputActions.UI.LoadScene.performed -= ctx => OnLoadSceneInput();
    }

    private void OnLoadSceneInput()
    {
        if (loadingScreen.activeSelf)
            isLoading = true;
    }

    public void OnClicked()
    {
        // Show the loading screen
        loadingScreen.SetActive(true);

        // Hide the main menu
        mainMenu.SetActive(false);

        StartCoroutine(LoadSceneAsync("GameScene")); // Replace "GameScene" with your actual scene name
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        // Start loading the scene asynchronously
        var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // Prevent the scene from activating immediately

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            progressBar.value = progress;
            loadingText.text = $"{progress * 100f:0}%";

            // Scene yükleme bittiğinde (progress 0.9'a ulaştığında), 
            // izin verip çık
            if (asyncLoad.progress >= 0.9f)
            {
                loadingInfoText.gameObject.SetActive(true);
                if (isLoading)
                {
                    isLoading = false; // Reset loading state
                    asyncLoad.allowSceneActivation = true;
                }
            }

            yield return null; // Her frame bekle
        }
    }
}
