using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SimulatedPlayer : MonoBehaviour
{
    [SerializeField] private Transform _startTransform;
    [SerializeField] private bool _randomizeValues = false;

    [Header("Heart Rate")]
    [SerializeField] [Range(60.0f, 80.0f)] private float _baselineHeartRate = 75.0f;
    [SerializeField] [Range(0.5f, 1.5f)] private float _sensitivity = 1.0f;
    [SerializeField] [Range(12.0f, 24.0f)] private float _recoveryRateBpm = 18.0f; // BPM per minute
    [SerializeField] private float _noiseLevel = 1.0f;

    [Header("Movement")]
    [SerializeField] [Range(2.0f, 8.0f)] private float _speed = 5.0f;
    [SerializeField] [Range(60.0f, 180.0f)] private float _angularSpeed = 120.0f;
    [SerializeField] [Range(4.0f, 12.0f)] private float _acceleration = 8.0f;
    [SerializeField] private Transform _groundTransform;
    [SerializeField] private Bounds _movementBounds = new Bounds();
    private Vector3 _currentTargetPosition;

    private float _currentHeartRate = 0.0f;
    private float _averageSpeed = 0.0f;
    private Vector3 _lastPosition;

    private Rigidbody _rigidbody;
    private CapsuleCollider _collider;
    private NavMeshAgent _navMeshAgent;

    private void Update()
    {
        UpdateHeartRate();
    }

    private void FixedUpdate()
    {
        UpdateMovement();
    }

    #region Getters
    public float CurrentHeartRate { get { return _currentHeartRate; } }
    public float AverageSpeed { get { return _averageSpeed; } }
    public CapsuleCollider Collider { get { return _collider; } }
    #endregion

    #region Awake
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        SetNewTargetPosition();
        ResetPlayerState();
    }
    #endregion

    #region logic
    public void ApplyEvent(Intensity intensity)
    {
        // Apply Event
        float increase = 0.0f;
        switch (intensity)
        {
            case Intensity.Low:
                increase = 10.0f;
                break;
            case Intensity.Medium:
                increase = 15.0f;
                break;
            case Intensity.High:
                increase = 20.0f;
                break;
            default:
                increase = 10.0f;
                break;
        }

        _currentHeartRate += increase * _sensitivity;
    }

    private void UpdateHeartRate()
    {
        // Calculate the recovery amount in beats per frame
        float recoveryPerSecond = _recoveryRateBpm / 60.0f;
        float difference = _baselineHeartRate - _currentHeartRate;

        // Move towards baseline at a fixed rate
        if (Mathf.Abs(difference) > recoveryPerSecond * Time.deltaTime)
        {
            _currentHeartRate += Mathf.Sign(difference) * recoveryPerSecond * Time.deltaTime;
        }
        else
        {
            _currentHeartRate = _baselineHeartRate; // Snap if very close
        }

        // Noise
        float noise = UnityEngine.Random.Range(-_noiseLevel, _noiseLevel);
        _currentHeartRate += noise * Time.deltaTime;

        // Hard clamp within realistic range
        _currentHeartRate = Mathf.Clamp(_currentHeartRate, 40.0f, 220.0f);
    }

    public void UpdateDecisionMetrics(float decisionInterval)
    {
        float distance = Vector3.Distance(_lastPosition, transform.position);
        _averageSpeed = distance / decisionInterval;

        _lastPosition = transform.position;
    }

    private void UpdateMovement()
    {
        if ((_currentTargetPosition - transform.position).sqrMagnitude < 2.0f * 2.0f) SetNewTargetPosition();
    }

    private void SetNewTargetPosition()
    {
        float randomX = _groundTransform.position.x + _movementBounds.center.x + Random.Range(-_movementBounds.extents.x, _movementBounds.extents.x);
        float randomY = _groundTransform.position.y + _movementBounds.center.y + Random.Range(-_movementBounds.extents.y, _movementBounds.extents.y);
        float randomZ = _groundTransform.position.z + _movementBounds.center.z + Random.Range(-_movementBounds.extents.z, _movementBounds.extents.z);
        _currentTargetPosition = new Vector3(randomX, randomY, randomZ);
        _navMeshAgent.SetDestination(_currentTargetPosition);

    }

    public void ResetPlayerState()
    {
        // transform
        transform.position = _startTransform.position;
        transform.rotation = _startTransform.rotation;

        // randomize values if on
        if (_randomizeValues)
        {
            _baselineHeartRate = Random.Range(60.0f, 80.0f);
            _sensitivity = Random.Range(0.5f, 1.5f);
            _recoveryRateBpm = Random.Range(12.0f, 24.0f);
            _speed = Random.Range(2.0f, 8.0f);
            _angularSpeed = Random.Range(60.0f, 180.0f);
            _acceleration = Random.Range(4.0f, 12.0f);
        }

        // observation values
        _currentHeartRate = _baselineHeartRate;
        _averageSpeed = 0.0f;
        _lastPosition = transform.position;

        // apply movement values to navmeshagent
        _navMeshAgent.speed = _speed;
        _navMeshAgent.angularSpeed = _angularSpeed;
        _navMeshAgent.acceleration = _acceleration;
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_currentTargetPosition, 2.0f);
    }
}
