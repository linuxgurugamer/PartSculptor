# Manual editor checks — 0.0.5.17

- Select an individual vertex. Enable Soft selection. Type `0.123456`; the field must retain at most three fractional digits (`0.123`). Delete the text and type `0.004` normally. Try a negative number, letters and a second decimal point; invalid characters must not enter the field.
- Drag the radius slider at several positions; the text must show at most three decimals. Enter a value larger than the current slider maximum; the slider must expand. Apply and reselect the vertex; the displayed radius must agree. Zero and disabled soft selection still apply hard selection. Existing craft radii are not modified until Apply.
- Open Settings. Drag from the title, labels, empty areas and spaces between controls. The window should move while buttons, sliders and the scrolling controls still respond normally. Confirm it remains clamped inside the screen.
- In PG Vertices mode, locate the collision fallback checkbox below the radius controls. Toggle it without selecting a vertex, then select a vertex and retry. Verify the effective value changes without restarting and is saved to UserSettings.cfg, without changing any unrelated user settings or active editor flags.
- Open Settings while PG is active. Change the PG checkbox; the Settings checkbox should follow. Save or Apply in Settings and confirm PG reflects the effective setting. If Cancel is used after a PG change during the open Settings session, the opening snapshot (including the saved file) is restored. Close and reopen to verify persistence.
- On a craft with generated collision geometry, leave fallback enabled. The toggle affects later geometry operations; it does not force an immediate rebuild.

The offline regression suite checks source integration only; the mouse interaction and persistence checks above require KSP.
