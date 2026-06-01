# Upgrade Profile Analyzer

Upgrade the Profile Analyzer package and confirm that saved `.pdata` analysis files remain compatible with your workflow.

When you install this release through the Package Manager, review the format changes for saved analysis files before you share `.pdata` files with other users or depend on older package versions. For new features in this release, refer to [What's new](whats-new.md). For the full change list, refer to the [Profile Analyzer package changelog](https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@latest/index.html?subfolder=/changelog/CHANGELOG.html).

## Prerequisites

Before you complete the tasks in this guide, update the `com.unity.performance.profile-analyzer` package in your project to the latest release through the [Package Manager](https://docs.unity3d.com/Manual/upm-ui.html).

## Review .pdata file format compatibility

This release bumps the saved analysis file format to **Version 9** so save and load persist the data required by the GC allocation panels.

To confirm `.pdata` file compatibility after you upgrade:

1. Open `.pdata` files that you saved with Profile Analyzer 1.3.4 or earlier (format Versions 7 and 8) in this release. They continue to load in this release.
2. Do not share `.pdata` files that you save with this release with users on Profile Analyzer 1.3.4 or earlier. Those versions cannot open format Version 9 files. To collaborate across versions, share the underlying Profiler `.data` capture instead, or ask collaborators to upgrade to this release.

After you confirm compatibility, pull or load captures again if needed and refer to [What's new](whats-new.md) for GC allocation panels and export changes.

## Additional resources

- [What's new](whats-new.md)
- [About the Profile Analyzer package](index.md)
- [Collecting and viewing data workflow](collecting-and-viewing-data.md)
- [Marker Summary reference](marker-summary.md)
- [Frame Summary](frame-summary.md)
- [Export window reference](export-data-reference.md)

