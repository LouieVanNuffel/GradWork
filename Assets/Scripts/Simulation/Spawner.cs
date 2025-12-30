using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject _objectToSpawn;
    [SerializeField] private Vector3 _startPosition = Vector3.zero;
    [SerializeField] private Vector3 _offsetPerSpawn = Vector3.zero;
    [SerializeField] private int _instancesCount = 10;

    private void Awake()
    {
        for (int index = 0; index < _instancesCount; ++index)
        {
            Vector3 position = _startPosition + _offsetPerSpawn * index;
            GameObject spawnedObject = Instantiate(_objectToSpawn, position, Quaternion.identity);
            EvaluationLogger evaluationLogger = spawnedObject.GetComponent<EvaluationLogger>();
            if (evaluationLogger != null) evaluationLogger.AgentIndex = index;
        }
    }
}
