using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

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

    // State tracking
    private float _timeSinceLastEvent = 0.0f;
    private EventType _lastEventType;
    private Intensity _lastIntensity;

    // Components
    [SerializeField] private SimulatedPlayer _player;
    [SerializeField] private EventController _eventController;

    public override void OnEpisodeBegin()
    {
        StopAllCoroutines();

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

        StartCoroutine(DecisionLoop());
        StartCoroutine(EpisodeTimer());
    }

    private IEnumerator DecisionLoop()
    {
        while (true)
        {
            _player.UpdateDecisionMetrics(1.0f);
            RequestDecision();
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
        int eventType = actions.DiscreteActions[0];  // (Light=1, Sound=2, Apparition=3, Darkness=4, None=5)
        int intensity = actions.DiscreteActions[1];   // (Low=0, Medium=1, High=2)
        bool validInput = true;

        float reward = 0.0f;
        
        // Update time tracker
        _timeSinceLastEvent += Time.deltaTime;

        reward += CalculateHeartRateReward();

        // Small punishment if giving values that don't correspond with anything
        if ((eventType < 0 || eventType > 5)
            || (intensity < 0 || intensity > 2))
        {
            reward -= 0.1f;
            validInput = false;
        }

        // Execute event
        if (validInput && eventType != 5) // if valid input and not event type none
        {
            if (_eventController.TriggerEvent((EventType)eventType, (Intensity)intensity, _player)) reward += 0.05f; // Small reward if player is within range

            _lastEventType = (EventType)eventType;
            _lastIntensity = (Intensity)intensity;
            _timeSinceLastEvent = 0f;

            reward -= 0.02f; // Small cost for every event to prevent spamming
        }

        // If asked to do nothing
        if (eventType == 5)
        {
            // Reward waiting if heart rate is already good
            if (_player.CurrentHeartRate >= _randomizedHRMin &&
                _player.CurrentHeartRate <= _randomizedHRMax)
            {
                reward += 0.02f;
            }
        }

        // Give reward to agent
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
