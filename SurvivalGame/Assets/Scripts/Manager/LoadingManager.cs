using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public GameObject loadingScreen;
    public Slider loadingBar;
    public TextMeshProUGUI loadingText;
    public Button startButton;

    public void LoadScene(int sceneIndex)
    {
        StartCoroutine(LoadSceneAsync(sceneIndex));
    }

    IEnumerator LoadSceneAsync(int sceneIndex)
    {
        loadingScreen.SetActive(true);
        startButton.gameObject.SetActive(false); // Başlat düğmesini gizle

        loadingBar.value = 0f; // Yükleme çubuğunu sıfırla
        loadingText.text = "0%"; // Yükleme metnini sıfırla

        AsyncOperation loadScene = SceneManager.LoadSceneAsync(sceneIndex);
        loadScene.allowSceneActivation = false; // Sahne yüklenene kadar sahne değişimini engelle


        while (loadingBar.value < 1f)
        {
            loadingBar.value = Mathf.MoveTowards(loadingBar.value, 1f, Time.deltaTime);
            loadingText.text = Mathf.RoundToInt(loadingBar.value * 100f) + "%"; // Yükleme metnini güncelle
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        loadScene.allowSceneActivation = true; // Sahne yüklendiğinde sahne değişimini etkinleştir
    }

}