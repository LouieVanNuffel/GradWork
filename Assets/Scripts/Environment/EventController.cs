using System;
using System.Collections.Generic;
using UnityEngine;

public class EventController : MonoBehaviour
{
    [SerializeField] private bool _randomized = true;
    [SerializeField] private GameObject[] _eventPrefabs;
    [SerializeField] private Transform _groundTransform;
    [SerializeField] private Bounds _movementBounds = new Bounds();

    private float _spawnedEventsCount = 4;
    [SerializeField] private List<Event> _events = new List<Event>();
    public List<Event> Events { get { return _events; } }

    #region Initialization
    private void Awake()
    {
        InitializeEvents();
    }

    public void InitializeEvents()
    {
        // If not randomized
        if (!_randomized)
        {
            if (_events.Count < _spawnedEventsCount) Debug.LogWarning($"Events are not randomized but {_events.Count} were assigned, instead of the intended {_spawnedEventsCount}");
            return;
        }

        // Destroy any spawned events
        foreach (Event evt in _events)
        {
            Destroy(evt.gameObject);
        }
        _events.Clear();

        if (_eventPrefabs.Length <= 0)
        {
            Debug.LogError("No Event prefabs were assigned to the event controller");
            return;
        }

        int prefabIndex = 0;

        for (int index = 0; index < _spawnedEventsCount; ++index)
        {
            // Spawn prefab
            Vector3 randomPosition = GetRandomPosition();
            GameObject eventObject = Instantiate(_eventPrefabs[prefabIndex], randomPosition, Quaternion.identity);
            eventObject.transform.parent = transform;

            // Get event component
            Event evt = eventObject.GetComponent<Event>();
            if (evt == null)
            {
                Debug.LogWarning("No Event component found on event prefab");
                return;
            }

            // Add to list and increment prefab index
            _events.Add(evt);
            ++prefabIndex;
            prefabIndex %= _eventPrefabs.Length;
        }
    }

    private Vector3 GetRandomPosition()
    {
        float randomX = _groundTransform.position.x + _movementBounds.center.x + UnityEngine.Random.Range(-_movementBounds.extents.x, _movementBounds.extents.x);
        float randomY = _groundTransform.position.y + _movementBounds.center.y + UnityEngine.Random.Range(-_movementBounds.extents.y, _movementBounds.extents.y);
        float randomZ = _groundTransform.position.z + _movementBounds.center.z + UnityEngine.Random.Range(-_movementBounds.extents.z, _movementBounds.extents.z);
        return new Vector3(randomX, randomY, randomZ);
    }
    #endregion

    #region Events Logic
    public bool TriggerEvent(EventType eventType, Intensity intensity, SimulatedPlayer player)
    {
        Event evt = GetEvent(eventType);
        if (evt != null)
        {
            if (evt.TriggerEvent(intensity, player)) return true;
            return false;
        }

        Debug.LogWarning($"Tried to trigger event with EventType: {eventType} with intensity: {intensity}, " +
            $"but no event matching the event type was found");
        return false;
    }

    private Event GetEvent(EventType eventType)
    {
        // Returns first event that it finds that matches the event type
        foreach (Event evt in _events)
        {
            if (evt.SetEventType == eventType)
            {
                return evt;
            }
        }

        return null;
    }
    #endregion
}
