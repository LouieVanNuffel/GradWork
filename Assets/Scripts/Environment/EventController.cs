using System.Collections.Generic;
using UnityEngine;

public class EventController : MonoBehaviour
{
    [SerializeField] private Event[] _events;

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
