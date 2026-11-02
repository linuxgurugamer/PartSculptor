# Full-value tooltip regression test — 0.0.4.25

1. In the VAB, open the Vessel Inspector and select a stretched part.
2. Hover over the Original and Current numeric cells. Confirm the table
   remains short but the tooltip contains the full round-trip numeric value.
3. Hover over the Notes, Property and FloatCurve cells; check that none of the
   contents are missing or clipped by the right-hand pane.
4. Reduce the window to minimum size, hover a long curve entry, then move the
   pointer inside its tooltip and scroll to the end. The full data should remain
   readable (including the last curve key).
5. Repeat in the individual PAW Show scaled values window.
6. Check Copy report for full-precision numeric values.
7. Move the mouse quickly between rows, scroll the report, toggle Show unchanged,
   resize the inspector and move it partially offscreen. Check KSP.log for
   ArgumentException related to Layout/Repaint control counts.

Requires manual in-game verification.
