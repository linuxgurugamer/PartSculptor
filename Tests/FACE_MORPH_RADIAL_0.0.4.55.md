# 0.0.4.55: Ctrl+Alt radial four-corner face morph

1. In the VAB, attach a stock FL-T400, activate the sixth Morph tool and select the tank.
2. With Ctrl+Alt pressed BEFORE the initial mouse click, drag a cyan top corner inward toward the face center. All four cyan corners should move inward simultaneously; the orange bottom face stays unchanged. Drag outward and all four move outward.
3. Try each corner on both faces. The result should be the same whichever corner is used for an equivalent radial drag. An orthogonal/tangential drag should not resize.
4. First resize to a rectangular face with different width and depth, then Ctrl+Alt drag. Check that both X and Z dimensions change by the same factor and the rectangle keeps its aspect ratio.
5. Shift+Alt alone remains translation of all four corners by the SAME offset. Ctrl+Alt scales their distance from the face center. Test with an already-offset or tilted face: Ctrl+Alt should not move its center or change its tilt.
6. Start Ctrl+Alt+Shift drag and test all four snap settings (0.01, 0.05, 0.10, 0.25). The shared scale factor, NOT individual dimensions, must snap. Release/re-press Shift during the drag to toggle snapping without switching modes.
7. With the face already at 0.25 or 4.0 in either axis, try growing/shrinking farther: both dimensions must stay within bounds and retain their ratio.
8. Esc or right-click during Ctrl+Alt drag restores the saved pre-drag geometry. Check undo/revert behavior, symmetry counterparts, craft save/reload, visual meshes, collider mesh and attachment nodes.

Note: Offline tests cannot establish KSP in-game behavior.
