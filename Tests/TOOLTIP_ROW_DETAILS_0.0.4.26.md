# Row tooltip regression tests — 0.0.4.26

1. Open the Vessel Inspector in the VAB with a scaled part selected.
2. Hover each of Property, Original, Current and Notes. Confirm every cell
   displays the same property name, Original, Current and Notes text.
3. Verify full-precision numeric values in the tooltip when the table shows
   shortened numbers; confirm Copy report still exports full precision.
4. Hover a row with empty Notes and another with a long FloatCurve. Confirm
   tooltip rendering, wrapping and scrolling are usable without GUI errors.
5. Repeat steps 2-4 in the independent PAW scaled-values window.
6. Switch rows, scroll the window and resize it. Confirm no IMGUI
   Layout/Repaint control-count exceptions are emitted to KSP.log.

Requires manual in-game verification.
