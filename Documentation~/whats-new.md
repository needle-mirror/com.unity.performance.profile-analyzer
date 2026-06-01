# What's new in Profile Analyzer

Analyze GC allocation per marker and per frame alongside CPU timing in this package update.

Discover new features and performance improvements in the latest update to Profile Analyzer.

For a full list of changes and updates in this version, refer to the [Profile Analyzer package changelog](https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@latest/index.html?subfolder=/changelog/CHANGELOG.html).

## GC allocation analysis for markers and frames

This release adds **GC allocation analysis** alongside existing CPU timing analysis. For each marker and frame in the selected range, Profile Analyzer reports allocation byte statistics (min, max, median, mean, lower and upper quartile, total) and allocation event counts.

The release adds the following UI and export updates:

- A **Frame GC Allocation Summary** panel below **Frame Summary** in Single view and Compare view. Refer to [Frame Summary](frame-summary.md).
- A **Marker GC Allocation Summary** panel below **Marker Summary**. Refer to [Marker Summary reference](marker-summary.md).
- Per-marker GC allocation columns in the marker table, including a **GC Allocations** preset in the **Marker columns** dropdown. Refer to [Single view](single-view.md) and [Compare view](compare-view.md).
- Additional columns in the marker, single-frame, and comparison-frame CSV exports. Refer to [Export window reference](export-data-reference.md).

Captures from Unity versions that predate the `GC.Alloc` profiler marker display **No GC allocation data in capture** in the new panels.

If you upgrade from Profile Analyzer 1.3.4 or earlier, refer to [Upgrade Profile Analyzer](upgrade-guide.md) for `.pdata` file format compatibility.

## Additional resources

- [Upgrade Profile Analyzer](upgrade-guide.md)
- [About the Profile Analyzer package](index.md)
- [Collecting and viewing data workflow](collecting-and-viewing-data.md)
- [Profile Analyzer window](profile-analyzer-window.md)
- [Export data](export-data.md)
- [Profile Analyzer package changelog](https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@latest/index.html?subfolder=/changelog/CHANGELOG.html)
