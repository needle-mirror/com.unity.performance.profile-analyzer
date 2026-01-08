using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using System;
using System.Threading.Tasks;
using ProfilerMarkerAbstracted = Unity.Profiling.ProfilerMarker;

namespace UnityEditor.Performance.ProfileAnalyzer
{
    internal class ProfileAnalyzer
    {
        public const int kDepthAll = -1;

        int m_Progress = 0;
        ProfilerFrameDataIterator m_frameData;
        List<string> m_threadNames = new List<string>();
        ProfileAnalysis m_analysis;

        public ProfileAnalyzer() { }

        public List<string> GetThreadNames()
        {
            return m_threadNames;
        }

        void CalculateFrameTimeStats(ProfileData data, out float median, out float mean, out float standardDeviation)
        {
            List<float> frameTimes = new List<float>();
            for (int frameIndex = 0; frameIndex < data.GetFrameCount(); frameIndex++)
            {
                var frame = data.GetFrame(frameIndex);
                float msFrame = frame.msFrame;
                frameTimes.Add(msFrame);
            }

            frameTimes.Sort();
            median = frameTimes[frameTimes.Count / 2];


            double total = 0.0f;
            foreach (float msFrame in frameTimes)
            {
                total += msFrame;
            }

            mean = (float)(total / (double)frameTimes.Count);


            if (frameTimes.Count <= 1)
            {
                standardDeviation = 0f;
            }
            else
            {
                total = 0.0f;
                foreach (float msFrame in frameTimes)
                {
                    float d = msFrame - mean;
                    total += (d * d);
                }

                total /= (frameTimes.Count - 1);
                standardDeviation = (float)Math.Sqrt(total);
            }
        }

        int GetClampedOffsetToFrame(ProfileData profileData, int frameIndex)
        {
            int frameOffset = profileData.DisplayFrameToOffset(frameIndex);
            if (frameOffset < 0)
            {
                Debug.Log(string.Format("Frame index {0} offset {1} < 0, clamping", frameIndex, frameOffset));
                frameOffset = 0;
            }

            if (frameOffset >= profileData.GetFrameCount())
            {
                Debug.Log(string.Format("Frame index {0} offset {1} >= frame count {2}, clamping", frameIndex, frameOffset, profileData.GetFrameCount()));
                frameOffset = profileData.GetFrameCount() - 1;
            }

            return frameOffset;
        }

        public static bool MatchThreadFilter(string threadNameWithIndex, List<string> threadFilters)
        {
            if (threadFilters == null || threadFilters.Count == 0)
                return false;

            if (threadFilters.Contains(threadNameWithIndex))
                return true;

            return false;
        }

        public void RemoveMarkerTimeFromParents(PerFrameMarkerData[] markers, ProfileData profileData, ProfileThread threadData, int markerAt)
        {
            // Get the info for the marker we plan to remove (assume thats what we are at)
            ProfileMarker profileMarker = threadData.markers[markerAt];
            float markerTime = profileMarker.msMarkerTotal;

            // Traverse parents and remove time from them
            int currentDepth = profileMarker.depth;
            for (int parentMarkerAt = markerAt - 1; parentMarkerAt >= 0; parentMarkerAt--)
            {
                ProfileMarker parentMarkerData = threadData.markers[parentMarkerAt];
                if (parentMarkerData.depth == currentDepth - 1)
                {
                    currentDepth--;

                    // Had an issue where marker not yet processed(marker from another thread)
                    // If a depth slice is applied we may not have a parent marker stored
                    if (markers[parentMarkerData.nameIndex].globalNameIndex != -1)
                    {
                        markers[parentMarkerData.nameIndex].RemoveMarkerTimeFromParent(markerTime);
                    }
                }
            }
        }

        public int RemoveMarker(ProfileThread threadData, int markerAt)
        {
            ProfileMarker profileMarker = threadData.markers[markerAt];
            int at = markerAt;

            // skip marker
            at++;

            // Skip children
            int currentDepth = profileMarker.depth;
            while (at < threadData.markers.Length)
            {
                profileMarker = threadData.markers[at];
                if (profileMarker.depth <= currentDepth)
                    break;

                at++;
            }

            // Mark the following number to be ignored
            int markerAndChildCount = at - markerAt;

            return markerAndChildCount;
        }



        public void UpdateMarker(
            MarkerData marker,
            PerFrameMarkerData perFrameMarker,
            int frameIndex)
        {
            marker.count += perFrameMarker.count;
            marker.msTotal += perFrameMarker.msTotal;

            // Individual marker time (not total over frame)
            if (perFrameMarker.msMinIndividual < marker.msMinIndividual)
            {
                marker.msMinIndividual = perFrameMarker.msMinIndividual;
                marker.minIndividualFrameIndex = perFrameMarker.minIndividualFrameIndex;
            }

            if (perFrameMarker.msMaxIndividual > marker.msMaxIndividual)
            {
                marker.msMaxIndividual = perFrameMarker.msMaxIndividual;
                marker.maxIndividualFrameIndex = perFrameMarker.maxIndividualFrameIndex;
            }

            // Record highest depth found
            if (perFrameMarker.minDepth < marker.minDepth)
                marker.minDepth = perFrameMarker.minDepth;
            if (perFrameMarker.maxDepth > marker.maxDepth)
                marker.maxDepth = perFrameMarker.maxDepth;

            marker.presentOnFrameCount += 1;
            var frameTime = new FrameTime(frameIndex, perFrameMarker.msTotal, perFrameMarker.count);
            marker.frames.Add(frameTime);

            // Add all thread names this marker occurs on
            for (int i = 0; i < perFrameMarker.threadIndexCount; i++)
            {
                int threadIndex = perFrameMarker.GetThreadIndex(i);

                if (!marker.globalThreadIndices.Contains(threadIndex))
                    marker.globalThreadIndices.Add(threadIndex);
            }
        }

        internal struct FrameProcessingSettings
        {
            public bool ProcessMarkers { get; }
            public List<string> ThreadFilters { get; }
            public int DepthFilter { get; }
            public bool FilteringByParentMarker { get; }
            public int ParentMarkerIndex { get; }
            public ThreadIdentifier MainThreadIdentifier { get; }
            public bool SelfTimes { get; }
            public string RemoveMarker { get; }

            public FrameProcessingSettings(
                ProfileData profileData,
                List<string> threadFilters,
                int depthFilter,
                string parentMarker,
                bool selfTimes = false,
                string removeMarker = null
            )
            {
                ThreadFilters = threadFilters;
                ProcessMarkers = (threadFilters != null);
                DepthFilter = depthFilter;

                FilteringByParentMarker = false;
                ParentMarkerIndex = -1;
                if (!Utility.IsNullOrWhiteSpace(parentMarker))
                {
                    // Returns -1 if this marker doesn't exist in the data set
                    ParentMarkerIndex = profileData.GetMarkerIndex(parentMarker);
                    FilteringByParentMarker = true;
                }

                MainThreadIdentifier = new ThreadIdentifier("Main Thread", 1);

                SelfTimes = selfTimes;
                RemoveMarker = removeMarker;
            }
        };

        static readonly ProfilerMarkerAbstracted k_ProcessFrameProfilerMarker = new ProfilerMarkerAbstracted("ProfileAnalyzer.ProcessFrame");

        public void ProcessFrame(
            ProfileData profileData,
            int frameIndex,
            ProfileFrame frameData,
            FrameProcessingSettings settings,
            PerFrameOutputData result)
        {
            using (k_ProcessFrameProfilerMarker.Auto())
            {
                if (frameData == null)
                    return;

                var msFrame = frameData.msFrame;
                if (settings.ProcessMarkers)
                {
                    // get the file reader in case we need to rebuild the markers rather than opening
                    // the file for every marker
                    for (int perFrameThreadIndex = 0; perFrameThreadIndex < frameData.threads.Count; perFrameThreadIndex++)
                    {
                        float msTimeOfMinDepthMarkers = 0.0f;
                        float msIdleTimeOfMinDepthMarkers = 0.0f;

                        var threadData = frameData.threads[perFrameThreadIndex];
                        result.threadsOnFrame[threadData.threadIndex].globalThreadIndex = threadData.threadIndex;

                        var threadNameWithIndex = profileData.GetThreadName(threadData);
                        bool include = MatchThreadFilter(threadNameWithIndex, settings.ThreadFilters);

                        int parentMarkerDepth = -1;

                        if (threadData.markers.Length != threadData.markerCount)
                        {
                            if (!threadData.ReadMarkers(profileData.FilePath))
                            {
                                Debug.LogError($"failed to read markers from {profileData.FilePath}");
                            }
                        }

                        int markerAndChildCount = 0;
                        for (int markerAt = 0, n = threadData.markers.Length; markerAt < n; markerAt++)
                        {
                            var markerData = threadData.markers[markerAt];

                            if (markerAndChildCount > 0)
                                markerAndChildCount--;

                            string markerName = null;

                            float ms = markerData.msMarkerTotal - (settings.SelfTimes ? markerData.msChildren : 0);
                            var markerDepth = markerData.depth;
                            if (markerDepth > result.maxMarkerDepthFound)
                                result.maxMarkerDepthFound = markerDepth;

                            if (markerDepth == 1)
                            {
                                markerName = profileData.GetMarkerName(markerData);
                                if (markerName.Equals("Idle", StringComparison.Ordinal))
                                    msIdleTimeOfMinDepthMarkers += ms;
                                else
                                    msTimeOfMinDepthMarkers += ms;
                            }

                            if (settings.RemoveMarker != null)
                            {
                                if (markerAndChildCount <= 0) // If we are already removing markers - don't focus on other occurances in the children
                                {
                                    if (markerName == null)
                                        markerName = profileData.GetMarkerName(markerData);

                                    if (markerName == settings.RemoveMarker)
                                    {
                                        float removeMarkerTime = markerData.msMarkerTotal;

                                        // Remove this markers time from frame time (if its on the main thread)
                                        if (threadNameWithIndex == settings.MainThreadIdentifier.threadNameWithIndex)
                                        {
                                            msFrame -= removeMarkerTime;
                                        }

                                        if (settings.SelfTimes == false) // (Self times would not need thread or parent adjustments)
                                        {
                                            // And from thread time
                                            if (markerName == "Idle")
                                                msIdleTimeOfMinDepthMarkers -= removeMarkerTime;
                                            else
                                                msTimeOfMinDepthMarkers -= removeMarkerTime;

                                            // And from parents
                                            RemoveMarkerTimeFromParents(result.markersOnFrame, profileData, threadData, markerAt);
                                        }

                                        markerAndChildCount = RemoveMarker(threadData, markerAt);
                                    }
                                }
                            }

                            if (!include)
                                continue;

                            // If only looking for markers below the parent
                            if (settings.FilteringByParentMarker)
                            {
                                // If found the parent marker
                                if (markerData.nameIndex == settings.ParentMarkerIndex)
                                {
                                    // And we are not already below the parent higher in the depth tree
                                    if (parentMarkerDepth < 0)
                                    {
                                        // record the parent marker depth
                                        parentMarkerDepth = markerData.depth;
                                    }
                                }
                                else
                                {
                                    // If we are now above or beside the parent marker then we are done for this level
                                    if (markerData.depth <= parentMarkerDepth)
                                    {
                                        parentMarkerDepth = -1;
                                    }
                                }

                                if (parentMarkerDepth < 0)
                                    continue;
                            }

                            if (settings.DepthFilter != kDepthAll && markerDepth != settings.DepthFilter)
                                continue;

                            if (result.markersOnFrame[markerData.nameIndex].globalNameIndex == -1)
                            {
                                result.markersOnFrame[markerData.nameIndex].globalNameIndex = markerData.nameIndex;
                                result.markersOnFrame[markerData.nameIndex].minDepth = markerDepth;
                                result.markersOnFrame[markerData.nameIndex].maxDepth = markerDepth;
                            }

                            result.markersOnFrame[markerData.nameIndex].SetValue(threadData.threadIndex, frameIndex, ms, markerDepth, markerAndChildCount);
                        }

                        if (include)
                        {
                            result.threadsOnFrame[threadData.threadIndex].FrameTime = new ThreadFrameTime(frameIndex, msTimeOfMinDepthMarkers, msIdleTimeOfMinDepthMarkers);
                        }
                    }

                    result.msFrame = msFrame;
                }
            }
        }

        internal unsafe struct PerFrameMarkerData
        {
            public int globalNameIndex;

            public float msTotal; // total time of this marker on a frame
            public float msMinIndividual; // min individual function call
            public float msMaxIndividual; // max individual function call
            public int minIndividualFrameIndex;
            public int maxIndividualFrameIndex;

            public int count; // total number of marker calls in the frame
            public int minDepth;
            public int maxDepth;
            const int staticThreadIndexStoreSize = 10;
            fixed int threadIndices[staticThreadIndexStoreSize];    // Index into the threads list for the frame
            List<int> extendedThreadIndices;                        // Index into the threads list for the frame if more than the static size
            public int threadIndexCount { get; private set; }

            public double timeRemoved;
            public double timeIgnored;

            public void Init()
            {
                globalNameIndex = -1;
                msMinIndividual = float.MaxValue;
                msMaxIndividual = float.MinValue;
                threadIndexCount = 0;
            }

            bool ContainsThreadIndex(int threadIndex)
            {
                if (threadIndexCount == 0)
                    return false;

                int max = threadIndexCount > staticThreadIndexStoreSize ? staticThreadIndexStoreSize : threadIndexCount;
                for (int i = 0; i < max; i++)
                {
                    if (threadIndices[i] == threadIndex)
                        return true;
                }

                if (extendedThreadIndices != null)
                {
                    for (int i = 0; i < extendedThreadIndices.Count; i++)
                    {
                        if (extendedThreadIndices[i] == threadIndex)
                            return true;
                    }
                }

                return false;
            }

            void AddThreadIndexInternal(int threadIndex)
            {
                if (threadIndexCount < staticThreadIndexStoreSize)
                {
                    threadIndices[threadIndexCount] = threadIndex;
                }
                else
                {
                    extendedThreadIndices ??= new List<int>(1);
                    extendedThreadIndices.Add(threadIndex);
                }
                threadIndexCount++;
            }

            void AddThreadIndex(int threadIndex)
            {
                if (!ContainsThreadIndex(threadIndex))
                    AddThreadIndexInternal(threadIndex);
            }

            public int GetThreadIndex(int index)
            {
                if (index >= threadIndexCount)
                    return -1;

                if (index < staticThreadIndexStoreSize)
                    return threadIndices[index];

                return extendedThreadIndices[index - 10];
            }

            public void SetValue(
                int threadIndex,
                int frameIndex,
                float ms,
                int markerDepth,
                int markerAndChildCount)
            {
                count += 1;

                if (markerAndChildCount > 0)
                {
                    timeIgnored += ms;

                    // Note ms can be 0 in some cases.
                    // Make sure timeIgnored is never left at 0.0
                    // This makes sure we can test for non zero to indicate the marker has been ignored
                    if (timeIgnored == 0.0)
                        timeIgnored = double.Epsilon;

                    // zero out removed marker time
                    // so we don't record in the individual marker times, marker frame times or min/max times
                    // ('min/max times' is calculated later from marker frame times)
                    ms = 0f;
                }

                msTotal += ms;

                // Individual marker time (not total over frame)
                if (ms < msMinIndividual)
                {
                    msMinIndividual = ms;
                    minIndividualFrameIndex = frameIndex;
                }

                if (ms > msMaxIndividual)
                {
                    msMaxIndividual = ms;
                    maxIndividualFrameIndex = frameIndex;
                }

                // Record highest depth found
                if (markerDepth < minDepth)
                    minDepth = markerDepth;
                if (markerDepth > maxDepth)
                    maxDepth = markerDepth;

                AddThreadIndex(threadIndex);
            }

            public void RemoveMarkerTimeFromParent(float markerTime)
            {
                // Revise the duration of parent to remove time from there too
                // Note if the marker to remove is nested (i.e. parent of the same name, this could reduce the msTotal, more than we add to the timeIgnored)
                msTotal -= markerTime;

                // Reduce from the max marker time too
                // This could be incorrect when there are many instances that contribute to the total time
                if (msMaxIndividual > markerTime)
                {
                    msMaxIndividual -= markerTime;
                }

                if (msMinIndividual > markerTime)
                {
                    msMinIndividual -= markerTime;
                }

                // Note that we have modified the time
                timeRemoved += markerTime;

                // Note markerTime can be 0 in some cases.
                // Make sure timeRemoved is never left at 0.0
                // This makes sure we can test for non zero to indicate the marker has been removed
                if (timeRemoved == 0.0)
                    timeRemoved = double.Epsilon;
            }

        }

        internal struct PerFrameThreadData
        {
            public int globalThreadIndex;
            public ThreadFrameTime FrameTime { get; set; }
        }

        internal class PerFrameOutputData
        {
            public PerFrameThreadData[] threadsOnFrame;
            public PerFrameMarkerData[] markersOnFrame;
            public int maxMarkerDepthFound;
            public float msFrame;

            public PerFrameOutputData(int threadCount, int markerCount)
            {
                threadsOnFrame = new PerFrameThreadData[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    threadsOnFrame[i].globalThreadIndex = -1;
                }

                markersOnFrame = new PerFrameMarkerData[markerCount];
                for (int i = 0; i < markerCount; i++)
                {
                    markersOnFrame[i].Init();
                }

                maxMarkerDepthFound = 0;
            }
        }

        static readonly ProfilerMarkerAbstracted k_AllocateFrameResultsStorageProfilerMarker = new ProfilerMarkerAbstracted("ProfileAnalyzer.AllocateFrameResultsStorage");

        public List<PerFrameOutputData> AllocateFrameResultsStorage(int threadCount, int frameCount, int markerCount)
        {
            using (k_AllocateFrameResultsStorageProfilerMarker.Auto())
            {
                List<PerFrameOutputData> frameResults = new List<PerFrameOutputData>(frameCount);
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                    frameResults.Add(new PerFrameOutputData(threadCount, markerCount));

                return frameResults;
            }
        }


        static readonly ProfilerMarkerAbstracted k_MergeMarkersProfilerMarker = new ProfilerMarkerAbstracted("ProfileAnalyzer.MergeMarkers");
        static readonly ProfilerMarkerAbstracted k_FinaliseProfilerMarker = new ProfilerMarkerAbstracted("ProfileAnalyzer.Finalise");

        public ProfileAnalysis MergeMarkers(ProfileData profileData, List<int> selectionIndices, List<PerFrameOutputData> frameResults, float timeScaleMin, float timeScaleMax)
        {
            int frameCount = selectionIndices.Count;
            if (frameCount < 0)
            {
                return null;
            }

            ProfileAnalysis analysis = new ProfileAnalysis(profileData.MarkerNameCount);
            int maxMarkerDepthFound = 0;

            using (k_MergeMarkersProfilerMarker.Auto())
            {
                if (selectionIndices.Count > 0)
                    analysis.SetRange(selectionIndices[0], selectionIndices[selectionIndices.Count - 1]);
                else
                    analysis.SetRange(0, 0);

                m_threadNames.Clear();

                // Merge frames
                for (int selectionAt = 0; selectionAt < frameCount; selectionAt++)
                {
                    int frameIndex = selectionIndices[selectionAt];
                    foreach (var threadOnFrame in frameResults[selectionAt].threadsOnFrame)
                    {
                        if (threadOnFrame.globalThreadIndex == -1) // Skip missing threads
                            continue;

                        var threadNameWithIndex = profileData.GetThreadNameFromIndex(threadOnFrame.globalThreadIndex);
                        var threadData = analysis.GetThreadByName(threadNameWithIndex);
                        if (threadData == null)
                        {
                            threadData = new ThreadData(threadNameWithIndex);
                            threadData.frames.Add(threadOnFrame.FrameTime);
                            analysis.AddThread(threadData);
                        }

                        threadData.frames.Add(threadOnFrame.FrameTime);
                    }

                    foreach (var markerOnFrame in frameResults[selectionAt].markersOnFrame)
                    {
                        if (markerOnFrame.globalNameIndex == -1) // Skip missing markers
                            continue;

                        var markerName = profileData.GetMarkerNameFromIndex(markerOnFrame.globalNameIndex);
                        var marker = analysis.GetMarkerByName(markerName);
                        if (marker == null)
                        {
                            marker = new MarkerData(markerName, markerOnFrame.threadIndexCount);
                            marker.firstFrameIndex = frameIndex;
                            marker.minDepth = markerOnFrame.minDepth;
                            marker.maxDepth = markerOnFrame.maxDepth;
                            analysis.AddMarker(marker);
                        }

                        // Merge data
                        UpdateMarker(marker, markerOnFrame, frameIndex);
                    }

                    if (frameResults[selectionAt].maxMarkerDepthFound > maxMarkerDepthFound)
                        maxMarkerDepthFound = frameResults[selectionAt].maxMarkerDepthFound;
                }

                // Generate summary
                for (int selectionAt = 0; selectionAt < frameCount; selectionAt++)
                {
                    int frameIndex = selectionIndices[selectionAt];
                    analysis.UpdateSummary(frameIndex, frameResults[selectionAt].msFrame);
                }

                analysis.GetFrameSummary().totalMarkers = profileData.MarkerNameCount;

                // Store thread names
                foreach (var thread in analysis.GetThreads())
                {
                    if (!m_threadNames.Contains(thread.threadNameWithIndex))
                        m_threadNames.Add(thread.threadNameWithIndex);
                }
            }

            using (k_FinaliseProfilerMarker.Auto())
            {
                analysis.Finalise(timeScaleMin, timeScaleMax, maxMarkerDepthFound);
            }

            return analysis;
        }

        static readonly ProfilerMarkerAbstracted k_AnalyzeProfilerMarker = new ProfilerMarkerAbstracted("ProfileAnalyzer.Analyze");

        public ProfileAnalysis Analyze(ProfileData profileData, List<int> selectionIndices, List<string> threadFilters, int depthFilter, bool selfTimes = false, string parentMarker = null, float timeScaleMin = 0, float timeScaleMax = 0, string removeMarker = null)
        {
            using (k_AnalyzeProfilerMarker.Auto())
            {
                m_Progress = 0;
                if (profileData == null)
                {
                    return null;
                }

                if (profileData.GetFrameCount() <= 0)
                {
                    return null;
                }

                int frameCount = selectionIndices.Count;
                if (frameCount < 0)
                {
                    return null;
                }

                if (profileData.HasFrames && !profileData.HasThreads)
                {
                    if (!ProfileData.Load(profileData.FilePath, out profileData))
                    {
                        return null;
                    }
                }

                int threadCount = profileData.GetThreadCount();
                int markerCount = profileData.MarkerNameCount;

                List<PerFrameOutputData> frameResults = AllocateFrameResultsStorage(threadCount, frameCount, markerCount);

                var frameProcessingSettings = new FrameProcessingSettings(
                    profileData,
                    threadFilters,
                    depthFilter,
                    parentMarker,
                    selfTimes,
                    removeMarker);

                int completedFrames = 0;

                Parallel.For(0, frameCount, selectionAt =>
                    {
                        int frameIndex = selectionIndices[selectionAt];

                        int frameOffset = profileData.DisplayFrameToOffset(frameIndex);
                        var frameData = profileData.GetFrame(frameOffset);
                        if (frameData == null)
                            return;

                        ProcessFrame(
                            profileData,
                            frameIndex,
                            frameData,
                            frameProcessingSettings,
                            frameResults[selectionAt]
                        );

                        completedFrames++;
                        m_Progress = (100 * completedFrames) / frameCount;
                    }
                );

                var analysis = MergeMarkers(profileData, selectionIndices, frameResults, timeScaleMin, timeScaleMax);

                m_Progress = 100;
                return analysis;
            }
        }

        public int GetProgress()
        {
            return m_Progress;
        }
    }
}
