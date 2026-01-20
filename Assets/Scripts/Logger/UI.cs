using TMPro;
using UnityEngine;

public class UI : MonoBehaviour
{
    [SerializeField] private SimulatedPlayer _simulatedPlayer;
    [SerializeField] private DirectorAgent _directorAgent;
    [SerializeField] private TextMeshProUGUI _heartRateText;
    [SerializeField] private TextMeshProUGUI _targetRangeText;
    [SerializeField] private TextMeshProUGUI _lastEventText;

    private void Update()
    {
        _heartRateText.text = $"Player Heart Rate: {Mathf.RoundToInt(_simulatedPlayer.CurrentHeartRate)}";
        _targetRangeText.text = $"Target Range: [{Mathf.RoundToInt(_directorAgent._currentTargetRangeMin)}, " +
            $"{Mathf.RoundToInt(_directorAgent._currentTargetRangeMax)}]";
        _lastEventText.text = $"Triggered {_directorAgent.LastEventType} with intensity {_directorAgent.LastIntensity}";
    }
}
