using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class DayNightCycle : MonoBehaviour
{
    [Header("Light Objects")]
    [SerializeField] private GameObject _sun;
    [SerializeField] private GameObject _moon;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private TextMeshProUGUI _timeText;

    [Header("Time Settings")]
    [SerializeField] private float _timeScale = 60f; // Geçiş hızı

    [Header("Skybox Settings")]
    [SerializeField] private Material _skyboxMaterial;
    [Range(0f, 24f)][SerializeField] private float _dawnTime = 6f; // Şafak vakti
    [Range(0f, 24f)][SerializeField] private float _duskTime = 18f; // Alacakaranlık
    [SerializeField] private float _transitionDuration = 2f; // Geçişin kaç saat süreceği

    private float _time;
    private float _days;
    private float _hours;
    private float _minutes;

    private void Start()
    {
        RenderSettings.skybox = _skyboxMaterial;
    }

    private void Update()
    {
        _time += Time.deltaTime * _timeScale;

        _minutes = (int)(_time % 60f);
        _hours = (int)((_time / 60f) % 24f);
        _days = (int)(_time / 1440f);

        _timeText.text = $"{((int)_hours).ToString("D2")}:{((int)_minutes).ToString("D2")}";
        _dayText.text = $"Day {_days}";

        float dayProgress = (_time % 1440f) / 1440f; // Progress through the day (0 to 1)
        float sunAngle = dayProgress * 360f;
        _sun.transform.rotation = Quaternion.Euler(new Vector3(sunAngle - 90f, -30f, 0f));
        _moon.transform.rotation = Quaternion.Euler(new Vector3(sunAngle + 90f, -30f, 0f));

        if (_hours >= 6 && _hours < 18)
        {
            _sun.SetActive(true);
            _moon.SetActive(false);
        }
        else
        {
            _sun.SetActive(false);
            _moon.SetActive(true);
        }

        UpdateSkybox();
    }

    private void UpdateSkybox()
    {
        #region Mathf.InverseLerp nedir?
        // Mathf.InverseLerp, bir değerin belirli bir aralık içindeki yüzdesini hesaplamak için kullanılan bir Unity fonksiyonudur.
        // Örneğin, Mathf.InverseLerp(10, 20, 15) ifadesi 0.5 (50%) döner çünkü 15, 10 ile 20 arasının tam ortasındadır.
        #endregion

        float currentTime = _hours + (_minutes / 60f);
        float blendValue = 0f;

        // Şafak vakti geçişi
        if (currentTime >= _dawnTime && currentTime < _dawnTime + _transitionDuration)
        {
            blendValue = 1f - Mathf.InverseLerp(_dawnTime, _dawnTime + _transitionDuration, currentTime);
        }
        // Alacakaranlık geçişi
        else if (currentTime >= _duskTime && currentTime < _duskTime + _transitionDuration)
        {
            blendValue = Mathf.InverseLerp(_duskTime, _duskTime + _transitionDuration, currentTime);
        }
        // Gündüz
        else if (currentTime >= _dawnTime + _transitionDuration && currentTime < _duskTime)
        {
            blendValue = 0f;
        }
        // Gece
        else
        {
            blendValue = 1f;
        }

        _skyboxMaterial.SetFloat("_CubemapTransition", blendValue);
    }
}
