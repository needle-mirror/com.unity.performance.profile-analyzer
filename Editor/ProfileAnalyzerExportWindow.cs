using UnityEngine;
using System.IO;
using System;

namespace UnityEditor.Performance.ProfileAnalyzer
{
    public class ProfileAnalyzerExportWindow : EditorWindow
    {
        ProfileData m_ProfileData;
        ProfileData m_LeftData;
        ProfileData m_RightData;

        public void SetData(ProfileData profileData, ProfileData leftData, ProfileData rightData)
        {
            m_ProfileData = profileData;
            m_LeftData = leftData;
            m_RightData = rightData;
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Export as CSV:");

            if (m_ProfileData != null)
            {
                if (GUILayout.Button("Single Frame Times"))
                    SaveFrameTimesCSV();
            }

            if (m_LeftData != null && m_RightData != null)
            {
                if (GUILayout.Button("Comparison Frame Times"))
                    SaveComparisonFrameTimesCSV();
            }

            EditorGUILayout.EndVertical();
        }

        private void SaveFrameTimesCSV()
        {
            if (m_ProfileData == null)
                return;

            string path = EditorUtility.SaveFilePanel("Save frame time CSV data", "", "frameTime.csv", "csv");
            if (path.Length != 0)
            {
                var analytic = ProfileAnalyzerAnalytics.BeginAnalytic();
                using (StreamWriter file = new StreamWriter(path))
                {
                    file.WriteLine("Frame Index, Frame Start (ms), Frame Start Offset (ms), Frame Time (ms)");
                    float maxFrames = m_ProfileData.GetFrameCount();

                    var frame = m_ProfileData.GetFrame(0);
                    double msInitialFrameStart = frame.msStartTime;

                    for (int frameIndex = 0; frameIndex < maxFrames; frameIndex++)
                    {
                        frame = m_ProfileData.GetFrame(frameIndex);
                        double msFrameStart = frame.msStartTime;
                        double msFrameStartOffset = frame.msStartTime - msInitialFrameStart;
                        float msFrame = frame.msFrame;
                        file.WriteLine("{0},{1},{2},{3}",
                            frameIndex, msFrameStart, msFrameStartOffset, msFrame);
                    }
                }
                ProfileAnalyzerAnalytics.SendUIButtonEvent(ProfileAnalyzerAnalytics.UIButton.ExportSingleFrames, analytic);
            }
        }

        private void SaveComparisonFrameTimesCSV()
        {
            if (m_LeftData == null || m_RightData == null)
                return;

            string path = EditorUtility.SaveFilePanel("Save comparison frame time CSV data", "", "frameTimeComparison.csv", "csv");
            if (path.Length != 0)
            {
                var analytic = ProfileAnalyzerAnalytics.BeginAnalytic();
                using (StreamWriter file = new StreamWriter(path))
                {
                    file.WriteLine("Frame Index, Left Frame Start (ms), Left Frame Start Offset (ms), Left Frame Time (ms), Right Frame Start (ms), Right Frame Start Offset (ms), Right Frame Time (ms), Frame Time Diff (ms)");
                    float maxFrames = Math.Max(m_LeftData.GetFrameCount(), m_RightData.GetFrameCount());

                    var leftFrame = m_LeftData.GetFrame(0);
                    var rightFrame = m_RightData.GetFrame(0);
                    double msInitialFrameStartLeft = leftFrame != null ? leftFrame.msStartTime : 0.0;
                    double msInitialFrameStartRight = rightFrame != null ? rightFrame.msStartTime : 0.0;

                    for (int frameIndex = 0; frameIndex < maxFrames; frameIndex++)
                    {
                        leftFrame = m_LeftData.GetFrame(frameIndex);
                        rightFrame = m_RightData.GetFrame(frameIndex);
                        double msFrameStartLeft = leftFrame != null ? leftFrame.msStartTime : 0.0;
                        double msFrameStartRight = rightFrame != null ? rightFrame.msStartTime : 0.0;
                        double msFrameStartOffsetLeft = leftFrame != null ? leftFrame.msStartTime - msInitialFrameStartLeft : 0.0;
                        double msFrameStartOffsetRight = rightFrame != null ? rightFrame.msStartTime - msInitialFrameStartRight : 0.0;
                        float msFrameLeft = leftFrame != null ? leftFrame.msFrame : 0;
                        float msFrameRight = rightFrame != null ? rightFrame.msFrame : 0;
                        float msFrameDiff = msFrameRight - msFrameLeft;
                        file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}",
                            frameIndex,
                            msFrameStartLeft, msFrameStartOffsetLeft, msFrameLeft,
                            msFrameStartRight, msFrameStartOffsetRight, msFrameRight,
                            msFrameDiff);
                    }
                }
                ProfileAnalyzerAnalytics.SendUIButtonEvent(ProfileAnalyzerAnalytics.UIButton.ExportComparisonFrames, analytic);
            }
        }
    }
}
