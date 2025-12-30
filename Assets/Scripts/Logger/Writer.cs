using System.IO;
using System.Text;
using UnityEngine;

public class Writer : MonoBehaviour
{
    [SerializeField] private string outputFileName = "director_evaluation_agent";
    private StreamWriter _writer;

    private void Awake()
    {
        outputFileName += $"{gameObject.GetInstanceID()}" + ".csv";
        string path = Path.Combine(Application.persistentDataPath, outputFileName);
        Debug.Log($"EvaluationLogger logging to path: {path}");
        bool fileExists = File.Exists(path);

        _writer = new StreamWriter(path, true, Encoding.UTF8);

        if (!fileExists)
        {
            _writer.WriteLine(
                "AgentIndex," +
                "Episode," +
                "TimeInRangeRatio," +
                "MeanDeviation," +
                "TotalEvents," +
                "EventsPerMinute," +
                "PanicCount," +
                "MaxHR," +
                "MeanRecoveryTime," +
                "OverreactionCount"
            );
            _writer.Flush();
        }
    }

    public void WriteLine(string line)
    {
        _writer?.WriteLine(line);
        _writer?.Flush();
    }

    void OnDestroy()
    {
        _writer?.Close();
    }
}
