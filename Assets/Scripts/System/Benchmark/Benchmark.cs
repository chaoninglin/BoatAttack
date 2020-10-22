using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BoatAttack.Benchmark
{
    public class Benchmark : MonoBehaviour
    {
        // data
        [HideInInspector] public int simpleRunScene = -1;
        public BenchmarkConfigData settings;
        public bool simpleRun = false;
        public static bool SimpleRun;
        private int benchIndex;
        public static BenchmarkData current { get; private set; }

        private static PerfomanceStats _stats;

        // Timing data
        //public static int totalRuns;
        public static int currentRunIndex;
        //public static int totalFrames;
        public static int currentRunFrame;
        private int totalRunFrames;
        private bool running = false;

        // Bench results
        private Dictionary<int, List<PerfBasic>> _perfData = new Dictionary<int, List<PerfBasic>>();
        public static List<PerfResults> PerfResults = new List<PerfResults>();

        //public AssetReference perfStatsUI;
        //public AssetReference perfSummaryUI;

        private void Start()
        {
            if (settings == null) AppSettings.ExitGame("Benchmark Not Setup");

            if(settings.disableVSync)
                QualitySettings.vSyncCount = 0;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _stats = gameObject.AddComponent<PerfomanceStats>();
            DontDestroyOnLoad(gameObject);

            if (simpleRun && settings.benchmarkData?[simpleRunScene] != null)
            {
                SimpleRun = simpleRun;
                current = settings.benchmarkData[simpleRunScene];
                LoadBenchmark();
            }
            else
            {
                current = settings.benchmarkData[benchIndex];
                LoadBenchmark();
            }
        }

        private void OnDestroy()
        {
            RenderPipelineManager.endFrameRendering -= EndFrameRendering;
        }

        private void LoadBenchmark()
        {
            _perfData.Add(benchIndex, new List<PerfBasic>());
            AppSettings.LoadScene(current.scene);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != current.scene) return;

            if (current.warmup)
            {
                currentRunIndex = -1;
            }
            else
            {
                currentRunIndex = 0;
            }

            currentRunFrame = 0;

            switch (current.type)
            {
                case BenchmarkType.Scene:
                    break;
                case BenchmarkType.Shader:
                    break;
                default:
                    AppSettings.ExitGame("Benchmark Not Setup");
                    break;
            }

            _stats.enabled = settings.stats;
            if(settings.stats)
                _stats.StartRun(current.benchmarkName, current.runLength);

            BeginRun();
            RenderPipelineManager.endFrameRendering += EndFrameRendering;
        }

        private void BeginRun()
        {
            currentRunFrame = 0;
        }

        private void EndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            currentRunFrame++;
            if (currentRunFrame < current.runLength) return;
            _stats.EndRun();

            currentRunIndex++;
            if (currentRunIndex < current.runs || simpleRun)
            {
                BeginRun();
            }
            else
            {
                RenderPipelineManager.endFrameRendering -= EndFrameRendering;
                EndBenchmark();
            }
        }

        public void EndBenchmark()
        {
            if(settings.saveData) SaveBenchmarkStats();
            benchIndex++;

            if (benchIndex < settings.benchmarkData.Count)
            {
                current = settings.benchmarkData[benchIndex];
                LoadBenchmark();
            }
            else
            {
                FinishBenchmark();
            }
        }

        private void FinishBenchmark()
        {
            switch (settings.finishAction)
            {
                case FinishAction.Exit:
                    AppSettings.ExitGame();
                    break;
                case FinishAction.ShowStats:
                    break;
                case FinishAction.Nothing:
                    break;
                default:
                    AppSettings.ExitGame("Benchmark Not Setup");
                    break;
            }
        }

        private void SaveBenchmarkStats()
        {
            if (settings.stats)
            {
                var stats = _stats.EndBench();
                if (stats != null)
                {
                    _perfData[benchIndex].Add(stats);
                }
            }

            var path = GetResultPath() + $"/{_perfData[benchIndex][0].info.BenchmarkName}.txt";
            var data = new string[_perfData[benchIndex].Count];

            for (var index = 0; index < _perfData[benchIndex].Count; index++)
            {
                var perfData = _perfData[benchIndex][index];
                data[index] = JsonUtility.ToJson(perfData);
            }
            var results = new PerfResults();
            results.fileName = Path.GetFileName(path);
            results.filePath = Path.GetFullPath(path);
            results.timestamp = DateTime.Now;
            results.perfStats = _perfData[benchIndex].ToArray();
            PerfResults.Add(results);
            File.WriteAllLines(path, data);
        }

        public static string GetResultPath()
        {
            string path;
            if (Application.isEditor)
            {
                path = Directory.GetParent(Application.dataPath).ToString();
            }
            else
            {
                path = Application.persistentDataPath;
            }
            path += "/PerformanceResults";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public static List<PerfResults> LoadAllBenchmarkStats()
        {
            if (PerfResults.Count > 0)
            {
                return PerfResults;
            }
            else
            {
                var list = new List<PerfResults>();
                var fileList = Directory.GetFiles(GetResultPath());

                foreach (var file in fileList)
                {
                    if(!File.Exists(file))
                        break;

                    var result = new PerfResults();
                    var data = File.ReadAllLines(file);

                    if(data.Length == 0)
                        break;

                    //process data
                    result.fileName = Path.GetFileName(file);
                    result.filePath = Path.GetFullPath(file);
                    result.timestamp = File.GetCreationTime(file);
                    var perfData = data.Select(t => (PerfBasic) JsonUtility.FromJson(t, typeof(PerfBasic))).ToArray();
                    result.perfStats = perfData;
                    list.Add(result);
                }

                return list;
            }
        }
    }

#if UNITY_EDITOR
    public class BenchmarkTool
    {
        [MenuItem("Boat Attack/Benchmark/Island Flythrough")]
        public static void IslandFlyThrough()
        {

        }
    }
#endif

    public class PerfResults
    {
        public string fileName;
        public string filePath;
        public DateTime timestamp;
        public PerfBasic[] perfStats;
    }

    public class PerfBasic
    {
        public TestInfo info;
        public int Frames;
        public RunData[] RunData;

        public PerfBasic(string benchmarkName, int frames)
        {
            Frames = frames;
            info = new TestInfo(benchmarkName);
            RunData = new RunData[Benchmark.current.runs];
            for (var index = 0; index < RunData.Length; index++)
            {
                RunData[index] = new RunData(new float[frames]);
            }
        }
    }

    [Serializable]
    public class RunData
    {
        public float RunTime;
        public float AvgMs;
        public FrameData MinFrame = FrameData.DefaultMin;
        public FrameData MaxFrame = FrameData.DefaultMax;
        public float[] rawSamples;

        public RunData(float[] times) { rawSamples = times; }

        public void Average()
        {
            AvgMs = 0.0f;
            foreach (var sample in rawSamples)
            {
                AvgMs += sample / rawSamples.Length;
            }
        }
        public void SetMin(float ms, int frame) { MinFrame.ms = ms; MinFrame.frameIndex = frame; }
        public void SetMax(float ms, int frame) { MaxFrame.ms = ms; MaxFrame.frameIndex = frame; }

        public void EndRun(float runtime, FrameData min, FrameData max)
        {
            RunTime = runtime;
            MinFrame = min;
            MaxFrame = max;
            Average();
        }

    }

    [Serializable]
    public class FrameData
    {
        public int frameIndex;
        public float ms;

        public FrameData(int frameNumber, float frameTime)
        {
            frameIndex = frameNumber;
            ms = frameTime;
        }

        public void Set(int frameNumber, float frameTime)
        {
            frameIndex = frameNumber;
            ms = frameTime;
        }

        public static FrameData DefaultMin => new FrameData(-1, Single.PositiveInfinity);

        public static FrameData DefaultMax => new FrameData(-1, Single.NegativeInfinity);
    }

    [Serializable]
    public class TestInfo
    {
        public string BenchmarkName;
        public string Scene;
        public string UnityVersion;
        public string UrpVersion;
        public string BoatAttackVersion;
        public string Platform;
        public string API;
        public string CPU;
        public string GPU;
        public string Os;
        public string Quality;
        public string Resolution;

        public TestInfo(string benchmarkName)
        {
            BenchmarkName = benchmarkName;
            Scene = Utility.RemoveWhitespace(SceneManager.GetActiveScene().name);
            UnityVersion = Application.unityVersion;
            UrpVersion = "N/A";
            BoatAttackVersion = Application.version;
            Platform =  Utility.RemoveWhitespace(Application.platform.ToString());
            API =  Utility.RemoveWhitespace(SystemInfo.graphicsDeviceType.ToString());
            CPU =  Utility.RemoveWhitespace(SystemInfo.processorType);
            GPU =  Utility.RemoveWhitespace(SystemInfo.graphicsDeviceName);
            Os =  Utility.RemoveWhitespace(SystemInfo.operatingSystem);
            Quality =  Utility.RemoveWhitespace(QualitySettings.names[QualitySettings.GetQualityLevel()]);
            Resolution = $"{Display.main.renderingWidth}x{Display.main.renderingHeight}";
        }
    }
}
