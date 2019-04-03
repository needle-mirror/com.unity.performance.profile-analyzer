using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Performance.ProfileAnalyzer
{
    class ComparisonTreeViewItem : TreeViewItem
    {
        public MarkerPairing data { get; set; }

        public ComparisonTreeViewItem(int id, int depth, string displayName, MarkerPairing data) : base(id, depth, displayName)
        {
            this.data = data;
        }
    }

    class ComparisonTable : TreeView
    {
        ProfileAnalysis m_Left;
        ProfileAnalysis m_Right;
        List<MarkerPairing> m_Pairings;
        ProfileAnalyzerWindow m_ProfileAnalyzerWindow;
        float m_MinDiff;
        float m_MaxDiff;

        const float kRowHeights = 20f;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        // All columns
        public enum MyColumns
        {
            Name,
            LeftMedian,
            Left,
            Right,
            RightMedian,
            Diff,
            AbsDiff,
            LeftCount,
            RightCount,
            CountDiff,
        }

        public enum SortOption
        {
            Name,
            LeftMedian,
            RightMedian,
            Diff,
            AbsDiff,
            LeftCount,
            RightCount,
            CountDiff,
        }

        // Sort options per column
        SortOption[] m_SortOptions =
        {
            SortOption.Name,
            SortOption.LeftMedian,
            SortOption.Diff,
            SortOption.Diff,
            SortOption.RightMedian,
            SortOption.Diff,
            SortOption.AbsDiff,
            SortOption.LeftCount,
            SortOption.RightCount,
            SortOption.CountDiff,
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

        public ComparisonTable(TreeViewState state, MultiColumnHeader multicolumnHeader, ProfileAnalysis left, ProfileAnalysis right, List<MarkerPairing> pairings, ProfileAnalyzerWindow profileAnalyzerWindow) : base(state, multicolumnHeader)
        {
            m_Left = left;
            m_Right = right;
            m_Pairings = pairings;
            m_ProfileAnalyzerWindow = profileAnalyzerWindow;

            Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(MyColumns)).Length, "Ensure number of sort options are in sync with number of MyColumns enum values");

            // Custom setup
            rowHeight = kRowHeights;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            // extraSpaceBeforeIconAndLabel = 0;
            multicolumnHeader.sortingChanged += OnSortingChanged;
            multiColumnHeader.visibleColumnsChanged += OnVisibleColumnsChanged;

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            int idForhiddenRoot = -1;
            int depthForHiddenRoot = -1;
            ProfileTreeViewItem root = new ProfileTreeViewItem(idForhiddenRoot, depthForHiddenRoot, "root", null);

            List<string> nameFilters = m_ProfileAnalyzerWindow.GetNameFilters();
            List<string> nameExcludes = m_ProfileAnalyzerWindow.GetNameExcludes();

            m_MinDiff = float.MaxValue;
            m_MaxDiff = 0.0f;
            for (int index = 0; index < m_Pairings.Count; ++index)
            {
                var pairing = m_Pairings[index];
                if (nameFilters.Count > 0)
                {
                    if (!m_ProfileAnalyzerWindow.NameInFilterList(pairing.name, nameFilters))
                        continue;
                }
                if (nameExcludes.Count > 0)
                {
                    if (m_ProfileAnalyzerWindow.NameInExcludeList(pairing.name, nameExcludes))
                        continue;
                }

                var item = new ComparisonTreeViewItem(index, 0, pairing.name, pairing);
                root.AddChild(item);
                float diff = Diff(item);
                if (diff < m_MinDiff)
                    m_MinDiff = diff;
                if (diff > m_MaxDiff && diff<float.MaxValue)
                    m_MaxDiff = diff;
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            if (rootItem != null && rootItem.children != null)
            {
                foreach (ComparisonTreeViewItem node in rootItem.children)
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
            foreach (var node in rootItem.children)
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

            var myTypes = rootItem.children.Cast<ComparisonTreeViewItem>();
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
                    case SortOption.LeftMedian:
                        orderedQuery = orderedQuery.ThenBy(l => LeftMedian(l), ascending);
                        break;
                    case SortOption.RightMedian:
                        orderedQuery = orderedQuery.ThenBy(l => RightMedian(l), ascending);
                        break;
                    case SortOption.Diff:
                        orderedQuery = orderedQuery.ThenBy(l => Diff(l), ascending);
                        break;
                    case SortOption.AbsDiff:
                        orderedQuery = orderedQuery.ThenBy(l => AbsDiff(l), ascending);
                        break;
                    case SortOption.LeftCount:
                        orderedQuery = orderedQuery.ThenBy(l => LeftCount(l), ascending);
                        break;
                    case SortOption.RightCount:
                        orderedQuery = orderedQuery.ThenBy(l => RightCount(l), ascending);
                        break;
                    case SortOption.CountDiff:
                        orderedQuery = orderedQuery.ThenBy(l => CountDiff(l), ascending);
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        float LeftMedian(ComparisonTreeViewItem item)
        {
            if (item.data.leftIndex < 0)
                return 0.0f;
            
            List<MarkerData> markers = m_Left.GetMarkers();
            if (item.data.leftIndex >= markers.Count)
                return 0.0f;
                
            return markers[item.data.leftIndex].msMedian;
        }
        float RightMedian(ComparisonTreeViewItem item)
        {
            if (item.data.rightIndex < 0)
                return 0.0f;

            List<MarkerData> markers = m_Right.GetMarkers();
            if (item.data.rightIndex >= markers.Count)
                return 0.0f;

            return markers[item.data.rightIndex].msMedian;
        }
        float Diff(ComparisonTreeViewItem item)
        {
            return RightMedian(item) - LeftMedian(item);
        }
        float AbsDiff(ComparisonTreeViewItem item)
        {
            return Math.Abs(Diff(item));
        }
        float LeftCount(ComparisonTreeViewItem item)
        {
            if (item.data.leftIndex < 0)
                return 0.0f;

            List<MarkerData> markers = m_Left.GetMarkers();
            if (item.data.leftIndex >= markers.Count)
                return 0.0f;

            return markers[item.data.leftIndex].count;
        }
        float RightCount(ComparisonTreeViewItem item)
        {
            if (item.data.rightIndex < 0)
                return 0.0f;

            List<MarkerData> markers = m_Right.GetMarkers();
            if (item.data.rightIndex >= markers.Count)
                return 0.0f;

            return markers[item.data.rightIndex].count;
        }
        float CountDiff(ComparisonTreeViewItem item)
        {
            return RightCount(item) - LeftCount(item);
        }

        IOrderedEnumerable<ComparisonTreeViewItem> InitialOrder(IEnumerable<ComparisonTreeViewItem> myTypes, int[] history)
        {
            SortOption sortOption = m_SortOptions[history[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case SortOption.Name:
                    return myTypes.Order(l => l.data.name, ascending);
                case SortOption.LeftMedian:
                    return myTypes.Order(l => LeftMedian(l), ascending);
                case SortOption.RightMedian:
                    return myTypes.Order(l => RightMedian(l), ascending);
                case SortOption.Diff:
                    return myTypes.Order(l => Diff(l), ascending);
                case SortOption.AbsDiff:
                    return myTypes.Order(l => AbsDiff(l), ascending);
                case SortOption.LeftCount:
                    return myTypes.Order(l => LeftCount(l), ascending);
                case SortOption.RightCount:
                    return myTypes.Order(l => RightCount(l), ascending);
                case SortOption.CountDiff:
                    return myTypes.Order(l => CountDiff(l), ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            // default
            return myTypes.Order(l => l.data.name, ascending);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (ComparisonTreeViewItem)args.item;

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

        void CellGUI(Rect cellRect, ComparisonTreeViewItem item, MyColumns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            // Find largest of min/max and use that range and both the -ve and +ve extends for the bar graphs.
            float min = Math.Abs(m_MinDiff);
            float max = Math.Abs(m_MaxDiff);
            float range = Math.Max(min, max);

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
                case MyColumns.LeftMedian:
                    if (item.data.leftIndex<0)
                        EditorGUI.LabelField(cellRect, "-");
                    else
                        EditorGUI.LabelField(cellRect, ToDisplayUnitsWithTooltips(LeftMedian(item)));
                    break;
                case MyColumns.Left:
                    {
                        float diff = Diff(item);
                        if (diff < 0.0f)
                        {
                            if (m_ProfileAnalyzerWindow.m_2D.DrawStart(cellRect))
                            {
                                float w = cellRect.width * -diff / range;
                                m_ProfileAnalyzerWindow.m_2D.DrawFilledBox(cellRect.width - w, 1, w, cellRect.height - 1, m_ProfileAnalyzerWindow.m_ColorLeft);
                                m_ProfileAnalyzerWindow.m_2D.DrawEnd();
                            }
                        }
                        GUI.Label(cellRect, new GUIContent("", ToDisplayUnits(Diff(item), true)));
                    }
                    break;
                case MyColumns.Diff:
                    EditorGUI.LabelField(cellRect, ToDisplayUnitsWithTooltips(Diff(item)));
                    break;
                case MyColumns.Right:
                    {
                        float diff = Diff(item);
                        if (diff > 0.0f)
                        {
                            if (m_ProfileAnalyzerWindow.m_2D.DrawStart(cellRect))
                            {
                                float w = cellRect.width * diff / range;
                                m_ProfileAnalyzerWindow.m_2D.DrawFilledBox(0, 1, w, cellRect.height - 1, m_ProfileAnalyzerWindow.m_ColorRight);
                                m_ProfileAnalyzerWindow.m_2D.DrawEnd();
                            }
                        }
                        GUI.Label(cellRect, new GUIContent("", ToDisplayUnits(Diff(item),true)));
                    }
                    break;
                case MyColumns.RightMedian:
                    if (item.data.rightIndex < 0)
                        EditorGUI.LabelField(cellRect, "-");
                    else
                        EditorGUI.LabelField(cellRect, ToDisplayUnitsWithTooltips(RightMedian(item)));
                    break;
                case MyColumns.AbsDiff:
                    EditorGUI.LabelField(cellRect, ToDisplayUnitsWithTooltips(AbsDiff(item)));
                    break;
                case MyColumns.LeftCount:
                    if (item.data.leftIndex < 0)
                        EditorGUI.LabelField(cellRect, "-");
                    else
                        EditorGUI.LabelField(cellRect, string.Format("{0}", LeftCount(item)));
                    break;
                case MyColumns.RightCount:
                    if (item.data.rightIndex < 0)
                        EditorGUI.LabelField(cellRect, "-");
                    else
                        EditorGUI.LabelField(cellRect, string.Format("{0}", RightCount(item)));
                    break;
                case MyColumns.CountDiff:
                    if (item.data.leftIndex < 0 && item.data.rightIndex < 0)
                        EditorGUI.LabelField(cellRect, "-");
                    else
                        EditorGUI.LabelField(cellRect, string.Format("{0}", CountDiff(item)));
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

            public HeaderData(string name, string tooltip = "", float _width = 50, float _minWidth=30, bool _autoResize = true, bool _allowToggleVisibility = true)
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
                new HeaderData("Left", "Left median times"),
                new HeaderData("<", "Difference if left data set is a larger value", 50), 
                new HeaderData(">", "Difference if right data set is a larger value", 50), 
                new HeaderData("Right", "Right median time"), 
                new HeaderData("Diff", "Difference between left and right times"), 
                new HeaderData("Abs Diff", "Absolute difference between left and right times"), 
                new HeaderData("L Count", "Left marker count over all frames\n\nMultiple can occur per frame"), 
                new HeaderData("R Count", "Right marker count over all frames\n\nMultiple can occur per frame"), 
                new HeaderData("D Count", "Difference in marker count")
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

            if (selectedIds.Count > 0)
                m_ProfileAnalyzerWindow.SelectPairing(selectedIds[0]);
        }

        private static void SetMode(Mode mode, MultiColumnHeaderState state)
        {
            switch (mode)
            {
                case Mode.All:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.LeftMedian,
                        (int)MyColumns.Left,
                        (int)MyColumns.Right,
                        (int)MyColumns.RightMedian,
                        (int)MyColumns.Diff,
                        (int)MyColumns.AbsDiff,
                        (int)MyColumns.LeftCount,
                        (int)MyColumns.RightCount,
                        (int)MyColumns.CountDiff,
                    };
                    break;
                case Mode.Time:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.LeftMedian,
                        (int)MyColumns.Left,
                        (int)MyColumns.Right,
                        (int)MyColumns.RightMedian,
                        //(int)MyColumns.Diff,
                        (int)MyColumns.AbsDiff,
                    };
                    break;
                case Mode.Count:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.LeftCount,
                        (int)MyColumns.RightCount,
                        (int)MyColumns.CountDiff,
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
}
