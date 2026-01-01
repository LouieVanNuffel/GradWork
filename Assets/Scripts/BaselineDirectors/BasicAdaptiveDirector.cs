using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using static UnityEditor.Searcher.Searcher.AnalyticsEvent;

public class BasicAdaptiveDirector : MonoBehaviour
{
    [Header("Target Ranges")]
    [SerializeField] private float _targetHRMin = 68.0f;
    [SerializeField] private float _targetHRMax = 85.0f;
    [SerializeField] private float _targetHRRandomDeviance = 5.0f;
    [SerializeField] private float _targetHRCenterRandomShift = 10.0f;
    private float _randomizedHRMin;
    private float _randomizedHRMax;
    [SerializeField] private float _panicThreshold = 105.0f;

    [Header("Time")]
    [SerializeField] private float _maxEpisodeTimeInSeconds = 60.0f;

    [Header("Evaluation")]
    [SerializeField] private EvaluationLogger _evaluationLogger = null;
    int _episodeIndex = -1;

    // State tracking
    private EventType _lastEventType;
    private Intensity _lastIntensity;
    private bool _eventTriggeredLastStep = false;

    // Components
    [SerializeField] private SimulatedPlayer _player;
    [SerializeField] private EventController _eventController;

    private void Start()
    {
        OnEpisodeBegin();
    }

    private void OnEpisodeBegin()
    {
        StopAllCoroutines();

        if (_evaluationLogger != null)
        {
            if (_episodeIndex >= 0) _evaluationLogger.EndEpisode(_episodeIndex);
            ++_episodeIndex;
            _evaluationLogger.PanicTreshold = _panicThreshold;
            _evaluationLogger.BeginEpisode();
        }

        // Randomize target heart rate range
        // Random offset min and max
        float minRangeOffset = Random.Range(-_targetHRRandomDeviance, _targetHRRandomDeviance);
        float maxRangeOffset = Random.Range(-_targetHRRandomDeviance, _targetHRRandomDeviance);
        _randomizedHRMin = _targetHRMin + minRangeOffset;
        _randomizedHRMax = Mathf.Clamp(_targetHRMax + maxRangeOffset, _targetHRMin + 5.0f, 220.0f); // always keep max > min

        // Random shift full range
        float targetCenter = (_randomizedHRMin + _randomizedHRMax) * 0.5f;
        float targetRangeSize = _randomizedHRMax - _randomizedHRMin;
        float newTargetCenter = targetCenter + Random.Range(-_targetHRCenterRandomShift, _targetHRCenterRandomShift);
        _randomizedHRMin = newTargetCenter - targetRangeSize * 0.5f;
        _randomizedHRMax = newTargetCenter + targetRangeSize * 0.5f;

        // Reset player simulation
        _player.ResetPlayerState();

        // Re initialize events
        _eventController.InitializeEvents();

        StartCoroutine(DecisionLoop());
        StartCoroutine(EpisodeTimer());
    }

    private IEnumerator DecisionLoop()
    {
        while (true)
        {
            _player.UpdateDecisionMetrics(1.0f);
            RequestDecision();

            if (_evaluationLogger != null)
            {

                _evaluationLogger.LogStep(
                _player.CurrentHeartRate,
                _randomizedHRMin,
                _randomizedHRMax,
                _eventTriggeredLastStep,
                _lastEventType,
                _lastIntensity,
                1.0f
            );
            }

            _eventTriggeredLastStep = false;

            yield return new WaitForSeconds(1.0f);
        }
    }

    private void RequestDecision()
    {
        // If heart rate is too low, trigger medium event if any is within range
        if (_player.CurrentHeartRate < _randomizedHRMin)
        {
            List<Event> events = _eventController.Events;
            foreach (Event evt in events)
            {
                if (evt.IsPlayerInRange(_player))
                {
                    evt.TriggerEvent(Intensity.Medium, _player);
                    _lastEventType = evt.SetEventType;
                    _lastIntensity = Intensity.Medium;
                    _eventTriggeredLastStep = true;
                }
            }
        }
    }

    private IEnumerator EpisodeTimer()
    {
        yield return new WaitForSeconds(_maxEpisodeTimeInSeconds);
        OnEpisodeBegin();
    }
}
