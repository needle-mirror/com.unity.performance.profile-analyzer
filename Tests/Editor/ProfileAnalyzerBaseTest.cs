using NUnit.Framework;
using UnityEditor.Performance.ProfileAnalyzer;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

public class ProfileAnalyzerBaseTest
{
    protected struct FrameSetupData
    {
        internal ProgressBarDisplay progressBar;
        internal ProfileAnalyzer analyzer;
        internal ProfilerWindowInterface profilerWindowInterface;
        internal ProfileData profileData;
        internal int depthFilter;
        internal List<string> threadFilters;
        internal int firstFrame;
        internal int lastFrame;
        internal bool selfTimes;
        internal string parentMarker;
        internal float timeScaleMin;
        internal float timeScaleMax;
        internal string removeMarker;
        internal FrameSetupData(int minFrame, int maxFrame, int filterDepth, List<string> filterThreads)
        {
            progressBar = new ProgressBarDisplay();
            firstFrame = minFrame;
            lastFrame = maxFrame;
            analyzer = new ProfileAnalyzer();
            profilerWindowInterface = new ProfilerWindowInterface(progressBar);
            profileData = profilerWindowInterface.PullFromProfiler(minFrame, maxFrame);
            depthFilter = filterDepth;
            threadFilters = filterThreads;
            selfTimes = false;
            parentMarker = null;
            timeScaleMin = 0;
            timeScaleMax = 0;
            removeMarker = null;
        }
    };

    protected FrameSetupData m_SetupData;

    [SetUp]
    public void SetupTest()
    {
        ProfilerDriver.ClearAllFrames();
        m_SetupData = new FrameSetupData(1, 300, -1, new List<string> { "1:Main Thread" });
    }

    [TearDown]
    public void TearDownTest()
    {
    }

    List<int> SelectRange(int startIndex, int endIndex)
    {
        List<int> list = new List<int>();
        for (int c = startIndex; c <= endIndex; c++)
        {
            list.Add(c);
        }

        return list;
    }

    internal ProfileData SetupProfileData(ProfileMarker[] markers, string[] markerNames, int numberOfFrames)
    {
        var profileData = new ProfileData();

        double msFrameStartTime = 0;
        float msFrameDuration = 16.6f;
        for (int frameIndex = 0; frameIndex < numberOfFrames; frameIndex++)
        {
            var profileFrame = new ProfileFrame();
            profileFrame.msStartTime = msFrameStartTime;
            profileFrame.msFrame = msFrameDuration;
            msFrameStartTime += msFrameDuration;

            var profileThread = new ProfileThread();
            profileThread.fileVersion = ProfileData.latestVersion;
            profileThread.threadIndex = 0;
            profileData.AddThreadName("1:Main Thread", profileThread);

            var markerArray = new ProfileMarker[markers.Length];
            for (int markerIndex = 0; markerIndex < markers.Length; markerIndex++)
            {
                markerArray[markerIndex].depth = markers[markerIndex].depth;
                markerArray[markerIndex].msMarkerTotal = markers[markerIndex].msMarkerTotal;
                profileData.AddMarkerName(markerNames[markers[markerIndex].nameIndex], ref markerArray[markerIndex]);  // checks if already present and only adds if new, updates the name index in the marker entry
            }
            profileThread.AddMarkerArray(markerArray);
            profileFrame.Add(profileThread);

            profileData.Add(profileFrame);

        }

        // Calculate child marker times
        profileData.Finalise();

        return profileData;
    }

    internal struct ExpectedResults
    {
        public float frameTime;
        public float totalTimePerFrameForFirstMarker;
        public float msMinIndividualForFirstMarker;
        public float msMaxIndividualForFirstMarker;
        public float msChildTime;
    }

    internal ProfileData SetupCommonProfileData(out ExpectedResults expectedResults)
    {
        ProfileMarker[] markerDefinition =
        {
            new() { nameIndex = 0, msMarkerTotal = 0.5f, depth = 1 },
            new() { nameIndex = 0, msMarkerTotal = 1.5f, depth = 1 },
            new() { nameIndex = 1, msMarkerTotal = 0.1f, depth = 2 },
        };
        string[] markerNames =
        {
            "MyMarker",
            "ChildMarker",
        };

        int numberOfFrames = 2;

        expectedResults.frameTime = 16.6f;
        expectedResults.totalTimePerFrameForFirstMarker = 2.0f;
        expectedResults.msMinIndividualForFirstMarker = 0.5f;
        expectedResults.msMaxIndividualForFirstMarker = 1.5f;
        expectedResults.msChildTime = 0.1f;

        return SetupProfileData(markerDefinition, markerNames, numberOfFrames);
    }

    internal ProfileAnalysis GetAnalysisFromFrameData(ProfileData profileData)
    {
        return m_SetupData.analyzer.Analyze(profileData,
            SelectRange(m_SetupData.firstFrame, m_SetupData.lastFrame),
            m_SetupData.threadFilters,
            m_SetupData.depthFilter,
            m_SetupData.selfTimes,
            m_SetupData.parentMarker,
            m_SetupData.timeScaleMin,
            m_SetupData.timeScaleMax,
            m_SetupData.removeMarker);
    }

    protected void StartProfiler()
    {
#if UNITY_2017_1_OR_NEWER
        ProfilerDriver.enabled = true;
#endif
        ProfilerDriver.profileEditor = true;
    }

    protected void StopProfiler()
    {
        EditorApplication.isPlaying = false;
#if UNITY_2017_1_OR_NEWER
        ProfilerDriver.enabled = false;
#endif
        ProfilerDriver.profileEditor = false;
    }
}
