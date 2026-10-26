namespace FusionContinent.SpineBake
{
    /// <summary>
    /// 局内 Spine 动画 CPU 采样累计（主线程单线程写入）。
    /// </summary>
    public static class SpineMeshBakePerfStats
    {
        public static double BakedMeshUpdateMs;
        public static int BakedMeshUpdateCalls;

        public static double UploadVerticesMs;
        public static int UploadVerticesCalls;

        public static double UploadColorsMs;
        public static int UploadColorsCalls;

        public static double TopologyMs;
        public static int TopologyCalls;

        public static double BoundsMs;
        public static int BoundsCalls;

        public static double TintColorsMs;
        public static int TintColorsCalls;

        public static double SpineRuntimeMs;
        public static int SpineRuntimeCalls;

        public static void RecordBakedMeshUpdate(double milliseconds)
        {
            BakedMeshUpdateMs += milliseconds;
            BakedMeshUpdateCalls++;
        }

        public static void RecordUploadVertices(double milliseconds)
        {
            UploadVerticesMs += milliseconds;
            UploadVerticesCalls++;
        }

        public static void RecordUploadColors(double milliseconds)
        {
            UploadColorsMs += milliseconds;
            UploadColorsCalls++;
        }

        public static void RecordTopology(double milliseconds)
        {
            TopologyMs += milliseconds;
            TopologyCalls++;
        }

        public static void RecordBounds(double milliseconds)
        {
            BoundsMs += milliseconds;
            BoundsCalls++;
        }

        public static void RecordTintColors(double milliseconds)
        {
            TintColorsMs += milliseconds;
            TintColorsCalls++;
        }

        public static void RecordSpineRuntime(double milliseconds)
        {
            SpineRuntimeMs += milliseconds;
            SpineRuntimeCalls++;
        }

        public static double GetAvgPerCall(double totalMs, int calls)
        {
            return calls > 0 ? totalMs / calls : 0;
        }

        public static void ResetInterval()
        {
            BakedMeshUpdateMs = 0;
            BakedMeshUpdateCalls = 0;
            UploadVerticesMs = 0;
            UploadVerticesCalls = 0;
            UploadColorsMs = 0;
            UploadColorsCalls = 0;
            TopologyMs = 0;
            TopologyCalls = 0;
            BoundsMs = 0;
            BoundsCalls = 0;
            TintColorsMs = 0;
            TintColorsCalls = 0;
            SpineRuntimeMs = 0;
            SpineRuntimeCalls = 0;
        }
    }
}
