using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Performance.ProfileAnalyzer
{
    public class ProfilerWindowInterface
    {
        private Type m_ProfilerWindowType;
        private EditorWindow m_ProfilerWindow;
        private FieldInfo m_CurrentFrameFieldInfo;
        private FieldInfo m_TimeLineGUIFieldInfo;
        private FieldInfo m_SelectedEntryFieldInfo;
        private FieldInfo m_SelectedNameFieldInfo;
        private FieldInfo m_SelectedTimeFieldInfo;
        private FieldInfo m_SelectedDurationFieldInfo;
        private FieldInfo m_SelectedInstanceIdFieldInfo;
        private FieldInfo m_SelectedInstanceCountFieldInfo;
        private FieldInfo m_SelectedFrameIdFieldInfo;
        private FieldInfo m_SelectedThreadIdFieldInfo;
        private FieldInfo m_SelectedNativeIndexFieldInfo;

        public ProfilerWindowInterface()
        {
            Assembly assem = typeof(Editor).Assembly;
            m_ProfilerWindowType = assem.GetType("UnityEditor.ProfilerWindow");
            m_CurrentFrameFieldInfo = m_ProfilerWindowType.GetField("m_CurrentFrame", BindingFlags.NonPublic | BindingFlags.Instance);

            m_TimeLineGUIFieldInfo = m_ProfilerWindowType.GetField("m_CPUTimelineGUI", BindingFlags.NonPublic | BindingFlags.Instance);
            if (m_TimeLineGUIFieldInfo != null)
                m_SelectedEntryFieldInfo = m_TimeLineGUIFieldInfo.FieldType.GetField("m_SelectedEntry", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (m_SelectedEntryFieldInfo != null)
            {
                m_SelectedNameFieldInfo = m_SelectedEntryFieldInfo.FieldType.GetField("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_SelectedTimeFieldInfo = m_SelectedEntryFieldInfo.FieldType.GetField("time", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_SelectedDurationFieldInfo = m_SelectedEntryFieldInfo.FieldType.GetField("duration", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_SelectedInstanceIdFieldInfo = m_SelectedEntryFieldInfo.FieldType.GetField("instanceId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_SelectedInstanceCountFieldInfo = m_SelectedEntryFieldInfo.FieldType.GetField("instanceCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_SelectedFrameIdFieldInfo = m_SelectedEntryFieldInfo.FieldType.GetField("frameId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_SelectedThreadIdFieldInfo = m_SelectedEntryFieldInfo.FieldType.GetField("threadId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_SelectedNativeIndexFieldInfo = m_SelectedEntryFieldInfo.FieldType.GetField("nativeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }

        /*
        public EditorWindow GetProfileWindow()
        {
            return m_profilerWindow;
        }
        */

        public bool IsReady()
        {
            if (m_ProfilerWindow != null)
                return true;

            return false;
        }

        public bool IsProfilerWindowOpen()
        {
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(m_ProfilerWindowType);
            if (windows != null && windows.Length > 0)
                return true;

            return false;
        }

        public void OpenProfilerOrUseExisting()
        {
            m_ProfilerWindow = EditorWindow.GetWindow(m_ProfilerWindowType);
        }

        public bool GetFrameRangeFromProfiler(out int first, out int last)
        {
            if (m_ProfilerWindow)
            //if (ProfilerDriver.enabled)
            {
                first = 1 + ProfilerDriver.firstFrameIndex;
                last = 1 + ProfilerDriver.lastFrameIndex;
                // Clip to the visible frames in the profile which indents 1 in from start and end
                if (first < last)
                    last--;
                if (first < last)
                    first++;
                return true;
            }

            first = 1;
            last = 1;
            return false;
        }

        public void CloseProfiler()
        {
            if (m_ProfilerWindow)
                m_ProfilerWindow.Close();
        }

        public string GetProfilerWindowMarkerName()
        {
            var timeLineGUI = m_TimeLineGUIFieldInfo.GetValue(m_ProfilerWindow);
            if (timeLineGUI != null && m_SelectedEntryFieldInfo != null)
            {
                var selectedEntry = m_SelectedEntryFieldInfo.GetValue(timeLineGUI);
                if (selectedEntry != null && m_SelectedNameFieldInfo != null)
                {
                    return m_SelectedNameFieldInfo.GetValue(selectedEntry).ToString();
                }
            }

            return null;
        }

        public float GetFrameTime(int frameIndex)
        {
            ProfilerFrameDataIterator frameData = new ProfilerFrameDataIterator();

            frameData.SetRoot(frameIndex, 0);
            float ms = frameData.frameTimeMS;
            frameData.Dispose();

            return ms;
        }

        private bool GetMarkerInfo(string markerName, int frameIndex, string threadFilter, out int outThreadIndex, out float time, out float duration, out int instanceId)
        {
            ProfilerFrameDataIterator frameData = new ProfilerFrameDataIterator();

            outThreadIndex = 0;
            time = 0.0f;
            duration = 0.0f;
            instanceId = 0;
            bool found = false;

            int threadCount = frameData.GetThreadCount(frameIndex);
            Dictionary<string, int> threadNameCount = new Dictionary<string, int>();
            for (int threadIndex = 0; threadIndex < threadCount; ++threadIndex)
            {
                frameData.SetRoot(frameIndex, threadIndex);

                var threadName = frameData.GetThreadName();
                if (!threadNameCount.ContainsKey(threadName))
                    threadNameCount.Add(threadName, 1);
                else
                    threadNameCount[threadName] += 1;
                var threadNameWithIndex = ProfileData.ThreadNameWithIndex(threadNameCount[threadName], threadName);

                if (threadFilter == "All" || threadNameWithIndex == threadFilter)
                {
                    const bool enterChildren = true;
                    while (frameData.Next(enterChildren))
                    {
                        if (frameData.name == markerName)
                        {
                            time = frameData.startTimeMS;
                            duration = frameData.durationMS;
                            instanceId = frameData.instanceId;
                            outThreadIndex = threadIndex;
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                    break;
            }

            frameData.Dispose();
            return found;
        }

        public void SetProfilerWindowMarkerName(string markerName, string threadFilter)
        {
            if (m_ProfilerWindow == null)
                return;
            
            var timeLineGUI = m_TimeLineGUIFieldInfo.GetValue(m_ProfilerWindow);
            if (timeLineGUI != null && m_SelectedEntryFieldInfo != null)
            {
                var selectedEntry = m_SelectedEntryFieldInfo.GetValue(timeLineGUI);
                if (selectedEntry != null)
                {
                    // Read profiler data direct from profile to find time/duration
                    int currentFrameIndex = (int)m_CurrentFrameFieldInfo.GetValue(m_ProfilerWindow);
                    float time;
                    float duration;
                    int instanceId;
                    int threadIndex;
                    if (GetMarkerInfo(markerName, currentFrameIndex, threadFilter, out threadIndex, out time, out duration, out instanceId))
                    {
                        /*
                        Debug.Log(string.Format("Setting profiler to {0} on {1} at frame {2} at {3}ms for {4}ms ({5})", 
                                                markerName, currentFrameIndex, threadFilter, time, duration, instanceId));
                         */
                        
                        if (m_SelectedNameFieldInfo != null)
                            m_SelectedNameFieldInfo.SetValue(selectedEntry, markerName);
                        if (m_SelectedTimeFieldInfo != null)
                            m_SelectedTimeFieldInfo.SetValue(selectedEntry, time);
                        if (m_SelectedDurationFieldInfo != null)
                            m_SelectedDurationFieldInfo.SetValue(selectedEntry, duration);
                        if (m_SelectedInstanceIdFieldInfo != null)
                            m_SelectedInstanceIdFieldInfo.SetValue(selectedEntry, instanceId);
                        if (m_SelectedFrameIdFieldInfo != null)
                            m_SelectedFrameIdFieldInfo.SetValue(selectedEntry, currentFrameIndex);
                        if (m_SelectedThreadIdFieldInfo != null)
                            m_SelectedThreadIdFieldInfo.SetValue(selectedEntry, threadIndex);
                        
                        // TODO : Update to fill in the total and number of instances.
                        // For now we force Instance count to 1 to avoid the incorrect info showing.
                        if (m_SelectedInstanceCountFieldInfo != null)
                            m_SelectedInstanceCountFieldInfo.SetValue(selectedEntry, 1);

                        // Set other values to non negative values so selection appears
                        if (m_SelectedNativeIndexFieldInfo != null)
                            m_SelectedNativeIndexFieldInfo.SetValue(selectedEntry, currentFrameIndex);

                        m_ProfilerWindow.Repaint();
                    }
                }
            }
        }

        public bool JumpToFrame(int index)
        {
            //if (!ProfilerDriver.enabled)
            //    return;

            if (!m_ProfilerWindow)
                return false;
            
            m_CurrentFrameFieldInfo.SetValue(m_ProfilerWindow, index - 1);
            m_ProfilerWindow.Repaint();
            return true;
        }
    }
}
