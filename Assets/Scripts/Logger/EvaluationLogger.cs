using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class EvaluationLogger : MonoBehaviour
{
    [Header("Evaluation Settings")]
    [SerializeField] private bool _setTimeScale = false;
    [SerializeField] private float _timeScale = 1.0f;

    private float _panicThreshold = 140.0f;
    private int _agentIndex = 0;
    public int AgentIndex { set { _agentIndex = value; } }
    public float PanicTreshold { set { _panicThreshold = value; } }

    // Episode accumulators
    private float _episodeTime;
    private float _timeInRange;
    private float _totalDeviation;
    private int _stepCount;

    private int _totalEvents;
    private int _panicCount;
    private float _maxHR;

    private float _timeOutsideRange;
    private float _recoveryTimer;
    private List<float> _recoveryTimes = new();

    private int _overreactionCount;
    private float _lastHR;
    private bool _lastStepWasHighIntensity;

    private Dictionary<EventType, int> _eventTypeCounts = new();

    // Logging
    [SerializeField] private Writer _writer = null;
    private CultureInfo ci = CultureInfo.InvariantCulture;

    void Awake()
    {
        if (_setTimeScale) Time.timeScale = _timeScale;
        if (_writer == null) _writer = FindAnyObjectByType<Writer>();
    }

    public void BeginEpisode()
    {
        _episodeTime = 0.0f;
        _timeInRange = 0.0f;
        _totalDeviation = 0.0f;
        _stepCount = 0;

        _totalEvents = 0;
        _panicCount = 0;
        _maxHR = 0.0f;

        _timeOutsideRange = 0.0f;
        _recoveryTimer = 0.0f;
        _recoveryTimes.Clear();

        _overreactionCount = 0;
        _lastStepWasHighIntensity = false;

        _eventTypeCounts.Clear();
    }

    public void LogStep(
        float currentHR,
        float targetMin,
        float targetMax,
        bool eventTriggered,
        EventType eventType,
        Intensity eventIntensity,
        float deltaTime
    )
    {
        ++_stepCount;
        _episodeTime += deltaTime;

        _maxHR = Mathf.Max(_maxHR, currentHR);

        float targetCenter = (targetMin + targetMax) * 0.5f;
        _totalDeviation += Mathf.Abs(currentHR - targetCenter);

        if (currentHR >= targetMin && currentHR <= targetMax)
        {
            _timeInRange += deltaTime;

            if (_recoveryTimer > 0.0f)
            {
                _recoveryTimes.Add(_recoveryTimer);
                _recoveryTimer = 0.0f;
            }
        }
        else
        {
            _timeOutsideRange += deltaTime;
            _recoveryTimer += deltaTime;
        }

        if (currentHR > _panicThreshold) ++_panicCount;

        if (eventTriggered)
        {
            ++_totalEvents;

            if (!_eventTypeCounts.ContainsKey(eventType)) _eventTypeCounts[eventType] = 0;
            ++_eventTypeCounts[eventType];

            bool highIntensity = (eventIntensity == Intensity.High);

            if (_lastStepWasHighIntensity && highIntensity && currentHR > _lastHR + 2.0f) ++_overreactionCount;

            _lastStepWasHighIntensity = highIntensity;
        }
        else _lastStepWasHighIntensity = false;

        _lastHR = currentHR;
    }

    public void EndEpisode(int episodeIndex)
    {
        float timeInRangeRatio = _episodeTime > 0.0f? _timeInRange / _episodeTime : 0.0f;
        float meanDeviation = _stepCount > 0? _totalDeviation / _stepCount : 0.0f;
        float eventsPerMinute = _episodeTime > 0.0f? _totalEvents / (_episodeTime / 60.0f) : 0.0f;

        float meanRecoveryTime = 0.0f;
        if (_recoveryTimes.Count > 0)
        {
            float sum = 0.0f;
            foreach (var t in _recoveryTimes) sum += t;
            meanRecoveryTime = sum / _recoveryTimes.Count;
        }

        if (_writer != null)
        {
            _writer.WriteLine(
                $"{_agentIndex}," +
                $"{episodeIndex}," +
                $"{timeInRangeRatio.ToString(ci)}," +
                $"{meanDeviation.ToString(ci)}," +
                $"{_totalEvents}," +
                $"{eventsPerMinute.ToString(ci)}," +
                $"{_panicCount}," +
                $"{_maxHR.ToString(ci)}," +
                $"{meanRecoveryTime.ToString(ci)}," +
                $"{_overreactionCount}"
            );
        }
    }
}
