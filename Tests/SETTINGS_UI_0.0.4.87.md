> Historical test plan: Cancel behavior changed in 0.0.4.89. For current behavior see `SETTINGS_CANCEL_OPENING_SNAPSHOT_0.0.4.89.md`.

# PartSculptor v0.0.4.87 — Settings/tooltip manual test

1. Open Vessel Inspector → Settings in the VAB. Ensure **Save / Apply / Cancel** remain visible at 1080p, 720p and increased KSP UI scaling.
2. Shrink the game window and reopen Settings. Confirm option content scrolls while the footer remains visible, and that dragging by the title bar remains possible.
3. Change a checkbox and choose **Cancel**: reopen and confirm no preference was written. Change again and choose **Apply**: window remains open, change persists across reopening. Choose **Save**: preference persists and window closes.
4. Set Morph tip opacity to 10%, Apply, and activate Morph. Check both the help panel under the editor toolbar and the drag tooltip: their backgrounds become faint, while text remains fully readable at normal opacity.
5. Repeat at 50% and 100%, with default and alternate UI skins. Verify buttons/other windows do not acquire unwanted transparency.
6. Test no changes to B9PartSwitch, MFT or RealFuels volume scaling; existing resource tests should remain green.
