using System;
using System.Collections;
using UnityEngine;

public enum Intensity
{
    Low, Medium, High
}

public enum EventType
{
    Light, Sound, Apparition, Darkness
}

public struct EventInfo
{
    public Vector3 position;
    public Vector3 rangeSize;
}

public class Event : MonoBehaviour
{
    [SerializeField] private EventType _eventType;
    public EventType SetEventType { get { return _eventType; } }
    private EventInfo _eventInfo;

    private bool _wasTriggeredInPastSecond = false;
    private Intensity _lastIntensity;
    private BoxCollider _collider;

    public EventInfo Info { get { return _eventInfo; } }

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        _eventInfo = new EventInfo();
        _eventInfo.position = transform.position;
        _eventInfo.rangeSize = _collider.size;
    }

    public bool TriggerEvent(Intensity intensity, SimulatedPlayer player)
    {
        bool playerInRange;
        if (!IsPlayerInRange(player)) playerInRange = false;
        else playerInRange = true;

        if (playerInRange) player.ApplyEvent(intensity);
        _lastIntensity = intensity;
        StopAllCoroutines();
        StartCoroutine(DrawSphere());
        Debug.Log($"Event: {gameObject.name} was triggered with intensity {intensity}");
        return playerInRange;
    }

    private bool IsPlayerInRange(SimulatedPlayer player)
    {
        Vector3 worldCenter = _collider.transform.TransformPoint(_collider.center);
        Vector3 worldHalfExtents = _collider.transform.TransformVector(_collider.size * 0.5f); // only necessary when collider is scaled by non-uniform transform
        Collider[] colliders = Physics.OverlapBox(worldCenter, worldHalfExtents, _collider.transform.rotation);
        CapsuleCollider playerCollider = player.Collider;

        foreach (Collider collider in colliders)
        {
            if (collider == playerCollider)
            {
                return true;
            }
        }

        return false;
    }

    #region Debug Drawing
    private IEnumerator DrawSphere()
    {
        _wasTriggeredInPastSecond = true;
        yield return new WaitForSeconds(1.0f);
        _wasTriggeredInPastSecond = false;
    }

    private void OnDrawGizmos()
    {
        if (_wasTriggeredInPastSecond)
        {
            Color color = Color.yellow;

            switch (_lastIntensity)
            {
                case Intensity.Low:
                    color = Color.yellow;
                    break;
                case Intensity.Medium:
                    color = Color.orange;
                    break;
                case Intensity.High:
                    color = Color.red;
                    break;
            }

            Gizmos.color = color;
            Gizmos.DrawCube(transform.position + _collider.center, _collider.size);
        }
    }
    #endregion
}
