using System;
using System.Collections.Generic;

namespace UnityEditor.Performance.ProfileAnalyzer
{
    [Serializable]
    internal class FrameSummary
    {
        public int first;
        public int last;
        public int count;                     // Valid frame count may not be last-first

        public double msTotal;
        public float msMean;
        public float msMedian;
        public float msLowerQuartile;
        public float msUpperQuartile;
        public float msMin;
        public float msMax;

        public int msMedianFrameIndex;
        public int msMinFrameIndex;
        public int msMaxFrameIndex;

        public long bytesTotal;
        public long bytesMean; // Not floating point
        public long bytesMedian;
        public long bytesLowerQuartile;
        public long bytesUpperQuartile;
        public long bytesMin;
        public long bytesMax;

        public int bytesMedianFrameIndex;
        public int bytesMinFrameIndex;
        public int bytesMaxFrameIndex;


        public int maxMarkerDepth;
        public int totalMarkers;
        public int markerCountMax;            // Largest marker count (over all frames)
        public float markerCountMaxMean;      // Largest marker count mean

        public int[] buckets = new int[20];   // Each bucket contains 'number of frames' for frametime in that range
        public List<FrameTime> frames = new List<FrameTime>();

        public FrameSummary()
        {
            msMin = float.MaxValue;
            bytesMin = long.MaxValue;

            for (int b = 0; b < buckets.Length; b++)
                buckets[b] = 0;
        }
    }
}
