# Export data

Export Profile Analyzer marker tables and frame timing data to CSV files for use in spreadsheets and other tools.

You can export data from the **Single** or **Compare** view after you load profiling data into the Profile Analyzer. Each export type writes a separate CSV file. Profile Analyzer assigns a default filename for each export type. For file format rules, export type summaries, and column definitions, refer to [Export data reference](export-data-reference.md).

## Prerequisites

Before you export data, load profiling data into the Profile Analyzer. For instructions, refer to [Collecting and viewing data workflow](collecting-and-viewing-data.md).

## Export Profile Analyzer data to CSV

To export Profile Analyzer data to a CSV file:

1. In the [Profile Analyzer window](profile-analyzer-window.md) toolbar, select **Export**.
2. In the **Export** window, choose an export type. Profile Analyzer disables an option when the relevant view has no valid data loaded. For a summary of each export type, refer to [Export types](export-data-reference.md#export-types) in **Export data reference**.
3. Save the file. The save dialog shows the default filename for the export type you chose. You can change the name and location before you save.

After you save the file, open it in a spreadsheet application or other tool that reads CSV. If an export option is unavailable, confirm that the correct view (**Single** or **Compare**) has valid data loaded, then try again. For column names and meanings, refer to [Export data reference](export-data-reference.md).

## Additional resources

- [Export data reference](export-data-reference.md)
- [Profile Analyzer window](profile-analyzer-window.md)
- [Single view](single-view.md)
- [Compare view](compare-view.md)
- [Collecting and viewing data workflow](collecting-and-viewing-data.md)
- [Unity Profiler](https://docs.unity3d.com/Manual/Profiler.html)