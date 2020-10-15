using System;
using Cinemachine;
using UnityEngine;

namespace BoatAttack.Benchmark
{
    public class BenchmarkCamera : MonoBehaviour
    {
        public BenchmarkCameraSettings[] cameras;

        private void Awake()
        {
            foreach (var cam in cameras)
            {
                switch (cam.type)
                {
                    case BenchmarkCameraType.Static:
                        break;
                    case BenchmarkCameraType.FlyThrough:
                        cam.Dolly = cam.camera.GetCinemachineComponent<CinemachineTrackedDolly>();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                cam.camera.enabled = true;
            }
        }

        private void LateUpdate()
        {
            foreach (var benchCam in cameras)
            {
                if (benchCam.type == BenchmarkCameraType.FlyThrough)
                {
                    if (!benchCam.Dolly) continue;

                    var frames = Benchmark.runFrames > 0 ? Benchmark.runFrames : 1000;
                    benchCam.Dolly.m_PathPosition += 1f / frames;
                    benchCam.Dolly.m_PathPosition = Mathf.Repeat(benchCam.Dolly.m_PathPosition, 1f);
                }
            }
        }

        [Serializable]
        public class BenchmarkCameraSettings
        {
            public BenchmarkCameraType type;
            public CinemachineVirtualCamera camera;

            // public but not saved
            [NonSerialized] public CinemachineTrackedDolly Dolly;
        }
    }
}