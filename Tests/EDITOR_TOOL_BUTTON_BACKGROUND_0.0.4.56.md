# KSP editor tool background — 0.0.4.56

Issue: the fifth Stretch and sixth Morph editor buttons had a white fill in the
transparent areas of their 38px icon PNGs, unlike the grey stock editor tools.

Root cause: `UnityEngine.UI.Image` draws a default white rectangle when its
sprite is unset. `EditorStretchTool` copies a stock sprite only when the native
button exposes an `Image` on the selectable itself; otherwise both new buttons
inherited opaque white. The idle reset path also set both background colors to
`Color.white`, recreating the problem even after UI refresh or tool switching.

Fix: tint both full-button background Images with a fixed KSP-like grey in
normal state, including after switching tools. Preserve the optional stock
button sprite/outline and the existing green/cyan selected-state tint. Load
the unmodified RGBA PNGs into `RawImage` children with white tint so the
images' actual alpha blends the glyph over the grey background.

Manual verification (VAB **and** SPH; game runtime not available offline):

1. Check the fifth Stretch and sixth Morph button backgrounds while neither
   tool is selected. Transparent areas of both icons should be grey, not white.
2. Activate Stretch, switch to Morph, then press a stock tool (1–4); the active
   button should be highlighted while the idle one stays grey. Press Esc to
   leave either mode; both backgrounds should return to grey.
3. Check hover/press feedback and tool clicks. Confirm icon transparency at
   different UI scales and after leaving/re-entering the editor.

Offline tests check the two background assignments, icon RGBA formats,
UI control wiring and version metadata; these do not replace in-game testing.
