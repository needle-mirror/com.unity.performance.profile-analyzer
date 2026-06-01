# Statistics reference

Explore the statistics Profile Analyzer reports for frame, thread, and marker summaries.

Profile Analyzer displays **Min**, **Max**, **Median**, **Mean**, lower and upper quartile, and interquartile range values in the [Frame Summary](frame-summary.md), [Thread Summary](thread-summary.md), and [Marker Summary pane reference](marker-summary.md). The same statistics apply to CPU frame and marker times and to GC allocation bytes.

## Available statistics

| **Statistic** | **Description** |
| --- | --- |
| **Min** | The lowest (minimum) value for the marker or frame time. |
| **Max** | The largest (maximum) value for the marker or frame time. |
| **Median** | The middle value in a data set. It separates the higher half from the lower half. Refer to [median](https://en.wikipedia.org/wiki/Median) for the general definition. |
| **Mean** | The average value in a data set: the sum of all values divided by the number of values. Refer to [mean](https://en.wikipedia.org/wiki/Arithmetic_mean) for the general definition. |
| **Lower and upper quartiles** | The lower [quartile](https://en.wikipedia.org/wiki/Quartile) is the middle value between the smallest value and the median. The upper quartile is the middle value between the median and the largest value. |
| **Interquartile range** | The range of values in the central 50% of the data. Profile Analyzer calculates it as the difference between the upper and lower quartile values. Refer to [interquartile range](https://en.wikipedia.org/wiki/Interquartile_range) for the general definition. |

## How Profile Analyzer displays statistics

Profile Analyzer shows statistics as numbers in the summary panes. It also draws [histograms](https://en.wikipedia.org/wiki/Histogram) and [box and whisker plots](https://en.wikipedia.org/wiki/Box_plot) so you can see how values are distributed across the selected frame range.

The following tables describe common distribution shapes you might see in Single view and Compare view.

## Distribution examples in Single view

| **Distribution** | **Description** |
| --- | --- |
| **Even distribution** | The histogram shows many buckets hit at a similar rate. The box and whisker plot has a large box near the middle of the range. In this example, marker calls range from 16.75 ms to 17.26 ms. |
| **Outlier distribution** | The histogram shows most values in the lower buckets and only a few expensive buckets. The box sits toward the bottom of the range and the upper whisker extends higher. In this example, marker calls range from 0.67 ms to 5.32 ms. |

## Distribution examples in Compare view

| **Distribution** | **Description** |
| --- | --- |
| **Similar distribution** | The left and right data sets follow a similar pattern in both the histogram and the box and whisker plot. Marker activity is comparable across both sets. |
| **Different distribution** | The left (blue) data set uses more expensive buckets than the right set. The box and whisker plot for the left set sits higher in its range. In this example, the marker in the left data set ran longer and warrants further investigation. |
| **Overlapping distributions** | Both data sets share a similar lower bound and overlap in the middle of the range. The right (orange) data set also uses some higher buckets and has a higher upper bound. In this example, activity in the right data set is more costly or occurs more often and warrants further investigation. |

## Additional resources

* [Reference](reference.md)
* [Profile Analyzer window](profile-analyzer-window.md)
* [Frame Summary](frame-summary.md)
* [Thread Summary](thread-summary.md)
* [Marker Summary pane reference](marker-summary.md)
* [Single view](single-view.md)
* [Compare view](compare-view.md)
