using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace BoatAttack.Benchmark
{
    [CreateAssetMenu(fileName = "BenchmarkSettings", menuName = "Boat Attack/System/Benchmark Settings")]
    public class BenchmarkData : ScriptableObject
    {
        public FinishAction finishAction;
        public bool saveData;
        public bool disableVSync = true;
        public List<BenchmarkSettings> benchmarks = new List<BenchmarkSettings>();
    }

    [Serializable]
    public enum BenchmarkType
    {
        Scene,
        Shader
    }

    [Serializable]
    public enum BenchmarkCameraType
    {
        Static,
        FlyThrough
    }

    [Serializable]
    public enum FinishAction
    {
        Exit,
        ShowStats,
        Nothing
    }

    [Serializable]
    public class BenchmarkSettings
    {
        public string benchmarkName;
#if UNITY_EDITOR
        public SceneAsset sceneAsset;
#endif
        public string scene = "benchmark_island-flythrough";
        public BenchmarkType type;
        public int runs = 4;
        public int runLength = 1000;
        public bool warmup;
        public bool stats = false;
    }
}
