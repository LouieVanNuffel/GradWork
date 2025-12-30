using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditor.Searcher.Searcher.AnalyticsEvent;

public class DirectorAgent : Agent
{
    [Header("Target Ranges")]
    [SerializeField] private float _targetHRMin = 68.0f;
    [SerializeField] private float _targetHRMax = 85.0f;
    [SerializeField] private float _targetHRRandomDeviance = 5.0f;
    private float _randomizedHRMin;
    private float _randomizedHRMax;
    [SerializeField] private float _panicThreshold = 105.0f;

    [Header("Time")]
    [SerializeField] private float _maxEpisodeTimeInSeconds = 60.0f;
    [SerializeField] private float _minimumEventCooldown = 3.0f;

    [Header("Evaluation")]
    [SerializeField] private EvaluationLogger _evaluationLogger = null;
    int _episodeIndex = -1;

    // State tracking
    private float _timeSinceLastEvent = 0.0f;
    private EventType _lastEventType;
    private Intensity _lastIntensity;
    private float _lastHeartRate = 0.0f;
    private bool _eventTriggeredLastStep = false;

    // Components
    [SerializeField] private SimulatedPlayer _player;
    [SerializeField] private EventController _eventController;

    public override void OnEpisodeBegin()
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
        float minRangeOffset = Random.Range(-_targetHRRandomDeviance, _targetHRRandomDeviance);
        float maxRangeOffset = Random.Range(-_targetHRRandomDeviance, _targetHRRandomDeviance);
        _randomizedHRMin = _targetHRMin + minRangeOffset;
        _randomizedHRMax = Mathf.Clamp(_targetHRMax + maxRangeOffset, _targetHRMin + 5.0f, 220.0f); // always keep max > min

        // Reset player simulation
        _player.ResetPlayerState();

        // Re initialize events
        _eventController.InitializeEvents();

        // Clear agent memory
        _timeSinceLastEvent = 0.0f;
        _lastEventType = 0;
        _lastIntensity = 0;
        _lastHeartRate = _player.CurrentHeartRate;

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

    private IEnumerator EpisodeTimer()
    {
        yield return new WaitForSeconds(_maxEpisodeTimeInSeconds);
        EndEpisode();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Player State
        sensor.AddObservation(_player.CurrentHeartRate / 220.0f); // Heartrate is normalized
        sensor.AddObservation(_randomizedHRMin / 220.0f);
        sensor.AddObservation(_randomizedHRMax / 220.0f);
        sensor.AddObservation(_player.AverageSpeed);

        // Event info (now 4, if more are added, observation vector size needs to be adjusted)
        EventInfo[] eventInfos = _eventController.GetEventInfos();
        foreach (EventInfo eventInfo in eventInfos)
        {
            Vector3 toPlayer = _player.transform.position - eventInfo.position;
            sensor.AddObservation(toPlayer);
            sensor.AddObservation(eventInfo.rangeSize);
            sensor.AddObservation((float)eventInfo.type);
        }

        // Events tracking state
        sensor.AddObservation((int)_lastEventType);
        sensor.AddObservation((int)_lastIntensity);
        sensor.AddObservation(_timeSinceLastEvent);

        // Normalized time since beginning of episode
        sensor.AddObservation(Time.timeSinceLevelLoad / _maxEpisodeTimeInSeconds);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int eventType = actions.DiscreteActions[0];   // Light=0, Sound=1, Apparition=2, Darkness=3, None=4
        int intensity = actions.DiscreteActions[1];   // Low=0, Medium=1, High=2

        float reward = 0.0f;

        // Time tracking
        _timeSinceLastEvent += Time.deltaTime;

        // Baseline heart rate reward (every step)
        reward += CalculateHeartRateReward();

        // Validate action bounds
        bool validEventType = eventType >= 0 && eventType <= 4;
        bool validIntensity = intensity >= 0 && intensity <= 2;

        if (!validEventType || !validIntensity)
        {
            reward -= 0.1f;
            AddReward(reward);
            return;
        }

        // eventType is none = wait
        if (eventType == 4)
        {
            if (_player.CurrentHeartRate >= _randomizedHRMin &&
                _player.CurrentHeartRate <= _randomizedHRMax)
            {
                reward += 0.05f; // Waiting while stable is good
                Debug.Log("Director waited while in heartrate range");
            }
            else
            {
                reward += 0.01f; // Small patience reward
                Debug.Log("Director waited while outside heartrate range");
            }

            AddReward(reward);
            return;
        }

        // Event attempted during cooldown
        if (_timeSinceLastEvent < _minimumEventCooldown)
        {
            reward -= 0.2f;
            AddReward(reward);
            return;
        }

        // Execute event
        bool playerInRange = _eventController.TriggerEvent((EventType)eventType, (Intensity)intensity, _player);

        if (playerInRange) reward += 0.05f;

        // Track last event
        _lastEventType = (EventType)eventType;
        _lastIntensity = (Intensity)intensity;
        _timeSinceLastEvent = 0.0f;
        _eventTriggeredLastStep = true;

        // Event cost (anti-spam)
        reward -= 0.15f;

        // Overreaction penalty (HR rising)
        float hrDelta = _player.CurrentHeartRate - _lastHeartRate;
        _lastHeartRate = _player.CurrentHeartRate;

        if (hrDelta > 2.0f)
        {
            reward -= 0.05f;
        }

        // Apply reward
        AddReward(reward);
    }

    #region Reward calculation
    private float CalculateHeartRateReward()
    {
        float hr = _player.CurrentHeartRate;

        // Target center
        float targetCenter = (_randomizedHRMin + _randomizedHRMax) * 0.5f;

        // Max distance for normalization
        float maxDistance = _panicThreshold - targetCenter;

        // Distance from target center
        float distance = Mathf.Abs(hr - targetCenter);
        float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

        float reward = 0.0f;

        if (hr >= _randomizedHRMin && hr <= _randomizedHRMax)
        {
            // Strong reward if inside the target range
            reward = 1.0f;
        }
        else
        {
            // Small shaping reward guiding toward the target
            reward = 0.2f * (1.0f - normalizedDistance);
        }

        // Panic penalty (soft, not instant end)
        if (hr > _panicThreshold)
        {
            reward -= 2.0f;
            if (hr > _panicThreshold + 10.0f) EndEpisode(); // End if drastically over panic threshold
        }

        return reward * 0.05f; // Scale down for PPO stability
    }
    #endregion
}
