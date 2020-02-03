
using System;

namespace UnityEditor.Performance.ProfileAnalyzer
{
    /// <summary>
    /// Metrics related to an individual frame
    /// </summary>
    public struct FrameTime : IComparable<FrameTime>
    {
        /// <summary>Duration in the frame in milliseconds</summary>
        public float ms;
        /// <summary>Index of which frame this time duration occured on. A zero based frame index</summary>
        public int frameIndex;
        /// <summary>Number of occurrences</summary>
        public int count;

        /// <summary>Initialise FrameTime</summary>
        /// <param name="index"> The frame index</param>
        /// <param name="msTime"> The duration of the frame in milliseconds</param>
        /// <param name="_count"> The number of occurrences</param>
        public FrameTime(int index, float msTime, int _count)
        {
            frameIndex = index;
            ms = msTime;
            count = _count;
        }

        /// <summary>Initialise from another FrameTime</summary>
        /// <param name="t"> The FrameTime to assign</param>
        public FrameTime(FrameTime t)
        {
            frameIndex = t.frameIndex;
            ms = t.ms;
            count = t.count;
        }

        /// <summary>Compare the time duration between the frames and check which is larger</summary>
        /// <param name="other"> The other FrameTime to compare </param>
        /// <returns>-1 if this is smaller, 0 if the same, 1 if this is larger</returns>
        public int CompareTo(FrameTime other)
        {
            return ms.CompareTo(other.ms);
        }

        /// <summary>Compare the time duration between two FrameTimes and check which is larger</summary>
        /// <param name="a"> The first FrameTime to compare </param>
        /// <param name="b"> The second FrameTime to compare </param>
        /// <returns>-1 if a is smaller, 0 if the same, 1 if a is larger</returns>
        public static int CompareMs(FrameTime a, FrameTime b)
        {
            return a.ms.CompareTo(b.ms);
        }

        /// <summary>Compare the instance count between two FrameTimes and check which is larger</summary>
        /// <param name="a"> The first FrameTime to compare </param>
        /// <param name="b"> The second FrameTime to compare </param>
        /// <returns>-1 if a is smaller, 0 if the same, 1 if a is larger</returns>
        public static int CompareCount(FrameTime a, FrameTime b)
        {
            return a.count.CompareTo(b.count);
        }
    }
}
