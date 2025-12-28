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
    [SerializeField] private Vector3 _minColliderSize;
    [SerializeField] private Vector3 _maxColliderSize;
    private BoxCollider _collider;
    private EventInfo _eventInfo;

    private bool _wasTriggeredInPastSecond = false;
    private Intensity _lastIntensity;

    public EventType SetEventType { get { return _eventType; } }
    public EventInfo Info { get { return _eventInfo; } }

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        _eventInfo = new EventInfo();

        // Set random collider size
        float randomX = Random.Range(_minColliderSize.x, _maxColliderSize.x);
        float randomY = Random.Range(_minColliderSize.y, _maxColliderSize.y);
        float randomZ = Random.Range(_minColliderSize.z, _maxColliderSize.z);
        _collider.size = new Vector3(randomX, randomY, randomZ);

        // Initialize event info
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
