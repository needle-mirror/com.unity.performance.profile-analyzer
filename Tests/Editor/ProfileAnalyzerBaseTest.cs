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
        internal ProfileData profileData;
        internal int depthFilter;
        internal string threadFilter;
        internal int firstFrame;
        internal int lastFrame;
        internal FrameSetupData(int minFrame, int maxFrame, int filterDepth, string filterThread)
        {
            progressBar = new ProgressBarDisplay();
            firstFrame = minFrame;
            lastFrame = maxFrame;
            analyzer = new ProfileAnalyzer(progressBar);
            profileData = analyzer.PullFromProfiler(minFrame, maxFrame);
            depthFilter = filterDepth;
            threadFilter = filterThread;
        }
    };

    protected FrameSetupData m_setupData;

    [SetUp]
    public void SetupTest()
    {
        UnityEditorInternal.ProfilerDriver.ClearAllFrames();
        m_setupData = new FrameSetupData(0, 300, -1, "1:Main Thread");
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

    protected ProfileAnalysis GetAnalysisFromFrameData(ProfileData profileData)
    {
        return m_setupData.analyzer.Analyze(profileData,
                                            SelectRange(m_setupData.firstFrame, m_setupData.lastFrame),
                                            m_setupData.threadFilter,
                                            m_setupData.depthFilter);
    }

    protected void StartProfiler()
    {
        ProfilerDriver.enabled = true;
        ProfilerDriver.profileEditor = true;
    }

    protected void StopProfiler()
    {
        EditorApplication.isPlaying = false;
        ProfilerDriver.enabled = false;
        ProfilerDriver.profileEditor = false;
    }

}