
using System;

namespace UnityEditor.Performance.ProfileAnalyzer
{
    public struct FrameTime : IComparable<FrameTime>
    {
        public float ms;
        public int frameIndex;
        public int count;

        public FrameTime(int index, float msTime, int _count)
        {
            frameIndex = index;
            ms = msTime;
            count = _count;
        }

        public int CompareTo(FrameTime other)
        {
            return ms.CompareTo(other.ms);
        }
    }
}
