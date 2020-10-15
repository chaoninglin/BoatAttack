using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BoatAttack.Benchmark;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;

public class BenchmarkWindow : EditorWindow
{
    [MenuItem("Tools/Benchmark")]
    static void Init()
    {
        var window = (BenchmarkWindow)GetWindow(typeof(BenchmarkWindow));
        window.Show();
    }

    class Styles
    {
        public static readonly GUIContent[] toolbarOptions = {new GUIContent("Tools"), new GUIContent("Results"), };
    }

    private static string assetGuidKey = "boatattack.benchmark.assetguid";
    private static string assetGUID;
    private static BenchmarkData benchData;

    public int currentToolbar;
    private const int ToolbarWidth = 150;
    private List<PerfResults> PerfResults = new List<PerfResults>();
    private string[] resultFiles;
    private int currentResult;
    private int currentRun = 0;

    // TempUI vars
    private bool resultInfoHeader;
    private bool resultDataHeader;

    private void OnEnable()
    {
        assetGUID = EditorPrefs.GetString(assetGuidKey);
        benchData = AssetDatabase.LoadAssetAtPath<BenchmarkData>(AssetDatabase.GUIDToAssetPath(assetGUID));
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);

        var toolbarRect = EditorGUILayout.GetControlRect();
        toolbarRect.position += new Vector2((toolbarRect.width - ToolbarWidth) * 0.5f, 0f);
        toolbarRect.width = ToolbarWidth;

        currentToolbar = GUI.Toolbar(toolbarRect, currentToolbar,
            Styles.toolbarOptions);

        switch (currentToolbar)
        {
            case 0:
                DrawTools();
                break;
            case 1:
                DrawResults();
                break;
        }
    }

    private void DrawTools()
    {
        GUILayout.Label("Tools Page");
        EditorGUI.BeginChangeCheck();
        benchData = (BenchmarkData) EditorGUILayout.ObjectField(new GUIContent("Benchmark Data File"), benchData,
            typeof(BenchmarkData), false);
        if(EditorGUI.EndChangeCheck())
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(benchData, out assetGUID, out long _);
            EditorPrefs.SetString(assetGuidKey, assetGUID);
        }
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Build to current platform");
        if (GUILayout.Button($"Build {EditorUserBuildSettings.activeBuildTarget.ToString()}"))
        {
            BuildBenchmark();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Build static scene");
        if (GUILayout.Button($"Build Static scene"))
        {
            BuildStaticBenchmark();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void BuildBenchmark()
    {
        var buildOptions = new BuildPlayerOptions();

        var sceneList = new List<string> {"Assets/scenes/menu_benchmark.unity"};
        sceneList.AddRange(benchData.benchmarks.Select(benchSettings => benchSettings.scene));
        buildOptions.scenes = sceneList.ToArray();

        Build(ref buildOptions);
    }

    private void BuildStaticBenchmark()
    {
        var buildOptions = new BuildPlayerOptions();
        var sceneList = new List<string> {"Assets/scenes/testing/benchmark_island-static.unity"};
        buildOptions.scenes = sceneList.ToArray();
        Build(ref buildOptions);
    }

    private void Build(ref BuildPlayerOptions options)
    {
        var curTarget = EditorUserBuildSettings.activeBuildTarget;
        options.locationPathName = $"Builds/Benchmark/{curTarget:G}/BoatattackBenchmark";
        options.target = curTarget;
        options.options = BuildOptions.Development;

        AutoBuildAddressables.Popup();
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        switch (summary.result)
        {
            case BuildResult.Succeeded:
                Debug.Log("Benchmark Build Complete");
                break;
            case BuildResult.Failed:
                Debug.LogError("Benchmark Build Failed");
                break;
        }
    }

    private void DrawResults()
    {
        if (PerfResults == null || PerfResults.Count == 0)
        {
            UpdateFiles();
        }

        if (PerfResults != null && PerfResults.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            currentResult = EditorGUILayout.Popup(new GUIContent("File"), currentResult, resultFiles);
            if (GUILayout.Button("reload", GUILayout.Width(100)))
            {
                UpdateFiles();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            DrawPerfInfo(PerfResults[currentResult].perfStats[0].info);

            DrawPerf(PerfResults[currentResult].perfStats[0]);
        }
        else
        {
            GUILayout.Label("No Stats found, please run a benchmark.");
        }
    }

    private void DrawPerfInfo(TestInfo info)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        resultInfoHeader = EditorGUILayout.BeginFoldoutHeaderGroup(resultInfoHeader, "Info");
        if (resultInfoHeader)
        {
            var fields = info.GetType().GetFields();
            var half = fields.Length / 2;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            for (var index = 0; index < fields.Length; index++)
            {
                var prop = fields[index];
                EditorGUILayout.LabelField(prop.Name, prop.GetValue(info).ToString(), EditorStyles.boldLabel);
                if (index == half)
                {
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndVertical();
    }

    private void DrawPerf(PerfBasic data)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var options = new string[data.RunData.Length + 1];
        options[0] = "Smooth all runs";
        for (int i = 1; i < data.RunData.Length + 1; i++)
        {
            options[i] = $"Run {i.ToString()}";
        }

        resultDataHeader = EditorGUILayout.BeginFoldoutHeaderGroup(resultDataHeader, "Data");
        if (resultDataHeader)
        {
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            {
                var lw = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 50f;
                currentRun = EditorGUILayout.Popup("Display", currentRun, options);
                EditorGUILayout.LabelField("Average:", $"{data.AvgMs:F2}ms", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Min:", $"{data.MinMs:F2}ms at frame {data.MinMSFrame}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Max:", $"{data.MaxMs:F2}ms at frame {data.MaxMSFrame}", EditorStyles.boldLabel);
                EditorGUIUtility.labelWidth = lw;
            }
            EditorGUILayout.EndHorizontal();

            var graphRect = EditorGUILayout.GetControlRect(false, 500f);
            DrawGraph(graphRect, data.RunData, 0, data.AvgMs * 2f);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndVertical();
    }

    #region GraphDrawing

    private void DrawGraph(Rect rect, FrameTimes[] values, float minMS, float maxMS)
    {
        var padding = 20f;
        rect.max -= Vector2.one * padding;
        rect.xMax -= 40f;
        rect.min += Vector2.one * padding;


        //draw value markers
        GUI.backgroundColor = new Color(0f, 0f, 0f, 1f);
        GUI.Box(rect, "");
        //GUI.DrawTexture(rect, Texture2D.grayTexture, ScaleMode.StretchToFill);

        DrawGraphMarkers(rect, minMS, maxMS, 5);

        var c = new Color(0.129f, 0.588f, 0.952f, 1.0f);
        if (currentRun == 0)
        {
            var averageValue = new float[values[0].rawSamples.Length];
            foreach (var frames in values)
            {
                for (int i = 0; i < averageValue.Length; i++)
                {
                    averageValue[i] += frames.rawSamples[i] / values.Length;
                }
            }
            DrawGraphLine(rect, averageValue, minMS, maxMS, c);
        }
        else
        {
            DrawGraphLine(rect, values[currentRun-1].rawSamples, minMS, maxMS, c);
        }
    }

    void DrawGraphLine(Rect rect, float[] points, float min, float max, Color color)
    {
        var graphPoints = new Vector3[points.Length];
        for (var j = 0; j < points.Length; j++)
        {
            var valA = rect.yMax - rect.height * GetGraphLerpValue(points[j], min, max);

            var xLerp = new Vector2(j, j + 1) / points.Length;
            var xA = Mathf.Lerp(rect.xMin, rect.xMax, xLerp.x);
            var posA = new Vector2(xA, valA);
            graphPoints[j] = posA;
        }
        Handles.color = color;
        Handles.DrawAAPolyLine(graphPoints);
    }

    private void DrawGraphMarkers(Rect rect, float min, float max, int count)
    {
        count--;
        for (int i = 0; i <= count; i++)
        {
            var y = Mathf.Lerp(rect.yMax, rect.yMin, (float)i / count);
            Handles.color = new Color(1f, 1f, 1f, 0.5f);
            Handles.DrawDottedLine(new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), 4);
            y -= EditorGUIUtility.singleLineHeight * 0.5f;
            var val = Mathf.Lerp(min, max, (float) i / count);
            GUI.Label(new Rect(new Vector2(rect.xMax, y), new Vector2(80, EditorGUIUtility.singleLineHeight)), $"{val:F1}ms");
        }
    }

    private float GetGraphLerpValue(float ms)
    {
        return GetGraphLerpValue(ms, 0f, 33.33f);
    }

    private float GetGraphLerpValue(float ms, float msMin, float msMax)
    {
        var msA = ms;
        return Mathf.InverseLerp(msMin, msMax, msA);
    }

    #endregion

    #region Utilitiess

    private void UpdateFiles()
    {
        PerfResults = Benchmark.LoadAllBenchmarkStats();
        resultFiles = new string[PerfResults.Count];
        for (var index = 0; index < PerfResults.Count; index++)
        {
            resultFiles[index] = PerfResults[index].fileName;
        }
    }

    #endregion
}
