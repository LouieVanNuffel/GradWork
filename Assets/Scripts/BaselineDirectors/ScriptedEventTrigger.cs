using UnityEngine;

public class ScriptedEventTrigger : MonoBehaviour
{
    [SerializeField] private SimulatedPlayer _player;
    [SerializeField] private ScriptedDirector _scriptedDirector;
    [SerializeField] private Event _eventToTrigger;
    [SerializeField] private Intensity _intensity;

    private void OnTriggerEnter(Collider other)
    {
        SimulatedPlayer hitSimulatedPlayer = other.transform.root.GetComponentInChildren<SimulatedPlayer>();
        if (hitSimulatedPlayer == null) return;

        if (hitSimulatedPlayer == _player)
        {
            _scriptedDirector.TriggerEvent(_eventToTrigger, _intensity);
        }
    }
}
