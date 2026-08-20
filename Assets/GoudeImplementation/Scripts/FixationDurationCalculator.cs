using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

public class FixationDurationCalculator : MonoBehaviour
{
    [SerializeField]
    private FixationController fixationController;

    [SerializeField]
    private int runDuration = 2365; // stop condition, measured in frames (same as GazeScoreCalculator)

    [Header("Metrics")]
    [SerializeField]
    private int totalFrames = 0; // total frames elapsed

    [SerializeField]
    private int totalFixationEvents = 0; // number of distinct fixations detected over the run

    private struct FixationRecord
    {
        public int FixationIndex;
        public string ObjectName;
        public Vector3 FixationPoint;
        public double ReportedDuration; // FixationController.currentFixationTime at the moment this fixation started

        public int StartFrame;
        public int EndFrame;
        public float StartTime;
        public float EndTime;

        public int FrameLength => EndFrame - StartFrame + 1;
        public float MeasuredDuration => EndTime - StartTime; // actual elapsed time until the next fixation started
    }

    private List<FixationRecord> fixationRecords;

    // state used to detect when FixationController has produced a new fixation
    private int previousFixationIndex;
    private bool hasOpenRecord;

    void Start()
    {
        fixationRecords = new List<FixationRecord>();
        previousFixationIndex = int.MinValue; // guarantees the first fixation seen is detected as "new"
        hasOpenRecord = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (totalFrames >= runDuration)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
            return; // avoid processing one extra frame after the stop condition fires
        }

        int currentFixationIndex = fixationController.fixationIndex;

        if (currentFixationIndex != previousFixationIndex)
        {
            // close out the previous fixation record, if any
            CloseCurrentRecord(totalFrames - 1, Time.time);

            // start a new fixation record using FixationController's current state
            GameObject fixatedObject = fixationController.currentFixationObject;

            fixationRecords.Add(new FixationRecord
            {
                FixationIndex = currentFixationIndex,
                ObjectName = fixatedObject != null ? fixatedObject.name : "(none)",
                FixationPoint = fixationController.currentFixationPoint,
                ReportedDuration = fixationController.currentFixationTime,
                StartFrame = totalFrames,
                StartTime = Time.time
            });

            hasOpenRecord = true;
            totalFixationEvents++;
            previousFixationIndex = currentFixationIndex;
        }

        totalFrames++;
    }

    // closes the most recently opened fixation record, filling in its end frame/time
    private void CloseCurrentRecord(int endFrame, float endTime)
    {
        if (hasOpenRecord && fixationRecords.Count > 0)
        {
            var last = fixationRecords[fixationRecords.Count - 1];
            last.EndFrame = endFrame;
            last.EndTime = endTime;
            fixationRecords[fixationRecords.Count - 1] = last;
        }
    }

    void OnApplicationQuit()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        // flush whatever fixation was still open when the app quit
        CloseCurrentRecord(totalFrames - 1, Time.time);

        Debug.Log("-------- Fixation Duration Metrics --------");
        Debug.Log("Total simulation frames: " + totalFrames);
        Debug.Log("Total fixation events: " + totalFixationEvents);

        float avgReportedDuration = fixationRecords.Count > 0
            ? (float)fixationRecords.Average(r => r.ReportedDuration)
            : 0f;
        float avgMeasuredDuration = fixationRecords.Count > 0
            ? fixationRecords.Average(r => r.MeasuredDuration)
            : 0f;
        float avgFrameLength = fixationRecords.Count > 0
            ? (float)fixationRecords.Average(r => r.FrameLength)
            : 0f;

        Debug.Log("Average fixation time (reported by model): " + avgReportedDuration + "s");
        Debug.Log("Average fixation time (measured, actual elapsed): " + avgMeasuredDuration + "s (" + avgFrameLength + " frames)");

        StringBuilder fixationCsv = new StringBuilder();
        fixationCsv.AppendLine("FixationIndex,ObjectName,PointX,PointY,PointZ,StartFrame,EndFrame,ReportedDuration,MeasuredDuration");

        foreach (var record in fixationRecords)
        {
            Debug.Log(string.Format(
                "  fixation #{0} on {1} @ ({2:F2},{3:F2},{4:F2}) — frames [{5}-{6}], reported {7:F3}s, measured {8:F3}s",
                record.FixationIndex, record.ObjectName,
                record.FixationPoint.x, record.FixationPoint.y, record.FixationPoint.z,
                record.StartFrame, record.EndFrame,
                record.ReportedDuration, record.MeasuredDuration));

            fixationCsv.AppendLine(string.Format(
                "{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                record.FixationIndex, record.ObjectName,
                record.FixationPoint.x, record.FixationPoint.y, record.FixationPoint.z,
                record.StartFrame, record.EndFrame,
                record.ReportedDuration, record.MeasuredDuration));
        }

        string res = "SoftmaxGeo," + totalFixationEvents + "," + avgReportedDuration + "," + avgMeasuredDuration;
        Debug.Log(res);

        Debug.Log("-------- Fixations (CSV) --------");
        Debug.Log(fixationCsv.ToString());
    }
}