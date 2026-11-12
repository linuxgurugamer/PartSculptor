# Manual KSP editor test: two FL-T400s, mirror-copy corner alignment — v0.0.4.116

No .NET runtime/offline test can prove the real Unity node/mesh update order. Verify this *in the VAB*.

1. Place an FL-T400 as root. Stack an identical FL-T400 above it at the top node. Enable **Copy attached morph steps**; disable **Match attached faces** (Copy takes priority for live morphs if both are on).
2. Using **Morph**, Alt-drag the *right (+X) edge* of the LOWER tank's top face downward a small amount. The upper tank's touching face must get the **opposite local tilt** on that same visible edge; inspect both meshes: the four corresponding corners should meet, with no new wedge at either edge, while the stack nodes stay attached.
3. Drag upward through the original position and further up; ensure the opposite sign works equally. Return to zero tilt: no accumulating roll or part-position drift.
4. Repeat for left (-X), front/back (+/-Z), and a corner tilted in both X and Z simultaneously. Verify alignment after several intermediate drag frames, not just the final mouse release.
5. Reverse which part drives: tilt the UPPER tank's bottom connection; the LOWER tank's top end must mirror the incremental movement. The child must still follow the final parent node, not drift away.
6. Test a 90-, 180-, or 270-degree roll of the attached tank before morphing; the same visible side must be selected. Also test an already slightly tilted joint and drag cancel/Escape and Ctrl+Z.
7. Confirm the upper tank's far/top face controls did NOT change, no change is propagated to grandchildren, and an initially different/mismatched face is not silently overwritten. **Match attached faces** should continue to do its original absolute geometric fit on demand.

Technical behavior: v116 snapshots the four initial world corners of both attached end planes. When the two *shapes* started equivalent, it preserves each part's morph values from the incremental Copy step, updates both nodes and the child subtree, and uses the in-plane X/Z tangents to resolve any remaining rotation about the node normal. The check ignores a constant mesh-cap-to-node offset and preserves editor node offsets; if the post-correction end shapes cannot agree within the safety tolerance, the child's pose is rolled back to node-normal alignment.

When reporting a remaining failure, provide which tank is driver, which face was edited (top/bottom), the edge/handle and modifier used, direction of drag, and whether the attached tank was rolled in the editor.
