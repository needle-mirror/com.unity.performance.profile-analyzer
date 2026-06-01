# Export window reference

Explore the CSV export formats and column definitions for Profile Analyzer marker tables and frame timing data.

Use this reference to interpret exported files or integrate them with spreadsheets and other tools. To export files from the Profile Analyzer, refer to [Export data](export-data.md). For analysis workflows before export, refer to [Collecting and viewing data workflow](collecting-and-viewing-data.md), the [Single view](single-view.md), and the [Compare view](compare-view.md).

## Export types


| **Export**                 | **View**     | **Source**                                                                              |
| -------------------------- | ------------ | --------------------------------------------------------------------------------------- |
| **Marker table**           | Single view  | Per-marker statistics from the [Single view](single-view.md) marker table.              |
| **Single Frame Times**     | Single view  | Per-frame timing and GC allocation data for every frame in the active capture.          |
| **Comparison table**       | Compare view | Per-marker statistics from the [Compare view](compare-view.md) marker comparison table. |
| **Comparison Frame Times** | Compare view | Per-frame timing and GC allocation data for both data sets, aligned by frame offset.    |


## File format

All four files share the same conventions:

- The separator is `;` (semicolon). Marker names are wrapped in double quotation marks, and any embedded double quotes are replaced with single quotes when written.
- Time and byte values always use a period (`.`) as the decimal separator, regardless of locale. Integer values (counts, frame indices) contain no decimal separator.
- Frame indices use the same **Profiler** window remapping that the rest of the UI uses, so the values match the **First frame**, **Median frame**, and similar buttons in the Frame and Marker summaries.
- GC allocation columns require a capture that includes the `GC.Alloc` profiler marker. When the marker is absent, the byte and count columns are written as `0`.

## Marker table CSV

Each marker has its own row in the [Single view](single-view.md) marker table (after include/exclude filtering). The default filename is `markerTable.csv`.

Markers are written in descending order of median time (the default Single view sort).


| **Column**                           | **Description**                                                                                      |
| ------------------------------------ | ---------------------------------------------------------------------------------------------------- |
| `Name`                               | Marker name. Wrapped in double quotation marks.                                                      |
| `Median Time`                        | Median per-frame marker time, in milliseconds.                                                       |
| `Min Time`                           | Minimum per-frame marker time, in milliseconds.                                                      |
| `Max Time`                           | Maximum per-frame marker time, in milliseconds.                                                      |
| `Median Frame Index`                 | Frame index of the median time.                                                                      |
| `Min Frame Index`                    | Frame index of the minimum time.                                                                     |
| `Max Frame Index`                    | Frame index of the maximum time.                                                                     |
| `Min Depth`                          | Shallowest call depth at which the marker appears.                                                   |
| `Max Depth`                          | Deepest call depth at which the marker appears.                                                      |
| `Total Time`                         | Total marker time across all selected frames, in milliseconds.                                       |
| `Mean Time`                          | Mean per-frame marker time, in milliseconds.                                                         |
| `Time Lower Quartile`                | Lower-quartile per-frame marker time, in milliseconds.                                               |
| `Time Upper Quartile`                | Upper-quartile per-frame marker time, in milliseconds.                                               |
| `Count Total`                        | Total number of marker occurrences across all selected frames.                                       |
| `Count Median`                       | Median per-frame occurrence count.                                                                   |
| `Count Min`                          | Minimum per-frame occurrence count.                                                                  |
| `Count Max`                          | Maximum per-frame occurrence count.                                                                  |
| `Number of frames containing Marker` | Number of selected frames that contain at least one instance of the marker.                          |
| `First Frame Index`                  | First frame in the selected range that contains the marker.                                          |
| `Time Min Individual`                | Shortest single occurrence of the marker, in milliseconds.                                           |
| `Time Max Individual`                | Longest single occurrence of the marker, in milliseconds.                                            |
| `Min Individual Frame`               | Frame index containing the shortest occurrence.                                                      |
| `Max Individual Frame`               | Frame index containing the longest occurrence.                                                       |
| `Time at Median Frame`               | Marker time on the median frame, in milliseconds.                                                    |
| `GC Alloc Total (bytes)`             | Total GC allocation bytes attributed to this marker across all selected frames.                      |
| `GC Alloc Count`                     | Number of GC allocation events attributed to this marker across all selected frames.                 |
| `GC Alloc Mean (bytes)`              | Mean per-frame GC allocation bytes.                                                                  |
| `GC Alloc Median (bytes)`            | Median per-frame GC allocation bytes.                                                                |
| `GC Alloc Min (bytes)`               | Minimum per-frame GC allocation bytes. `0` when the marker has no allocations in any selected frame. |
| `GC Alloc Max (bytes)`               | Maximum per-frame GC allocation bytes. `0` when the marker has no allocations in any selected frame. |
| `GC Alloc Lower Quartile (bytes)`    | Lower-quartile per-frame GC allocation bytes.                                                        |
| `GC Alloc Upper Quartile (bytes)`    | Upper-quartile per-frame GC allocation bytes.                                                        |
| `GC Alloc Median Frame Index`        | Frame index of the median GC allocation value.                                                       |
| `GC Alloc Min Frame Index`           | Frame index of the minimum GC allocation value.                                                      |
| `GC Alloc Max Frame Index`           | Frame index of the maximum GC allocation value.                                                      |


## Single Frame Times CSV

Each row represents a frame in the active capture, not just the selected range. The default filename is `frameTime.csv`.


| **Column**                   | **Description**                                                                |
| ---------------------------- | ------------------------------------------------------------------------------ |
| `Frame Offset`               | Zero-based offset into the captured frame buffer.                              |
| `Frame Index`                | Profiler-window frame index for the same frame.                                |
| `Frame Time (ms)`            | Total frame time reported by the Profiler, in milliseconds.                    |
| `Time from first frame (ms)` | Cumulative elapsed time since the first frame in the capture, in milliseconds. |
| `GC Alloc (bytes)`           | Sum of `GC.Alloc` bytes across all sampled threads for the frame.              |


## Comparison table CSV

One row per marker that appears in either data set in the [Compare view](compare-view.md) marker comparison table. The default filename is `tableComparison.csv`.

The export writes **every non-visualization column** the comparison table defines, regardless of whether it is currently visible in the UI. Bar-style columns (`<`, `>`, `< Count`, `> Count`, `< Total`, `> Total`, `< Frame Count`, `> Frame Count`, `< GC Alloc Median`, `> GC Alloc Median`, `< GC Total`, `> GC Total`, `< GC Count`, `> GC Count`) are excluded because they are purely graphical.

For descriptions of every exported column, refer to the column reference in [Compare view](compare-view.md). The headers in the CSV match the in-UI column titles exactly.

## Comparison Frame Times CSV

One row per frame offset, with values from both data sets side by side. The longer capture's tail rows are populated with `0` for the shorter capture's columns. The default filename is `frameTimeComparison.csv`.


| **Column**                         | **Description**                                                         |
| ---------------------------------- | ----------------------------------------------------------------------- |
| `Frame Offset`                     | Zero-based offset into the longer of the two captures.                  |
| `Left Frame Index`                 | Profiler-window frame index for the left data set.                      |
| `Right Frame Index`                | Profiler-window frame index for the right data set.                     |
| `Left Frame Time (ms)`             | Left-side frame time, in milliseconds.                                  |
| `Left time from first frame (ms)`  | Cumulative elapsed time in the left data set, in milliseconds.          |
| `Right Frame Time (ms)`            | Right-side frame time, in milliseconds.                                 |
| `Right time from first frame (ms)` | Cumulative elapsed time in the right data set, in milliseconds.         |
| `Frame Time Diff (ms)`             | `Right Frame Time` minus `Left Frame Time`.                             |
| `Left GC Alloc (bytes)`            | Sum of `GC.Alloc` bytes across all sampled threads for the left frame.  |
| `Right GC Alloc (bytes)`           | Sum of `GC.Alloc` bytes across all sampled threads for the right frame. |
| `GC Alloc Diff (bytes)`            | `Right GC Alloc` minus `Left GC Alloc`.                                 |


## Additional resources

- [Export data](export-data.md)
- [Profile Analyzer window](profile-analyzer-window.md)
- [Single view](single-view.md)
- [Compare view](compare-view.md)
- [Collecting and viewing data workflow](collecting-and-viewing-data.md)
- [Unity Profiler](https://docs.unity3d.com/Manual/Profiler.html)