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

public class Event : MonoBehaviour
{
    [SerializeField] private EventType _eventType;
    public EventType SetEventType { get { return _eventType; } }

    public event Action<Intensity, Vector3> OnEventHappened;

    bool _wasTriggeredInPastSecond = false;
    Intensity _lastIntensity;

    public void TriggerEvent(Intensity intensity)
    {
        Debug.Log($"Event: {gameObject.name} was triggered with intensity {intensity}");
        OnEventHappened?.Invoke(intensity, transform.position);
        _lastIntensity = intensity;
        StopAllCoroutines();
        StartCoroutine(DrawSphere());
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
            Gizmos.DrawSphere(transform.position, 2.0f);
        }
    }
    #endregion
}
