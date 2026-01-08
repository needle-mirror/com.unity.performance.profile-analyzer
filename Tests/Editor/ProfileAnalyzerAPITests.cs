using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Performance.ProfileAnalyzer;

public class ProfileAnalyzerAPITests : ProfileAnalyzerBaseTest
{
    [Test]
    public void ProfileAnalyzer_EmptyAnalysis_HasNoThreads()
    {
        var analyzer = m_SetupData.analyzer;
        Assert.IsTrue(0 == analyzer.GetThreadNames().Count);
    }

    [Test]
    public void ProfileAnalyzer_EmptyAnalysis_HasNoProgress()
    {
        var analyzer = m_SetupData.analyzer;
        Assert.IsTrue(0 == analyzer.GetProgress());
    }

    [Test]
    public void ProfileAnalyzer_EmptyAnalysis_ReturnsNullForAnalysis()
    {
        var analysis = GetAnalysisFromFrameData(null);
        Assert.IsNull(analysis);
    }

    [Test]
    public void ProfileAnalyzer_MultipleMarkerInstances_ReturnsCombinedTime()
    {
        ExpectedResults expectedResults;
        var profileData = SetupCommonProfileData(out expectedResults);
        int numberOfFrames = profileData.GetFrameCount();

        m_SetupData = new FrameSetupData(1, numberOfFrames, -1, new List<string> { "1:Main Thread" });

        var analysis = GetAnalysisFromFrameData(profileData);

        MarkerData markerData = analysis.GetMarker(0);
        float expectedTime = expectedResults.totalTimePerFrameForFirstMarker * numberOfFrames;
        Assert.AreEqual(expectedTime, markerData.msTotal);   // Total time of this marker on a frame
    }

    [Test]
    public void ProfileAnalyzer_MultipleMarkerInstances_ReturnsTotalTimeForIndividualMarkerTimes()
    {
        ExpectedResults expectedResults;
        var profileData = SetupCommonProfileData(out expectedResults);
        int numberOfFrames = profileData.GetFrameCount();

        m_SetupData = new FrameSetupData(1, numberOfFrames, -1, new List<string> { "1:Main Thread" });

        var analysis = GetAnalysisFromFrameData(profileData);

        MarkerData markerData = analysis.GetMarker(0);
        Assert.AreEqual(expectedResults.msMinIndividualForFirstMarker, markerData.msMinIndividual);
        Assert.AreEqual(expectedResults.msMaxIndividualForFirstMarker, markerData.msMaxIndividual);
    }

    [Test]
    public void ProfileAnalyzer_MultipleMarkerInstances_1stFrameSetup()
    {
        ExpectedResults expectedResults;
        var profileData = SetupCommonProfileData(out expectedResults);
        int numberOfFrames = profileData.GetFrameCount();

        m_SetupData = new FrameSetupData(1, numberOfFrames, -1, new List<string> { "1:Main Thread" });

        var analysis = GetAnalysisFromFrameData(profileData);

        MarkerData markerData = analysis.GetMarker(0);
        Assert.AreEqual(1, markerData.firstFrameIndex);
    }

    [Test]
    public void ProfileAnalyzer_SelfTimesFlag_ReturnsSelfTime()
    {
        ExpectedResults expectedResults;
        var profileData = SetupCommonProfileData(out expectedResults);
        int numberOfFrames = profileData.GetFrameCount();

        m_SetupData = new FrameSetupData(1, numberOfFrames, -1, new List<string> { "1:Main Thread" });
        m_SetupData.selfTimes = true;

        var analysis = GetAnalysisFromFrameData(profileData);

        MarkerData markerData = analysis.GetMarker(0);
        float expectedTime = expectedResults.totalTimePerFrameForFirstMarker - expectedResults.msChildTime;
        expectedTime *= numberOfFrames;
        Assert.AreEqual(expectedTime, markerData.msTotal);   // Total time of this marker on a frame
    }

    [Test]
    public void ProfileAnalyzer_SelfTimesFlag_ReturnsSelfTimeForIndividualInstances()
    {
        ExpectedResults expectedResults;
        var profileData = SetupCommonProfileData(out expectedResults);
        int numberOfFrames = profileData.GetFrameCount();

        m_SetupData = new FrameSetupData(1, numberOfFrames, -1, new List<string> { "1:Main Thread" });
        m_SetupData.selfTimes = true;

        var analysis = GetAnalysisFromFrameData(profileData);

        MarkerData markerData = analysis.GetMarker(0);
        Assert.AreEqual(expectedResults.msMinIndividualForFirstMarker, markerData.msMinIndividual);

        float expectedTime = expectedResults.msMaxIndividualForFirstMarker - expectedResults.msChildTime;     // Reduced due to child
        Assert.AreEqual(expectedTime, markerData.msMaxIndividual);
    }

    [Test]
    public void ProfileAnalyzer_RemoveMarker_ReturnsReducedMarkerTime()
    {
        ExpectedResults expectedResults;
        var profileData = SetupCommonProfileData(out expectedResults);
        int numberOfFrames = profileData.GetFrameCount();

        m_SetupData = new FrameSetupData(1, numberOfFrames, -1, new List<string> { "1:Main Thread" });
        m_SetupData.removeMarker = "ChildMarker";

        var analysis = GetAnalysisFromFrameData(profileData);

        MarkerData markerData = analysis.GetMarker(0);
        float expectedTime = expectedResults.totalTimePerFrameForFirstMarker - expectedResults.msChildTime;
        expectedTime *= numberOfFrames;
        Assert.AreEqual(expectedTime, markerData.msTotal);   // Total time of this marker on a frame
    }

    [Test]
    public void ProfileAnalyzer_RemoveMarker_ReducesFrameSummaryTime()
    {
        ExpectedResults expectedResults;
        var profileData = SetupCommonProfileData(out expectedResults);
        int numberOfFrames = profileData.GetFrameCount();

        m_SetupData = new FrameSetupData(1, numberOfFrames, -1, new List<string> { "1:Main Thread" });
        m_SetupData.removeMarker = "ChildMarker";

        var analysis = GetAnalysisFromFrameData(profileData);

        var frameSummary = analysis.GetFrameSummary();

        float expectedTime = expectedResults.frameTime - expectedResults.msChildTime;
        expectedTime *= numberOfFrames;
        Assert.AreEqual(expectedTime, frameSummary.msTotal);
    }
}
