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
	public bool autoMode = false;

	private List<float> samples = new List<float>();// in microseconds
    private int totalSamples = 250;

    // UI display
    private Text frametimeDisplay;
    private string debugInfo;

    private void Start()
    {
	    if (autoMode)
	    {
		    Stats = new PerfBasic("test", 1000) {RunIndex = Benchmark.currentRunNumber};
		    CreateTextGui();
	    }
    }

    public void StartRun(string benchmarkName, int runLength)
    {
	    Stats = new PerfBasic(benchmarkName, runLength) {RunIndex = Benchmark.currentRunNumber};
	    if(frametimeDisplay == null)
			CreateTextGui();
    }

    private void Update ()
    {
	    if (!frametimeDisplay) return;

	    if (autoMode)
	    {
		    Benchmark.currentRunFrames++;
		    if (Benchmark.currentRunFrames > 1000)
			    Benchmark.currentRunFrames = 0;
	    }

		frametimeDisplay.text = "";
        // sample frametime
        samples.Insert(0, Time.deltaTime * 1000f); // add sample at the start
		if(samples.Count >= totalSamples)
		{
            samples.RemoveAt(totalSamples - 1);
        }
        UpdateFrametime();

        var totalMem = Profiler.GetTotalAllocatedMemoryLong();
        var gpuMem = Profiler.GetAllocatedMemoryForGraphicsDriver();

        DrawText(((float)totalMem / 1000000).ToString("#0.00"), ((float)gpuMem / 1000000).ToString("#0.00"));

        Stats.RunTime += Time.unscaledDeltaTime;
    }

    private void DrawText(string memory, string gpuMemory)
	{
		var i = Stats.info;
		debugInfo = $"<b>Unity:</b>{i.UnityVersion}   " +
		            $"<b>URP:</b>{i.UrpVersion}   " +
		            $"<b>Build:</b>{i.BoatAttackVersion}   " +
		            $"<b>Scene:</b>{i.Scene}   " +
		            $"<b>Quality:</b>{i.Quality}\n" +
		            //////////////////////////////////////////////////
		            $"<b>DeviceInfo:</b>{i.Platform}   " +
		            $"{i.API}   " +
		            $"{i.Os.Replace(" ", "")}\n" +
		            //////////////////////////////////////////////////
		            $"<b>CPU:</b>{i.CPU}   " +
		            $"<b>GPU:</b>{i.GPU}   " +
		            $"<b>Resolution:</b>{i.Resolution}\n" +
		            //////////////////////////////////////////////////
		            $"<b>CurrentFrame:</b>{Benchmark.currentRunFrames}   " +
		            $"<b>Mem:</b>{memory}mb   " +
		            $"<b>GPUMem:</b>{gpuMemory}mb\n" +
		            //////////////////////////////////////////////////
		            $"<b>AvgFrametime:</b>{Stats.AvgMs:#0.00}ms   " +
		            $"<b>MinFrametime:</b>{Stats.MinMs:#0.00}ms(frame {Stats.MinMSFrame})   " +
		            $"<b>MaxFrametime:</b>{Stats.MaxMs:#0.00}ms(frame {Stats.MaxMSFrame})";
		frametimeDisplay.text = $"<size=50>{Application.productName} Benchmark - {i.BenchmarkName}</size>\n{debugInfo}";
	}

	public void EndRun()
	{
		var runNumber = Benchmark.currentRunNumber == -1 ? "Warmup" : (Benchmark.currentRunNumber + 1).ToString();
		Debug.Log($"<b>{Stats.info.BenchmarkName} Run {runNumber}: TotalRuntime:{Stats.RunTime:#0.00}s</b>\n{debugInfo}");
		if(Benchmark.currentRunNumber >= 0)
			Stats.Run(Benchmark.currentRunNumber,samples.ToArray());
		samples.Clear();
	}

	public PerfBasic EndBench()
	{
		frametimeDisplay.text = "<size=50>Benchmark Ended</size>";
		return Stats != null ? Stats : null;
	}

	private void UpdateFrametime()
	{
		Stats.AvgMs = 0f;
        var sampleDivision = 1f / samples.Count;

        foreach (var t in samples)
        {
	        Stats.AvgMs += t * sampleDivision;
        }

        if (Benchmark.runNumber < 0) return;

        if (Stats.MinMs > samples[0])
        {
	        Stats.MinMs = samples[0];
	        Stats.MinMSFrame = Benchmark.currentRunFrames;
        }

        if (Benchmark.currentRunFrames > 20 && Stats.MaxMs < samples[0])
        {
	        Stats.MaxMs = samples[0];
	        Stats.MaxMSFrame = Benchmark.currentRunFrames;
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
}
