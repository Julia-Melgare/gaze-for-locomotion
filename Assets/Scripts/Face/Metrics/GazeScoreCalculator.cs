using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

public class GazeScoreCalculator : MonoBehaviour
{
    [SerializeField]
    private AttentionController attentionController;

    [SerializeField]
    private FixationController fixationController;

    [SerializeField]
    private int runDuration = 2365;

    [SerializeField]
    private List<GameObject> relevantObjects;

    [Header("Metrics")]
    [SerializeField]
    private int totalFixations = 0; // total frames elapsed

    [SerializeField]
    private Dictionary<string, int> fixationsPerObject; // total frames spent on each object (relevant or not), keyed by name

    private HashSet<string> relevantNames; // names of objects considered "relevant" for the gaze score

    // ---- interval tracking ----
    private struct FixationInterval
    {
        public int StartFrame;
        public int EndFrame;
        public float StartTime;
        public float EndTime;

        public int FrameLength => EndFrame - StartFrame + 1;
        public float Duration => EndTime - StartTime;
    }

    private Dictionary<string, List<FixationInterval>> fixationIntervals; // keyed by name, same as fixationsPerObject

    // state used to detect when a fixation starts/ends
    private string currentTargetName;
    private int currentStartFrame;
    private float currentStartTime;

    // ---- switch tracking ----
    private int totalSwitches = 0; // any change of fixation target, including quick 1-frame switches
    private float simulationStartTime;

    void Start()
    {
        relevantNames = new HashSet<string>(relevantObjects.Select(o => o.name));

        fixationsPerObject = new Dictionary<string, int>();
        fixationIntervals = new Dictionary<string, List<FixationInterval>>();

        // pre-seed with relevant objects so they always appear, even with zero fixations
        foreach (var name in relevantNames)
        {
            fixationsPerObject[name] = 0;
            fixationIntervals[name] = new List<FixationInterval>();
        }

        currentTargetName = null;
        currentStartFrame = -1;
        currentStartTime = -1f;
        simulationStartTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (totalFixations >= runDuration)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
            return; // avoid processing one extra frame after the stop condition fires
        }

        GameObject currentFixationTarget;

        if (attentionController != null)
        {
            currentFixationTarget = attentionController.GetCurrentFocus().gameObject;
        }
        else
        {
            currentFixationTarget = fixationController.currentFixationObject;
        }

        string currentName = currentFixationTarget.name;

        // register this object the first time it's seen, whether or not it's "relevant"
        if (!fixationsPerObject.ContainsKey(currentName))
        {
            fixationsPerObject[currentName] = 0;
            fixationIntervals[currentName] = new List<FixationInterval>();
        }
        fixationsPerObject[currentName]++;

        // ---- interval + switch bookkeeping ----
        if (currentName != currentTargetName)
        {
            // count every change of target as a switch, except the very first frame
            // (where currentTargetName starts as null and there's nothing to switch from)
            if (currentTargetName != null)
            {
                totalSwitches++;
            }

            // close out the previous interval (now always tracked, not just relevant ones)
            CloseCurrentInterval(totalFixations - 1, Time.time);

            // start a new interval on the new target
            currentTargetName = currentName;
            currentStartFrame = totalFixations;
            currentStartTime = Time.time;
        }

        totalFixations++;
    }

    // closes the interval that was open on currentTargetName, if any
    private void CloseCurrentInterval(int endFrame, float endTime)
    {
        if (currentTargetName != null && fixationIntervals.ContainsKey(currentTargetName) && currentStartFrame != -1)
        {
            fixationIntervals[currentTargetName].Add(new FixationInterval
            {
                StartFrame = currentStartFrame,
                EndFrame = endFrame,
                StartTime = currentStartTime,
                EndTime = endTime
            });
        }
    }

    void OnApplicationQuit()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        // flush whatever interval was still open when the app quit
        CloseCurrentInterval(totalFixations - 1, Time.time);

        float totalSimTime = Time.time - simulationStartTime;

        string res = "SoftmaxGeo,";
        Debug.Log("-------- Gaze Score Metrics --------");
        Debug.Log("Total simulation frames: " + totalFixations);

        int totalRelevantFixations = 0;
        StringBuilder intervalCsv = new StringBuilder();

        // collected across every object (relevant or not) for the overall average
        var allIntervals = new List<FixationInterval>();

        foreach (var obj in fixationsPerObject)
        {
            bool isRelevant = relevantNames.Contains(obj.Key);
            Debug.Log((isRelevant ? "[relevant] " : "[other] ") + "Frames focusing on " + obj.Key + ": " + obj.Value + "(" + ((float)obj.Value / totalFixations) + ")");

            if (isRelevant)
            {
                totalRelevantFixations += obj.Value;
                res += (float)obj.Value / totalFixations + ",";
            }

            var intervals = fixationIntervals[obj.Key];
            allIntervals.AddRange(intervals);

            Debug.Log("  " + intervals.Count + " fixation interval(s) on " + obj.Key + ":");

            // build "start-end;start-end;..." for this object
            string intervalsStr = string.Join(";", intervals.Select(i => i.StartFrame + "-" + i.EndFrame));

            foreach (var interval in intervals)
            {
                Debug.Log(string.Format(
                    "    frames [{0}-{1}] ({2} frames), time [{3:F3}s-{4:F3}s] ({5:F3}s)",
                    interval.StartFrame, interval.EndFrame, interval.FrameLength,
                    interval.StartTime, interval.EndTime, interval.Duration));
            }

            // one line per object, always quoted (even if empty or single interval)
            intervalCsv.AppendLine(string.Format("{0},\"{1}\"", obj.Key, intervalsStr));
        }

        float overallGazeScore = (float)totalRelevantFixations / totalFixations;
        Debug.Log("Overall Gaze Score: " + overallGazeScore);
        res += overallGazeScore + ",";

        // ---- average fixation duration (across ALL objects, relevant or not) ----
        float avgFixationFrames = allIntervals.Count > 0 ? (float)allIntervals.Average(i => i.FrameLength) : 0f;
        float avgFixationTime = allIntervals.Count > 0 ? allIntervals.Average(i => i.Duration) : 0f;

        Debug.Log("Total target switches: " + totalSwitches);
        Debug.Log("Average fixation duration (any object): " + avgFixationFrames + " frames (" + avgFixationTime + "s)");

        res += avgFixationFrames + "," + avgFixationTime + "," + totalSwitches;
        Debug.Log(res);

        Debug.Log("-------- Fixation Intervals (CSV) --------");
        Debug.Log(intervalCsv.ToString());
    }
}