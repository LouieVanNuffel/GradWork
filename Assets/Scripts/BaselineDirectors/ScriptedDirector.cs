using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptedDirector : MonoBehaviour
{
    [Header("Target Ranges")]
    [SerializeField] private float _targetHRMin = 68.0f;
    [SerializeField] private float _targetHRMax = 85.0f;
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

    private void OnEnable()
    {
        _player.OnReachedEnd += OnEpisodeBegin;
    }

    private void OnDisable()
    {
        _player.OnReachedEnd -= OnEpisodeBegin;
    }

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

            if (_evaluationLogger != null)
            {

                _evaluationLogger.LogStep(
                _player.CurrentHeartRate,
                _targetHRMin,
                _targetHRMax,
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

    public void TriggerEvent(Event evt, Intensity intensity)
    {
        evt.TriggerEvent(intensity, _player);
        _lastEventType = evt.SetEventType;
        _lastIntensity = intensity;
        _eventTriggeredLastStep = true;
    }

    private IEnumerator EpisodeTimer()
    {
        yield return new WaitForSeconds(_maxEpisodeTimeInSeconds);
        OnEpisodeBegin();
    }
}
