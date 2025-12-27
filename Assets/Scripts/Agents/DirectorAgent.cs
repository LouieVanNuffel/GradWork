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

        // Reset player simulation
        _player.ResetPlayerState();

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
        sensor.AddObservation(_player.CurrentHeartRate);
        sensor.AddObservation(_player.AverageHeartRate);
        sensor.AddObservation(_player.AverageSpeed);
        sensor.AddObservation(_player.AverageRotationSpeed);
        sensor.AddObservation(_player.transform.position);

        // Event positions (now 4, if more are added, observation vector size needs to be adjusted)
        Vector3[] eventPositions = _eventController.GetEventPositions();
        foreach (Vector3 eventPosition in eventPositions)
        {
            sensor.AddObservation(eventPosition);
        }

        // Events tracking state
        sensor.AddObservation((int)_lastEventType);
        sensor.AddObservation((int)_lastIntensity);
        sensor.AddObservation(_timeSinceLastEvent);

        // Normalized time since beginning of episode (episode is 300 seconds)
        sensor.AddObservation(Time.timeSinceLevelLoad / 300f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int eventType = actions.DiscreteActions[0];  // (Light=1, Sound=2, Apparition=3, Darkness=4, None=5)
        int intensity = actions.DiscreteActions[1];   // (Low=0, Medium=1, High=2)
        bool validInput = true;

        float reward = 0.0f;
        
        // Update time tracker
        _timeSinceLastEvent += Time.deltaTime;

        // Small pusishment if giving values that don't correspond with anything
        if ((eventType < 0 || eventType > 5) 
            || (intensity < 0 || intensity > 2))
        {
            reward -= 0.1f;
            validInput = false;
        }

        // Execute event
        if (validInput && eventType != 5) // if valid input and not event type none
        {
            _eventController.TriggerEvent((EventType)eventType, (Intensity)intensity);

            _lastEventType = (EventType)eventType;
            _lastIntensity = (Intensity)intensity;
            _timeSinceLastEvent = 0f;

            // Small penalty to avoid spam
            reward -= 0.01f;
        }

        // Reward on heart rate stability
        float hr = _player.CurrentHeartRate;

        if (hr >= _targetHRMin && hr <= _targetHRMax)
        {
            reward += 0.1f; // Reward for staying within range
        }
        else if (hr > _panicThreshold)
        {
            reward -= 1f; // Big penalty (simulate quitting from fear)
            EndEpisode();
        }
        else
        {
            reward -= 0.05f; // Mild penalty for deviating
        }

        // Give reward to agent
        AddReward(reward);
    }
}
