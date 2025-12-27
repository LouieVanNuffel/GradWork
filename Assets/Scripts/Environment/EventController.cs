using System.Collections.Generic;
using UnityEngine;

public class EventController : MonoBehaviour
{
    [SerializeField] private Event[] _events;

    public void TriggerEvent(EventType eventType, Intensity intensity)
    {
        // Triggers first event that it finds that matches the event type
        foreach (Event evt in _events)
        {
            if (evt.SetEventType == eventType)
            {
                evt.TriggerEvent(intensity);
                return;
            }
        }

        Debug.LogWarning($"Tried to trigger event with EventType: {eventType} with intensity: {intensity}, " +
            $"but no event matching the event type was found");
    }

    public Vector3[] GetEventPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (Event evt in _events)
        {
            positions.Add(evt.transform.position);
        }

        return positions.ToArray();
    }
}
