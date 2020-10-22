using System;
using System.Collections.Generic;
using BoatAttack;
using BoatAttack.Benchmark;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class PerfomanceStats : MonoBehaviour
{
	// Frame time stats
	private PerfBasic Stats;
	private float frametime;
	private float runtime;

	// Demo stuff
	public bool autoMode = false;
	private const int demoFrames = 1000;

	// TempData
	private float averageFrametime;
	private List<float> avgSamples = new List<float>(60);// data for average value - in milliseconds
	private FrameData minFrame = FrameData.DefaultMin;
	private FrameData maxFrame = FrameData.DefaultMax;

	// UI display
    private Text frametimeDisplay;
    private string debugInfo;

    // FrameDisplay
    public LineRenderer line;

    private void Start()
    {
	    if (autoMode)
	    {
		    Stats = new PerfBasic("AutoTest", demoFrames);
		    CreateTextGui();
	    }
    }

    public void StartRun(string benchmarkName, int runLength)
    {
	    Stats = new PerfBasic(benchmarkName, runLength);
	    if(frametimeDisplay == null)
			CreateTextGui();
    }

    private void Update ()
    {
	    if (!frametimeDisplay) return;


	    // Timing
	    frametime = Time.unscaledDeltaTime * 1000f;
	    runtime += Time.unscaledDeltaTime;

	    avgSamples.Insert(0, frametime);
	    if(avgSamples.Count > 60)
		    avgSamples.RemoveAt(60);
	    UpdateFrametime();

	    //Displaying
	    var totalMem = Profiler.GetTotalAllocatedMemoryLong();
	    var mem = ((float) totalMem / 1000000).ToString("#0.00");
	    var gpuMem = Profiler.GetAllocatedMemoryForGraphicsDriver();
	    var gpu = ((float) gpuMem / 1000000).ToString("#0.00");
	    DrawText(mem, gpu);

	    //Saving Data
	    if (Benchmark.currentRunIndex >= 0 && Benchmark.currentRunIndex < Stats.RunData.Length)
	    {
		    var runData = Stats.RunData?[Benchmark.currentRunIndex];
		    if(runData == null)
			    Stats.RunData[0] = new RunData(new float[Benchmark.current.runLength]);

		    runData.rawSamples[Benchmark.currentRunFrame] = frametime; // add sample
	    }

	    // Auto mode
        if (autoMode)
        {
	        Benchmark.currentRunFrame++;
	        if (Benchmark.currentRunFrame >= demoFrames)
		        Benchmark.currentRunFrame = 0;
        }
    }

    private void DrawText(string memory, string gpuMemory)
	{
		frametimeDisplay.text = "";
		var info = Stats.info;
		debugInfo = $"<b>Unity:</b>{info.UnityVersion}   " +
		            $"<b>URP:</b>{info.UrpVersion}   " +
		            $"<b>Build:</b>{info.BoatAttackVersion}   " +
		            $"<b>Scene:</b>{info.Scene}   " +
		            $"<b>Quality:</b>{info.Quality}\n" +
		            //////////////////////////////////////////////////
		            $"<b>DeviceInfo:</b>{info.Platform}   " +
		            $"{info.API}   " +
		            $"{info.Os.Replace(" ", "")}\n" +
		            //////////////////////////////////////////////////
		            $"<b>CPU:</b>{info.CPU}   " +
		            $"<b>GPU:</b>{info.GPU}   " +
		            $"<b>Resolution:</b>{info.Resolution}\n" +
		            //////////////////////////////////////////////////
		            $"<b>CurrentFrame:</b>{Benchmark.currentRunFrame}   " +
		            $"<b>Mem:</b>{memory}mb   " +
		            $"<b>GPUMem:</b>{gpuMemory}mb\n" +
		            //////////////////////////////////////////////////
		            $"<b>Frametimes Average:</b>{averageFrametime:#0.00}ms   " +
		            $"<b>Min(Fastest):</b>{minFrame.ms:#0.00}ms(@frame {minFrame.frameIndex})   " +
		            $"<b>Max(Slowest):</b>{maxFrame.ms:#0.00}ms(@frame {maxFrame.frameIndex})";
		frametimeDisplay.text = $"<size=50>{Application.productName} Benchmark - {info.BenchmarkName}</size>\n{debugInfo}";
	}

	public void EndRun()
	{
		var runNumber = Benchmark.currentRunIndex == -1 ? "Warmup" : (Benchmark.currentRunIndex + 1).ToString();
		Debug.Log($"<b>{Stats.info.BenchmarkName} " +
		          $"Run {runNumber}:" +
		          $"TotalRuntime:{runtime:#0.00}s</b>\n{debugInfo}");

		if (Benchmark.currentRunIndex >= 0 && !Benchmark.SimpleRun)
		{
			Stats.RunData[Benchmark.currentRunIndex].EndRun(runtime, minFrame, maxFrame);
		}

		minFrame = FrameData.DefaultMin;
		maxFrame = FrameData.DefaultMax;
		runtime = 0.0f;
	}

	public PerfBasic EndBench()
	{
		frametimeDisplay.text = "<size=50>Benchmark Ended</size>";
		return Stats != null ? Stats : null;
	}

	private void UpdateFrametime()
	{
		averageFrametime = 0.0f;
        var sampleDivision = 1.0f / avgSamples.Count;

        foreach (var t in avgSamples)
        {
	        averageFrametime += t * sampleDivision;
        }

        if (minFrame.ms > frametime)
        {
	        minFrame.Set(Benchmark.currentRunFrame, frametime);
        }

        if (maxFrame.ms < frametime)
        {
	        maxFrame.Set(Benchmark.currentRunFrame, frametime);
        }
	}

	private void CreateTextGui()
	{
		var textGo = new GameObject("perfText", typeof(Text));
		textGo.transform.SetParent(AppSettings.ConsoleCanvas.transform, true);

		frametimeDisplay = textGo.GetComponent<Text>();
		frametimeDisplay.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
		frametimeDisplay.fontSize = 20;
		frametimeDisplay.lineSpacing = 1.2f;
		frametimeDisplay.raycastTarget = false;

		var rectTransform = frametimeDisplay.rectTransform;
		rectTransform.anchorMin = rectTransform.sizeDelta = rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
	}

	/*void SetGraphData()
	{
		line.positionCount = avgSamples.Count;
		var points = new Vector3[avgSamples.Count];
		for (int i = 0; i < points.Length; i++)
		{
			var x = Mathf.Lerp(1f, -1f, (float) i / avgSamples.Count);
			var y = (avgSamples[i] - Stats.Average) * 0.02f;
			points[i] = new Vector3(x, y, 0f);
		}

		line.SetPositions(points);
	}*/
}
