using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Performance.ProfileAnalyzer
{
    class ProfileTreeViewItem: TreeViewItem
    {
        public MarkerData data { get; set; }

        public ProfileTreeViewItem(int id, int depth, string displayName, MarkerData data) : base(id, depth, displayName)
        {
            this.data = data;
        }
    }

    class ProfileTable : TreeView
    {
        ProfileAnalysis m_Model;
        ProfileAnalyzerWindow m_ProfileAnalyzerWindow;
        float m_MaxMedian;

        const float kRowHeights = 20f;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        // All columns
        public enum MyColumns
        {
            Name,
            Depth,
            Median,
            MedianBar,
            Mean,
            Min,
            Max,
            Range,
            Count,
            CountMean,
            FirstFrame,
            AtMedian,
        }

        public enum SortOption
        {
            Name,
            Depth,
            Median,
            Mean,
            Min,
            Max,
            Range,
            Count,
            FirstFrame,
            AtMedian,
        }

        // Sort options per column
        SortOption[] m_SortOptions =
        {
            SortOption.Name,
            SortOption.Depth,
            SortOption.Median,
            SortOption.Median,
            SortOption.Mean,
            SortOption.Min,
            SortOption.Max,
            SortOption.Range,
            SortOption.Count,
            SortOption.Count,
            SortOption.FirstFrame,
            SortOption.AtMedian,
        };

        internal static class Styles
        {
            public static readonly GUIContent menuItemSelectFramesInAll = new GUIContent("Select Frames that contain this marker (within whole data set)", "");
            public static readonly GUIContent menuItemSelectFramesInCurrent = new GUIContent("Select Frames that contain this marker (within current selection)", "");
            public static readonly GUIContent menuItemSelectFramesAll = new GUIContent("Clear Selection", "");
            public static readonly GUIContent menuItemAddToIncludeFilter = new GUIContent("Add to Include Filter", "");
            public static readonly GUIContent menuItemAddToExcludeFilter = new GUIContent("Add to Exclude Filter", "");
            public static readonly GUIContent menuItemRemoveFromIncludeFilter = new GUIContent("Remove from Include Filter", "");
            public static readonly GUIContent menuItemRemoveFromExcludeFilter = new GUIContent("Remove from Exclude Filter", "");
        }

        public ProfileTable(TreeViewState state, MultiColumnHeader multicolumnHeader, ProfileAnalysis model, ProfileAnalyzerWindow profileAnalyzerWindow) : base(state, multicolumnHeader)
        {
            m_Model = model;
            m_ProfileAnalyzerWindow = profileAnalyzerWindow;

            Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(MyColumns)).Length, "Ensure number of sort options are in sync with number of MyColumns enum values");

            // Custom setup
            rowHeight = kRowHeights;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            // extraSpaceBeforeIconAndLabel = 0;
            multicolumnHeader.sortingChanged += OnSortingChanged;
            multicolumnHeader.visibleColumnsChanged += OnVisibleColumnsChanged;

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            int idForhiddenRoot = -1;
            int depthForHiddenRoot = -1;
            ProfileTreeViewItem root = new ProfileTreeViewItem(idForhiddenRoot, depthForHiddenRoot, "root", null);

            List<string> nameFilters = m_ProfileAnalyzerWindow.GetNameFilters();
            List<string> nameExcludes = m_ProfileAnalyzerWindow.GetNameExcludes();

            m_MaxMedian = 0.0f;
            var markers = m_Model.GetMarkers();
            for (int index = 0; index < markers.Count; ++index)
            {
                var marker = markers[index];
                if (nameFilters.Count > 0)
                {
                    if (!m_ProfileAnalyzerWindow.NameInFilterList(marker.name, nameFilters))
                        continue;
                }
                if (nameExcludes.Count > 0)
                {
                    if (m_ProfileAnalyzerWindow.NameInExcludeList(marker.name, nameExcludes))
                        continue;
                }

                var item = new ProfileTreeViewItem(index, 0, marker.name, marker);
                root.AddChild(item);
                float ms = item.data.msMedian;
                if (ms > m_MaxMedian)
                    m_MaxMedian = ms;
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            if (rootItem!=null && rootItem.children!=null)
            {
                foreach (ProfileTreeViewItem node in rootItem.children)
                {
                    m_Rows.Add(node);
                }
            }

            SortIfNeeded(m_Rows);

            return m_Rows;
        }

        void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            SortIfNeeded(GetRows());
        }

        protected virtual void OnVisibleColumnsChanged(MultiColumnHeader multiColumnHeader)
        {
            m_ProfileAnalyzerWindow.SetMode(Mode.Custom);
        }

        void SortIfNeeded(IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
            {
                return;
            }

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return; // No column to sort for (just use the order the data are in)
            }

            // Sort the roots of the existing tree items
            SortByMultipleColumns();

            // Update the data with the sorted content
            rows.Clear();
            foreach (ProfileTreeViewItem node in rootItem.children)
            {
                rows.Add(node);
            }

            Repaint();
        }

        void SortByMultipleColumns()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
            {
                return;
            }

            var myTypes = rootItem.children.Cast<ProfileTreeViewItem>();
            var orderedQuery = InitialOrder(myTypes, sortedColumns);
            for (int i = 1; i < sortedColumns.Length; i++)
            {
                SortOption sortOption = m_SortOptions[sortedColumns[i]];
                bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

                switch (sortOption)
                {
                    case SortOption.Name:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.name, ascending);
                        break;
                    case SortOption.Depth:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.minDepth, ascending);
                        break;
                    case SortOption.Mean:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.msMean, ascending);
                        break;
                    case SortOption.Median:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.msMedian, ascending);
                        break;
                    case SortOption.Min:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.msMin, ascending);
                        break;
                    case SortOption.Max:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.msMax, ascending);
                        break;
                    case SortOption.Range:
                        orderedQuery = orderedQuery.ThenBy(l => (l.data.msMax - l.data.msMin), ascending);
                        break;
                    case SortOption.Count:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.count, ascending);
                        break;
                    case SortOption.FirstFrame:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.firstFrameIndex, ascending);
                        break;
                    case SortOption.AtMedian:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.msAtMedian, ascending);
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<ProfileTreeViewItem> InitialOrder(IEnumerable<ProfileTreeViewItem> myTypes, int[] history)
        {
            SortOption sortOption = m_SortOptions[history[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case SortOption.Name:
                    return myTypes.Order(l => l.data.name, ascending);
                case SortOption.Depth:
                    return myTypes.Order(l => l.data.minDepth, ascending);
                case SortOption.Mean:
                    return myTypes.Order(l => l.data.msMean, ascending);
                case SortOption.Median:
                    return myTypes.Order(l => l.data.msMedian, ascending);
                case SortOption.Min:
                    return myTypes.Order(l => l.data.msMin, ascending);
                case SortOption.Max:
                    return myTypes.Order(l => l.data.msMax, ascending);
                case SortOption.Range:
                    return myTypes.Order(l => (l.data.msMax - l.data.msMin), ascending);
                case SortOption.Count:
                    return myTypes.Order(l => l.data.count, ascending);
                case SortOption.FirstFrame:
                    return myTypes.Order(l => l.data.firstFrameIndex, ascending);
                case SortOption.AtMedian:
                    return myTypes.Order(l => l.data.msAtMedian, ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            // default
            return myTypes.Order(l => l.data.name, ascending);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (ProfileTreeViewItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (MyColumns)args.GetColumn(i), ref args);
            }
        }

        string ToDisplayUnits(float ms, bool showUnits = false)
        {
            return m_ProfileAnalyzerWindow.ToDisplayUnits(ms, showUnits, 0);
        }

        GUIContent ToDisplayUnitsWithTooltips(float ms, bool showUnits = false)
        {
            return new GUIContent(ToDisplayUnits(ms, showUnits), ToDisplayUnits(ms, true));
        }

        void ShowContextMenu(Rect cellRect, string markerName)
        {
            Event current = Event.current;
            if (cellRect.Contains(current.mousePosition) && current.type == EventType.ContextClick)
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(Styles.menuItemSelectFramesInAll, false, () => m_ProfileAnalyzerWindow.SelectFramesContainingMarker(markerName, false));
                menu.AddItem(Styles.menuItemSelectFramesInCurrent, false, () => m_ProfileAnalyzerWindow.SelectFramesContainingMarker(markerName, true));
                menu.AddItem(Styles.menuItemSelectFramesAll, false, () => m_ProfileAnalyzerWindow.SelectAllFrames());
                menu.AddSeparator("");
                if (!m_ProfileAnalyzerWindow.GetNameFilters().Contains(markerName))
                    menu.AddItem(Styles.menuItemAddToIncludeFilter, false, () => m_ProfileAnalyzerWindow.AddToIncludeFilter(markerName));
                else
                    menu.AddItem(Styles.menuItemRemoveFromIncludeFilter, false, () => m_ProfileAnalyzerWindow.RemoveFromIncludeFilter(markerName));
                if (!m_ProfileAnalyzerWindow.GetNameExcludes().Contains(markerName))
                    menu.AddItem(Styles.menuItemAddToExcludeFilter, false, () => m_ProfileAnalyzerWindow.AddToExcludeFilter(markerName));
                else
                    menu.AddItem(Styles.menuItemRemoveFromExcludeFilter, false, () => m_ProfileAnalyzerWindow.RemoveFromExcludeFilter(markerName));

                menu.ShowAsContext();

                current.Use();
            }
        }

        void CellGUI(Rect cellRect, ProfileTreeViewItem item, MyColumns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            ShowContextMenu(cellRect, item.data.name);

            switch (column)
            {
                case MyColumns.Name:
                    {
                        args.rowRect = cellRect;
                        //base.RowGUI(args);
                        EditorGUI.LabelField(cellRect, new GUIContent(item.data.name, item.data.name));
                    }
                    break;

                case MyColumns.Mean:
                    EditorGUI.LabelField(cellRect, ToDisplayUnitsWithTooltips(item.data.msMean, false));
                    break;
                case MyColumns.Depth:
                    if (item.data.minDepth == item.data.maxDepth)
                        EditorGUI.LabelField(cellRect, string.Format("{0}", item.data.minDepth));
                    else 
                        EditorGUI.LabelField(cellRect, string.Format("{0}-{1}", item.data.minDepth, item.data.maxDepth));
                    break;
                case MyColumns.Median:
                    EditorGUI.LabelField(cellRect, ToDisplayUnitsWithTooltips(item.data.msMedian));
                    break;
                case MyColumns.MedianBar:
                    {
                        float ms = item.data.msMedian;
                        if (ms > 0.0f)
                        {
                            if (m_ProfileAnalyzerWindow.m_2D.DrawStart(cellRect))
                            {
                                float w = cellRect.width * ms / m_MaxMedian;
                                m_ProfileAnalyzerWindow.m_2D.DrawFilledBox(0, 1, w, cellRect.height - 1, m_ProfileAnalyzerWindow.m_ColorBar);
                                m_ProfileAnalyzerWindow.m_2D.DrawEnd();
                            }
                        }
                        GUI.Label(cellRect, new GUIContent("", ToDisplayUnits(item.data.msMedian,true)));
                    }
                    break;
                case MyColumns.Min:
                    EditorGUI.LabelField(cellRect, ToDisplayUnitsWithTooltips(item.data.msMin));
                    break;
                case MyColumns.Max:
                    EditorGUI.LabelField(cellRect, ToDisplayUnitsWithTooltips(item.data.msMax));
                    break;
                case MyColumns.Range:
                    EditorGUI.LabelField(cellRect, ToDisplayUnitsWithTooltips(item.data.msMax - item.data.msMin));
                    break;
                case MyColumns.Count:
                    EditorGUI.LabelField(cellRect, string.Format("{0}", item.data.count));
                    break;
                case MyColumns.CountMean:
                    EditorGUI.LabelField(cellRect, string.Format("{0}", item.data.count / m_Model.GetFrameSummary().count));
                    break;
                case MyColumns.FirstFrame:
                    if (!m_ProfileAnalyzerWindow.IsProfilerWindowOpen())
                        GUI.enabled = false;
                    if (GUI.Button(cellRect, new GUIContent(item.data.firstFrameIndex.ToString())))
                    {
                        m_ProfileAnalyzerWindow.SelectMarker(item.id);
                        m_ProfileAnalyzerWindow.JumpToFrame(item.data.firstFrameIndex);
                    }

                    GUI.enabled = true;
                    break;
                case MyColumns.AtMedian:
                    EditorGUI.LabelField(cellRect, ToDisplayUnitsWithTooltips(item.data.msAtMedian));
                    break;
            }
        }


        // Misc
        //--------

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        struct HeaderData
        {
            public GUIContent content;
            public float width;
            public float minWidth;
            public bool autoResize;
            public bool allowToggleVisibility;

            public HeaderData(string name, string tooltip = "", float _width = 50, float _minWidth = 30, bool _autoResize = true, bool _allowToggleVisibility = true)
            {
                content = new GUIContent(name, tooltip);
                width = _width;
                minWidth = _minWidth;
                autoResize = _autoResize;
                allowToggleVisibility = _allowToggleVisibility;
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columnList = new List<MultiColumnHeaderState.Column>();
            HeaderData[] headerData = new HeaderData[]
            {
                new HeaderData("Name", "Marker Name\n\nFrame marker time is total of all instances in frame", 300, 100, false, false),
                new HeaderData("Depth", "Marker depth in marker hierarchy\n\nMay appear at multiple levels"),
                new HeaderData("Median", "Central marker time over all frames\n\nAlways present in data set\n1st of 2 central values for even frame count"),
                new HeaderData("Median", "Central marker time over all frames", 50),
                new HeaderData("Mean", "Per frame marker time / frame count"),
                new HeaderData("Min", "Minimum marker time"),
                new HeaderData("Max", "Maximum marker time"),
                new HeaderData("Range", "Difference between maximum and minimum"),
                new HeaderData("Count", "Marker count over all frames\n\nMultiple can occur per frame"),
                new HeaderData("Count Mean", "Average number of markers\n\ntotal count / number of frames",70,50),
                new HeaderData("1st", "First frame index that the marker appears on"),
                new HeaderData("At Median Frame", "Marker time on the median frame\n\nI.e. Marker total duration on the average frame",90,50),

            };
            foreach (var header in headerData)
            {
                columnList.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = header.content,
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = header.width,
                    minWidth = header.minWidth,
                    autoResize = header.autoResize,
                    allowToggleVisibility = header.allowToggleVisibility
                });
            };
            var columns = columnList.ToArray();

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(MyColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            SetMode(Mode.All, state);
            return state;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count>0)
                m_ProfileAnalyzerWindow.SelectMarker(selectedIds[0]);
        }

        private static void SetMode(Mode mode, MultiColumnHeaderState state)
        {
            switch (mode)
            {
                case Mode.All:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.Depth,
                        (int)MyColumns.Median,
                        (int)MyColumns.MedianBar,
                        (int)MyColumns.Mean,
                        (int)MyColumns.Min,
                        (int)MyColumns.Max,
                        (int)MyColumns.Range,
                        //(int)MyColumns.FirstFrame,
                        (int)MyColumns.Count,
                        (int)MyColumns.CountMean,
                        (int)MyColumns.AtMedian
                    };
                    break;
                case Mode.Time:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.Depth,
                        (int)MyColumns.Median,
                        (int)MyColumns.MedianBar,
                        //(int)MyColumns.Mean,
                        (int)MyColumns.Min,
                        (int)MyColumns.Max,
                        (int)MyColumns.Range,
                        //(int)MyColumns.FirstFrame,
                        (int)MyColumns.AtMedian
                    };
                    break;
                case Mode.Count:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.Depth,
                        (int)MyColumns.Count,
                        (int)MyColumns.CountMean,
                        //(int)MyColumns.FirstFrame,
                    };
                    break;
            }
        }

        public void SetMode(Mode mode)
        {
            SetMode(mode, multiColumnHeader.state);
            multiColumnHeader.ResizeToFit();
        }
    }

    static class MyExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }
            else
            {
                return source.OrderByDescending(selector);
            }
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }
            else
            {
                return source.ThenByDescending(selector);
            }
        }
    }
}
