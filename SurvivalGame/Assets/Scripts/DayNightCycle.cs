using TMPro;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private TextMeshProUGUI _timeText;

    [SerializeField] private float _timeScale = 60f; // Speed of the cycle

    private float _time;
    private float _days;
    private float _hours;
    private float _minutes;

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
        transform.rotation = Quaternion.Euler(new Vector3(sunAngle - 90f, 170f, 0f));
    }
}
