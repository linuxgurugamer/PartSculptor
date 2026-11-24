# PartSculptor

**PartSculptor** is a part-scaling and face-morphing mod for **KSP 1.12.x**. In the VAB and SPH, you can stretch a part independently along its local X, Y, and Z axes, or reshape its two end faces using interactive handles. Your changes are saved with the craft and restored when it is loaded.

PartSculptor is more than a visual resize: it also updates supported attachment geometry, mass, cost, resource capacities, and other scale-dependent properties.

This started as a derivative of a mod uploaded to Spacedock called StretchScale (https://spacedock.info/mod/4718/StretchScale). Changes made via StretchScale do not affect parts' stats or aero models like TweakScale does, only changing the visible model and its collision.

The mod is currently in an alpha test with a few users. I do welcome more people who would be willing to test and provide feedback.

Forum page is located here:  https://forum.kerbalspaceprogram.com/topic/232076-112x-linuxgurugamers-part-sculptor/

## Features

- **Independent X/Y/Z scaling**, or **Lock proportions** to resize uniformly. Use numeric fields, built-in or custom presets, or the three-axis editor gizmo.
- **Procedural Geometry (seventh editor widget, PG):** add interior cross-section rings or individual vertices that actually split mesh triangles. Adjust each ring's Y height and X/Z multipliers; edit multiple rings, remove them, or reset the mesh. Craft saves store the profile parameters, not generated mesh data. This works with compatible readable static meshes and supported colliders, including B9 mesh alternatives. Interior ring edits keep end faces and attachment nodes in place; individual vertex edits can move affected attachment nodes. This is an experimental editor feature: see collision-mesh limits below.
- **Face morphing** with sixteen handles (four corners and four edge midpoints on each end face). Create tapered, tilted, offset, and radially expanded or contracted shapes. The PAW and Morph tool show each face's tilt in degrees (overall angle and signed X/Z components), updating while you drag.
- **Craft persistence and symmetry** for part scales and morph settings. Optional **Stretch children** also scales attached descendant parts.
- **Mass, cost, and resource capacity scaling** from the part's original values, so repeated edits never compound. Fuel fill levels are kept as capacities change.
- **Attachment handling** for stack and surface nodes: node size tiers, repositioning of attached parts, attach nodes that follow tilted morph faces, and optional automatic stack-node size matching.
- **Copy attached morph steps** additionally rotates and repositions the connected child through the editor rotation path after both touching faces morph. This retains mirrored local face changes and refreshes editor rotation events/gizmos. Actual edge-to-edge flushness still depends on compatible geometry.
- **Modular Fuel Tanks / RealFuels volumes:** `ModuleFuelTanks` tank volume follows stretched and morphed geometry. The optional adapter uses RealFuels 15.16.1 `ChangeTotalVolume()` with utilization-aware total volume; older MFT callback/message variants remain as fallbacks, and MFT/RealFuels retains responsibility for fuel allocation. Validate individual installations in the editor and flight.
- **Mod and part compatibility**: docking ports, B9 Part Switch, supported fuel-switcher modules, and engine visual effects (including optional Waterfall effects).
- **TweakScale coexistence**, including support for `TWEAKSCALEEXPONENTS`, `TWEAKSCALEBEHAVIOR`, and compatible part/module scaling rules. PartSculptor includes `Patches/ScaleExponents.cfg` with fallback `TWEAKSCALEEXPONENTS` and `TWEAKSCALEBEHAVIOR` definitions when TweakScale is not installed.
- **Vessel Inspector** with per-part reports of scaled values, change filtering, highlighting, and access to presets and settings in the editor. Inspection is also available in flight.
- **Career Mode scale limits** based on VAB/SPH upgrade level, plus configurable safety limits.
- **Crewed parts included by default**, with crew-capacity increases limited by the seats actually available in the part's IVA.

**Maximum face tilt:** Edit `maxFaceTilt = 2.5` in
`GameData/PartSculptor/PluginData/Settings.cfg` and restart KSP to change
the maximum magnitude of **each** tilt-axis slope. The new default is `2.5`;
set `0.75` to restore the original, more restrictive limit (valid values are
greater than 0 and at most 4.0). This is a dimensionless slope **not** a
degree limit: the PAW/Morph tool degree displays depend on the face width,
depth and part stretching, and two-axis tilt produces a combined angle.
The same setting applies to attached-face matching **and Copy attached
morph steps** (including the copied child face). Extreme tilts can
distort mesh geometry, colliders or attachment behavior.

**Diagnostic logging:** `debugLogChanges` in `GameData/PartSculptor/PluginData/Settings.cfg` controls `ScaleDebug` messages and the logger level selected at startup. Restart KSP after changing it. `ScaleDebug` entries use `Log.Debug`, and enabling diagnostics selects the `DETAIL` logger level so those messages are written.

**Troubleshooting screen messages:** Every PartSculptor message shown on screen is also logged to `KSP.log` as `[ScreenMessage]` with the exact message, code source, part title/name/instance, and its `GameData` asset path (`partUrl`) and exact config filename (`configFileFullName`) where available. These entries are written even when `debugLogChanges = false`, making it easier to identify the mod supplying a problematic part.

**B9 attachment reloads:** When a B9PartSwitch length subtype has been stretched, PartSculptor restores stack-connected parts to the saved attachment location after loading an editor craft. This includes late subtype node updates, and preserves intentional saved connection offsets. The correction is restricted to initial craft loading, not normal editor resizing.

## Requirements

- KSP **1.12.x**
- **ModuleManager**: a single patch adds `ModulePartSculptor` to eligible parts (including crewed parts), but skips parts containing `ProceduralPart` from the Procedural Parts mod and parts that already have `ModulePartSculptor`.
- **ClickThroughBlocker**, **ToolbarController** and **SpaceTuxLibrary**: used by the inspector and other windows.

**Optional integrations:** TweakScale, B9 Part Switch, supported fuel-switching mods, and Waterfall. None of these are needed for basic stretching or morphing.

## Installation

1. Install the required mods listed above.
2. Copy the `GameData/PartSculptor` folder from the release into your KSP `GameData` folder.
3. Start (or restart) KSP.

Restart KSP whenever you install, update, or change ModuleManager patches.

### Crewed parts

The single ModuleManager patch adds `ModulePartSculptor` to **eligible parts**, including command pods and cockpits, without requiring PatchManager or an opt-in step. It excludes Procedural Parts components (`ProceduralPart`) and parts that already contain `ModulePartSculptor`. Crew-capacity scaling remains limited by available IVA seats. EVA Kerbals (`KerbalEVA`, `kerbalEVA*`), planted flags (`FlagSite`, `flag`) and standalone flag panels (`FlagDecalBackground`, `flagPart*`) are excluded both by the patch and at runtime. Regular crewed parts and ordinary parts with flag decals remain eligible.

Test crewed parts carefully, especially IVAs, hatches, EVA, and crew assignment. 

**Morph tool on rotated parts:** After an attached part has been rotated by face morphing, its Morph handles use its current orientation when a new drag starts. During one drag, the starting frame stays fixed so automatic child rotations cannot change the control direction.

## Quick start: stretching a part

1. Open the **VAB** or **SPH** and place a part that has PartSculptor (for example, a stock fuel tank).
2. **Right-click** the part and expand **Stretch Scale** in its Part Action Window (PAW).
3. Adjust **Scale X (width)**, **Scale Y (height)**, and **Scale Z (depth)**. Turn on **Lock proportions** to resize all three axes together.
4. Or select the **Stretch** tool (the fifth editor tool, next to KSP's four stock tools), click a part, and drag the **red (X)**, **green (Y)**, or **blue (Z)** gizmo handle.
5. Use **Reset scale** to return to the original size, or **Swap X / Z** to exchange those two axes.

While dragging a Stretch handle:

- Hold **Shift** to snap in 0.05 steps.
- Hold **Alt** to temporarily lock X/Y/Z proportions.
- Hold **Shift+Alt** to lock all proportions and snap.
- Hold **Ctrl** to lock X/Z together while leaving Y unchanged (when dragging X or Z). Dragging Y still changes only Y.
- Hold **Shift+Ctrl** to lock X/Z together and snap.
- **Alt takes precedence over Ctrl** if both are held. These modifiers do not change the part's saved **Lock proportions** setting; Ctrl temporarily overrides it with X/Z-only locking.
- Press **Esc** or right-click to cancel the drag.

The Stretch tool displays a help panel below the editor tool buttons, similar to the Morph help panel, listing all modifier combinations.

To leave the Stretch tool, select another editor tool, press **Esc** (when not dragging), or click the Stretch tool again.

**Undo / redo:** In the editor, **Ctrl+Z** undoes a Stretch or Morph handle drag, and **Ctrl+Y** redoes it. Each gesture is recorded as a single editor undo step. Undo or redo exits the active Stretch/Morph tool and hides its handles so they cannot remain attached to a part KSP has restored or replaced; choose the tool and part again to keep editing.

Gizmo handles are always drawn on top, so neighboring parts never hide them.

**All-controls window:** Click **Show PartSculptor controls window** in a part's PAW for a movable, scrollable window mirroring its complete PartSculptor control group. Drag the window from its title or unused interior areas (including text labels); buttons, fields, and scrollbars continue working normally. The selected part name is displayed in **bold**. It includes X/Y/Z scaling (enter a number, then click **Set**), session-wide attachment toggles, per-part locks, readouts, and numeric face tilt (**Top/Bottom**, X/Z degrees, **Apply face tilt**). It also has presets, gizmos, reset actions, and manual attachment matching. Close it using the window's **Close** button or the PAW toggle. The window follows the **selected editor part**, including the Vessel Inspector or another part's PAW. Parts without PartSculptor show an informational message. Staged inputs reset on selection.

**Compact PAW option:** Enable **Hide PartSculptor PAW controls (keep window button)** in **Vessel Inspector → Settings**, or toggle it directly at the top of the All Controls window. This keeps only the **Show/Hide PartSculptor controls window** PAW button; the other controls remain available in the window. Saved in `PluginData/UserSettings.cfg`, default `hidePartSculptorPAWControls = false` in `Settings.cfg`. Other modules are unaffected.

The PAW also offers **Show scaling gizmo** and **Show scaled values**. The scaled-values window compares original and current values, including resources and attachment data.

### Other PAW options

- **Stretch children:** Also stretch attached descendant parts that support PartSculptor.
- **Match node sizes:** Automatically fit a newly attached stack part to the cross-section of the node it attaches to.
- **Match attached faces:** When enabled, newly placed stack parts automatically attempt to match their connecting face to the morphed parent. **Morphing the end of an already connected part now also updates its attached neighbor's touching end live**, whether you edit the parent or the child. Only the matching end is reshaped; the opposite face stays as it was. Supports different native sizes, quarter-turn rotations and small deviations caused by tilted nodes. Unsupported, side/surface and unsafe matches are skipped.
- **Copy attached morph steps (opposite-side mirror):** During live Morph edits, duplicate the *incremental* change onto the neighbor's touching end. The same **visible side** is used (including rolled parts), but the **Y/tilt movement is reversed across the joint**: drag the primary's right side down and the connected part's right side moves up. Width/depth and X/Z offsets follow the matching visible directions. The neighbor's existing shape differences and its opposite end are preserved. This is intentionally different from absolute **Match attached faces**, which makes the two touching surfaces geometrically coincide. If both modes are enabled, **Copy** takes priority for live edits; new placements still use Match.
- **Match face to parent now:** Retry a face match from the newly attached child's PAW, even when automatic matching is disabled. An on-screen message explains why a match is unavailable.
- **Scale preset:** Apply an **absolute** preset. **Browse presets** opens the full list of absolute and relative presets.
- **Repeat preset:** Reapply the last preset used, including relative presets.

**Stretch children**, **Match node sizes**, **Match attached faces**, and **Copy attached morph steps** apply to the whole editor session, not to one part. Changing them in the PAW lasts until you leave the editor; their starting values come from **Vessel Inspector → Settings**.

### Matching faces on attached parts

Enable **Match attached faces** in any PartSculptor PAW before you attach a new part. It is **off by default**, independently of **Match node sizes**. To enable it automatically in future editor sessions, use **Vessel Inspector → Settings → Match attached faces by default**.

After morphing one end of a tank, attach another compatible PartSculptor tank **by a stack node** to that end. The new tank's touching face is reshaped (width, depth and tilt) to fit the four corners of the parent face; the new tank's other end stays as it was. A small closest-fit tolerance accommodates tilted node orientations that rotate a placed part slightly. Existing craft are never automatically re-morphed merely by opening them.

**Live face matching (0.0.4.109):** With **Match attached faces** enabled, select the Morph tool and drag an end-face handle of an already attached part. Its stack-connected neighbor automatically attempts to fit its own touching face to the edited face, during the drag. This works both from parent to child and child to parent; it also runs when you reset the face or cancel a drag, so the neighbor can return to its previous shape. The opposite face of each neighbor is preserved, and matching never propagates beyond the immediate neighbor. Parts that share symmetry are handled independently at each physical join. This option applies to both new placements and subsequent face edits; disable it if you want to edit faces independently. Already-loaded craft are unchanged **until you actually edit a face**.

**Mirrored copy mode (reworked in 0.0.4.113):** Enable **Copy attached morph steps** in a PartSculptor PAW and drag a connected end-face handle. The module records the source and neighbor face values and relative axes *before* moving anything. The neighbor receives the corresponding **relative** width/depth/offset change on the same visible X/Z side, while face tilt is **reversed along the stack's Y direction**. For example, lowering the right side of the primary face raises the right side of the connected face relative to that part. The other end is left unchanged; this is not a geometric Match. The orientation is captured before KSP moves an attached child, so the mapping stays stable during a drag.

If both connecting faces were flush before the edit, this mode attempts to keep them flush as width, depth, tilt, and offset change. If they already differed, their original physical differences are preserved where the new shape is representable, rather than forcing the child to match the parent's absolute shape. For different native part sizes, the copied *physical* dimension may correspond to a different normalized width/depth factor on the neighbor. The neighbor's far end stays unchanged, changes do not cascade to grandchildren, and undo/cancel/reset apply their reverse physical motion. If the geometry cannot be mirrored to within 0.5% of the smaller native face span (or 1 mm), or would exceed Career, volume or morph limits, that neighbor is skipped instead of leaving a visible gap. Use **Match face to parent now** first when the two faces were not flush initially. New part placement continues to use the separate **Match attached faces** option. This mode is off by default and its starting value can be set in Vessel Inspector → Settings.

If a part did not match when attached, right-click **the newly attached child** and choose **Match face to parent now**. This tries again with the current geometry, regardless of the automatic toggle, and displays a reason if matching is not possible. It can also be used after editing an already attached parent.

The **Match attached faces** option is a **geometry match**, not generic mesh welding or a surface-attach feature. **Copy attached morph steps** now mirrors incremental morph-control changes on the same visible sides, reversing local longitudinal tilt; it does not force different starting faces into an absolute match. For live updates, only an end face that actually changes is considered, and only its directly attached stack neighbor is modified. Other ends and other parts are not automatically changed. Both nodes must be near the model's Y end faces, with compatible planar geometry. The **Match** mode permits at most 7.5% closest-fit corner mismatch. **Copy** is deliberately a mirrored step, not a face fit; it can create an angled joint or gaps on some models, even when the two faces started flush. Use Match when flush contact is the priority. Arbitrary rotations or major skew, joint offsets that cannot be reproduced, unsupported colliders, large size differences outside the 0.25–4 face limits, and Career/volume-restricted changes are left unchanged. If both matching toggles are enabled, node-size scaling is applied first, then the new face fit is attempted. Test tilted/offset joins in the editor before launch.

## Face morphing

**Numeric face tilt:** Open the part's PAW, choose **Top** or **Bottom** under **Tilt face**, enter signed **Tilt X (degrees)** and **Tilt Z (degrees)**, then click **Apply face tilt**. Both inputs are absolute angles relative to the part's own face plane (not its world/editor rotation). The face's existing width/depth and part X/Y/Z scaling are accounted for; other face dimensions and offsets are preserved. Unapplied entries do not deform the part. The Apply action uses the normal face morph path, so Copy/Match, symmetry, node movement, and Ctrl+Z undo remain available. Values beyond the configured `maxFaceTilt` slope are rejected rather than reduced automatically. Switching Top/Bottom reloads that face's current angles. The original Morph gizmos remain fully supported.

**Tilt readout:** The PartSculptor PAW shows **Bottom face tilt** and **Top face tilt**. The Morph tool's instruction box shows both values, and the tooltip while dragging shows the edited face's value live. The first number is the total angle in degrees from a flat end face; X and Z are signed component angles (for example, `12.6° (X +12.6°, Z 0.0°)`). Values account for face width/depth and independent X/Y/Z stretching. This measures the face relative to the part's own axis, **not** the rotation of the entire part in the editor. A face with no tilt reads `0.0°`.


Select the **Morph** tool (the sixth editor tool) and click a compatible part. Eight **orange** handles control one end face and eight **cyan** handles control the other. Each end has four corner handles and four smaller handles halfway along its perimeter edges. Between the two faces, the part's shape blends smoothly from one to the other. Handles and their outline are drawn on top of other parts, so you can still reach a face that a neighboring part covers.

You can also use **Show face morph handles** in the part's PAW. **Reset face morph** restores the original shape.

| Gesture | Result |
| --- | --- |
| Drag a corner | Resize the face in X and Z. See **Lock X/Z** below. |
| Drag an edge midpoint | Resize only X (left/right edge) or Z (front/back edge), when Lock X/Z is off. |
| **Alt** + drag an edge midpoint | Tilt the face on the axis perpendicular to that edge. |
| **Shift** + drag | Resize, snapping to the configured increment. |
| **Alt** + drag | Tilt the face. |
| **Shift + Alt** + drag | Move the whole face sideways (in the part's local X/Z plane), with snapping. Size and tilt are unchanged. |
| **Ctrl + Alt** + drag | Grow or shrink the face evenly from its center, keeping its width-to-depth ratio. |
| **Ctrl + Alt + Shift** + drag | Grow or shrink evenly, with snapping. |
| **Esc** or right-click while dragging | Cancel the drag and restore the previous shape. |
| **M** (while Stretch or Morph is active) | Switch between the Stretch and Morph tools. |

**Hold modifier keys before clicking a handle.** The drag mode is chosen when you press the mouse button and does not change mid-drag.

**Attached-child tilt handle direction:** With **Copy attached morph steps** enabled, Alt-dragging the face of a child that connects to its parent compensates for the child-body rotation so the handle follows the mouse. The projected drag direction is held constant for each gesture, even as the child rotates. Parent faces, free child faces, and non-Copy modes retain their previous behavior.

Midpoint resize handles follow the mouse along their permitted on-screen movement direction, including at oblique camera angles. With **Lock X/Z** enabled they move using the common face scale factor; when Shift snapping is active they move in the configured increments. A handle will not follow mouse motion perpendicular to the axis it controls, or beyond the face size limits.

The extra edge handles control the same rectangular face geometry, not independently deformable or curved edges. **Shift** snapping, **Shift+Alt** translation, and **Ctrl+Alt** radial resizing also work from the midpoint handles. Each end face is controlled independently. A help box below the editor toolbar summarizes these controls while the Morph tool is active.

Morph shapes, including face offsets, tilt, and size, are saved with the craft and kept when a part is duplicated or mirrored by symmetry. A duplicated part keeps its own independent geometry: subsequent Morph changes to the original do not alter an Alt-copied part. Genuine symmetry counterparts still follow the selected part's face settings.

When you tilt a face in the editor, parts connected to its attach nodes move and rotate with those nodes, pivoting around their attachment points. Connected child assemblies follow as a group.

> **Linux users:** Many desktop environments use Alt + drag to move windows, which prevents KSP from seeing Alt-based gestures. If tilt or translate doesn't work, change or disable that shortcut in your desktop's window settings.

### Lock X/Z

**Lock X/Z** (in the part's PAW) controls what an ordinary corner or edge-midpoint drag does:

- **On** (default): width and depth change together, keeping the face's current proportions. This also applies with Shift snapping.
- **Off**: corners can resize width and depth independently; edge midpoints change only their corresponding axis.

It does not affect tilt, translate, or radial resize. This is a separate setting from **Lock proportions**, which applies to Stretch scaling of the whole part.

Lock X/Z is saved per part with the craft and copied by symmetry. To change the default for newly placed parts, set **Lock X/Z by default** in **Vessel Inspector → Settings**; existing parts keep their current setting.

### Snapping increment

In **Vessel Inspector → Settings**, choose the **Face morph snap increment**: **0.01**, **0.05** (default), **0.10**, or **0.25**, then click **Apply** or **Save**. This affects face morphing only; the Stretch gizmo always snaps in 0.05 steps.

### Which parts can be morphed

Face morphing works on parts with readable static meshes and supported colliders. A `ModuleAnimateGeneric` module alone no longer blocks morphing: the part still undergoes normal mesh/collider validation. Skinned or dynamically regenerated meshes are not supported, and animation-driven transform movement may produce distorted deployed geometry or misaligned colliders; test animated parts carefully. B9 Part Switch subtypes can change resources, use `TEXTURE` blocks (including `transform` / `baseTransform` texture targets), or use `transform = someModelName` to show/hide **pre-existing static model variants**. PartSculptor captures both active and inactive variant meshes, so they remain morphed when the subtype changes. Nested `SUBTYPE { NODE { name = ... position = ... } }` stack-node modifiers (including `positionOffset` and `size`) are supported. A subtype without a NODE override returns to the original PART node baseline. B9 still controls node visibility via the separate `node = ...` selector, which PartSculptor does not override. Nested `SUBTYPE { MODULE { ... } }` blocks are ignored by the face-morph compatibility check (B9PartSwitch retains responsibility for applying them). Nested `TRANSFORM { ... }` blocks that reposition, rotate, or scale model transforms, separate node-visibility switches, and unsupported `MATERIAL` blocks are not supported.

Always check collisions, attachments, and flight behavior before relying on a heavily morphed craft.

## Presets

Click **Browse presets** in a part's PAW to see the full catalog:

- **Absolute** presets set X/Y/Z to fixed values.
- **Relative** presets multiply the current X/Y/Z.

The PAW's **Scale preset** selector lists only absolute presets. It shows **Custom** when a part's scale has been edited by hand.

To create your own presets:

1. Open **Vessel Inspector** from its toolbar button in the VAB or SPH.
2. Select a part with PartSculptor and click **Edit presets**.
3. Use **New from current scale** or **Duplicate selected**, choose absolute or relative, enter values, and save.
4. Select a preset to apply it to the part.

Built-in presets can't be edited, but you can duplicate and change them. Built-in presets are in `GameData/PartSculptor/PluginData/Presets.cfg`; your own are saved separately in `GameData/PartSculptor/PluginData/CustomPresets.cfg`, so they survive updates. Presets are shared across all craft.

## Vessel Inspector and settings

Click the **PartSculptor** toolbar button to open **Vessel Inspector** (VAB, SPH, and flight). Select a part in the list to compare its original and scaled values. Options include **Scaled parts only** (on by default), hover highlighting, and a resizable window. In the editor, it also provides **Edit presets**, **Settings**, and an inspect-only selection mode.

To find a particular part, type its full or partial name in **Filter by name** above the part list. The search is case-insensitive and matches both the displayed part title and its internal part name. You can combine it with **Scaled parts only**, and use **Clear** to show all matching parts again. Clicking a part directly on the craft clears any filter that would otherwise hide that part. The **Show unchanged values** toggle in the right-hand report stays selected (or cleared) when you switch parts in the Vessel Inspector.

**Vessel Inspector → Settings** saves your defaults for:

- Match node sizes
- Match attached faces
- Copy attached morph steps
- Stretch children
- Lock proportions
- Lock X/Z
- Face morph snap increment
- Morph tip opacity (10–100%)
- Alternate skin (host IMGUI instead of KSP skin)

These preferences are stored in `GameData/PartSculptor/PluginData/UserSettings.cfg`. The Morph tip opacity slider adjusts only the **background transparency** of the help box under the editor toolbar and the tip shown while dragging; its text remains fully opaque. At 100%, tip backgrounds are completely solid even if the selected KSP skin uses translucent box textures. The alternate-skin checkbox changes the appearance of PartSculptor windows without changing KSP's own UI. The Settings window keeps **Save**, **Apply**, and **Cancel** accessible in a fixed footer: Save commits and closes; Apply saves without closing; Cancel restores all settings to their values when the window opened, even if Apply was clicked, and rolls back those changes in `UserSettings.cfg`. The settings list scrolls if necessary on smaller screens. Drag any unoccupied area of the Settings window to move it; buttons, sliders and scrollbars remain interactive.

Advanced options are in `GameData/PartSculptor/PluginData/Settings.cfg`: per-axis limits and UI steps, mass and volume safety caps, compatibility and effects toggles, debug logging, and Career Mode limits. Changes to this file usually require a KSP restart. The All Controls window uses the supplied compact input layout and centers its scale-preset label between the preset arrows.

## Career Mode limits

In Career Mode, the scales you can set depend on the upgrade level of the building you're in (VAB or SPH):

| Facility level | Minimum per axis | Maximum per axis | Volume limit (X × Y × Z) |
| --- | ---: | ---: | ---: |
| 1 | 0.75x | 1.5x | 3x |
| 2 | 0.5x | 3x | 27x |
| 3+ | 0.25x | 10x | 1,000x |

Both limits apply at once, so the volume limit may stop you before an axis reaches its maximum. For example, at level 1, scaling all three axes to 1.5x would exceed 3x volume.

Science and Sandbox modes ignore these limits. Saved craft are never resized when loaded, even if they exceed the current limits. You can change or disable the career limits in `Settings.cfg`; the general mass and volume safety limits still apply.

## Compatibility and cautions

- Dry mass and cost are recalculated from the part's original values. Resource capacities are updated, keeping fill levels where possible. PartSculptor does **not** automatically change engine thrust or other unrelated module stats.
- Where scaling rules or module integrations exist, other values are adjusted too. Test B9 Part Switch, ModularFuelTanks/RealFuels, Firespitter, InterstellarFuelSwitch, docking ports, and engine effects with your particular craft and mod combination.
- Crew capacity only increases if the part's IVA has enough seats. Stock pods do **not** gain usable seats just by growing.
- Scaling and morphing can affect attachment placement, collisions, docking alignment, and animations. Back up important craft before major edits.


**Mirrored face inclination:** With **Copy attached morph steps** enabled, the
attached face now copies each change in physical face slope (tilt angle) rather
than copying only raw tilt parameters. This accounts for face width/depth and
individual part X/Y/Z stretch, while preserving pre-existing angle differences.
The child's separate rigid rotation still moves it to follow the attachment.
If in-game angles disagree, set `debugLogChanges = true` in
`GameData/PartSculptor/PluginData/Settings.cfg` and compare the `mirrored face
inclination` entries in `KSP.log`.

### B9 mesh-switch attachment node modifiers (0.0.4.145)

When a B9PartSwitch subtype changes a stack attachment node using `SUBTYPE { NODE { name = ... position = ... } }`, PartSculptor reads the selected subtype's immutable `position`, optional `positionOffset`, and `size` as its native node data. Selectors support exact names, `*`/`?` wildcards, and B9 `/regex/` patterns. A subtype without a matching `NODE` override restores the original PART node position and size, instead of inheriting the prior stretched subtype. PartSculptor then applies its X/Y/Z stretch and any face morph without multiplying those changes on repeated switches or craft loads. B9 still owns node visibility (`node = ...`) and mesh selection (`transform = ...`): a `NODE` modifier moves/resizes an existing stack node, it does not create or enable it. Unlike older PartSculptor versions, valid NODE modifiers no longer disqualify otherwise morph-safe static mesh variants. Ambiguous or invalid NODE modifiers, and mesh-repositioning `TRANSFORM { ... }` blocks, remain unsupported for face morphing.

For example, ORANGES `oranges_NLSUS_interstage` has a stock `top` Y of `2.4885` in NLSUS-A and an override of `1.208` in NLSUS-B. With a PartSculptor Y stretch of 1.5 and no TweakScale, the expected positions are Y=3.73275 (A) and Y=1.812 (B). Both nodes should remain visible where B9's active mesh expects them. Test both switch directions, with and without a part attached to `top`.

### External attachment-node updates (0.0.4.138)

PartSculptor now remembers the native positions supplied by other part modules as
well as its own pre-morph and final deformed attachment-node positions. When a
compatible stock variant or B9 switch changes a node, it reapplies the current
stretch and face morph without treating its own previous morph as a new base.
The history is written into craft files and supports existing craft files that
pre-date the feature. Normal editor changes and load/flight startup repairs use
different movement rules to avoid moving a saved craft during initialization.
See `Tests/EXTERNAL_NODE_RECOVERY_0.0.4.138.md` for an in-game test plan.

## Troubleshooting

### Morph handles don't appear

Select the Morph tool and click the part. If the part can't be morphed, PartSculptor shows the reason, such as:

- the part doesn't have PartSculptor,
- its mesh can't be read,
- its collider type isn't supported, or
- a robotic or transform-switching module is present, or its part-switcher configuration is incompatible with face morphing.

The reason is also written to `KSP.log`.

If no reason appears but you still can't see handles, they may be behind the camera. Orbit or zoom until the part is in view; handles on long or offset parts appear even when the part's center is off-screen.

### In-flight fuel display shows the original upper limit

For a stretched tank, PartSculptor now refreshes the in-flight Part Action Window when it opens so the displayed maximum matches the live `PartResource.maxAmount`, including vessels launched with an already-correct saved capacity. Ordinary fuel consumption does not trigger UI rebuilds. If the number remains wrong, note whether you are viewing the part's right-click menu or a separate resource-monitor mod, and include the tank name and fuel-switcher modules in your report.

## License

- [License.md](License.md)

## Fuel/Part Switchers
There is support for the following part/fuel switchers:

- B9 Part Switch
- ModularFuelTanks
- RealFuels
- Firespitter
- InterstellarFuelSwitch
- Configurable Containers
- Cryo Tanks
- Simple Fuel Switch

There is custom code to work with the following:
- B9 Part Switch
- ModularFuelTanks
- RealFuels
- Firespitter
- InterstellarFuelSwitch

### B9PartSwitch and stretched tank capacities

PartSculptor uses B9PartSwitch's selected tank type and scaled `baseVolume` to
calculate the actual resource capacity. In the editor and on entering flight,
it reconciles B9-managed `PartResource.maxAmount` without changing the fuel
amount or refilling a partially emptied tank. Non-B9 tanks use the normal
resource-scaling path.

B9 `ModuleB9PartSwitch` fields `affectFARVoxels`, `advancedTweakablesOnly`
and `parentID` are accepted by the face-morph compatibility check. Repeated
`transform = ...` fields inside `SUBTYPE` may select between pre-existing static
meshes. B9 still owns which transform group is active; PartSculptor only deforms
the independently cloned mesh vertices and supported colliders.

### B9 `volumeAddedToParent` and face morphing

B9PartSwitch child switchers (`parentID = ...`) can set a signed
`volumeAddedToParent` on each `SUBTYPE`. PartSculptor scales this contribution
from its **original** value by the part's total X/Y/Z and face-morph volume
factor, just as it scales B9 `baseVolume`. For example, a subtype with
`volumeAddedToParent = -200` becomes `-250` at 1.25× volume, or `-100`
at 0.5× volume. Zero remains zero, and inactive subtypes are updated too.

Parent B9 tank resources are recalculated in the editor without changing their
fill percentage; in flight, capacities are reconciled without refilling or
emptying tanks. Values return to their native amounts at 1× geometry and are
preserved across subtype switches, undo, symmetry, and craft reloads. B9 remains
the owner of parent/child tank allocation.

### Procedural Geometry (experimental)

In the VAB or SPH, click the seventh editor tool marked **PG**, next to Stretch and Morph, then click a PartSculptor-enabled part. The Procedural Geometry panel appears beneath the editor widgets. Select **Add ring** to insert real vertices at a cross-section of the existing mesh. The panel shows the number of inserted visual vertices. Use the ring selector and enter **Height (%)** from 1 to 99 (measured along part-local Y), **X multiplier**, and **Z multiplier** (each 0.1–4.0). Select **Apply ring changes** to change that ring; **Remove ring** or **Clear all** to restore an earlier profile. Add up to six rings; changes are mirrored to symmetry counterparts and can be undone with Ctrl+Z.

The selected ring displays nine draggable handles: red X-side handles, blue Z-side handles, a green center handle for Y height, and four yellow corner handles for simultaneous independent X/Z resizing. Rotate the camera to see the ring plane at an angle if a corner cannot be dragged edge-on. Drag a handle to preview the outline; release to rebuild the mesh and collider as one undoable edit. Hold **Alt** before dragging a red, blue or yellow ring handle to tilt the planar ring about its center, like the Morph tool. A side handle tilts along one axis; a corner handle tilts along both. The green center handle moves the entire ring along local Y, with or without Alt. Shift no longer changes PG dragging. The drag mode stays fixed until release. Escape or right-click cancels the preview. Rotate the editor camera if an axis is viewed end-on. Ring-center drags cannot cross neighboring rings.

**Individual vertices:** choose **Vertices** in the PG panel, click **Add vertex**, then click directly on a visible triangle of the selected part. A face insertion splits the triangle into three. Use **Add edge/ring vertex** and click within 14 screen pixels of a visible edge, including edges generated by procedural ring cuts, to snap the insertion onto that edge. Both adjoining triangles are split and separate material/UV seam attributes are preserved within the same mesh. Click between existing endpoints. The new vertex appears as a yellow handle with a white border when selected. Repeat Add vertex to insert more (up to 64 per part). Select a yellow vertex to display a red X, green Y and blue Z axis gizmo (84 pixels long per axis). Drag an arrow for single-axis movement, or drag the yellow handle in the part-local X/Z plane (Alt for Y). Hold Ctrl and click yellow handles to add or remove them from the selection. Selected vertices have white borders; the shared gizmo is centered at their average position. Drag any selected handle or the group center to translate the entire selection, or use the shared arrows to constrain movement. Click an unselected handle without Ctrl to replace the selection. **Select all visible** selects editable vertices on the current part (not inactive mesh variants); **Deselect all** or Esc clears the selection. Esc/right-click during a drag cancels it instead. Each group drag creates one undo checkpoint. Group motion solves for offset increments that account for overlapping soft influence and inherited subdivision movement, so selected handles translate equally. Ill-conditioned groups are rejected; reduce the radius or select fewer vertices. Enable **Soft selection**, enter a radius in part-local metres or use the synchronized slider, then click **Apply influence radius**. Radius entry and slider use at most three decimal places (0.001 m). The label, slider and compact numeric entry share one row, with the field sized for 0.000; larger values remain supported. Hover over the mode buttons, mirror choices, Soft selection or collision fallback toggle for help. The slider spans at least the part diagonal or 1 m and expands for larger typed values. Nearby vertices on the same visual mesh follow a smooth falloff; zero or disabled restores hard selection. The radius is saved with each vertex and applies to its total offset, so changing it can reshape an existing edit. Applying a radius with a group selected sets it on all selected vertices; the panel indicates mixed radii. Selection is editor-only and is cleared on part or vertex-topology changes. The handle previews the change; mesh, collider, affected attachment nodes and calculated volume update on release. Use **Delete selected** to remove the selected added vertices and rebuild topology from the native mesh, replaying remaining edits. **Clear vertices** restores ring-only topology; remove rings separately to restore native topology. Ctrl+Z restores deleted edits. Retained anchors keep their stored offsets but may change position when a supporting vertex is removed. Switch back to **Rings** for the existing ring tools. The two modes share a part's editable geometry but show different handles to avoid ambiguous clicks.

**Mirrored vertices:** in Vertices mode choose Off, X, Y or Z under Mirror plane (one plane at a time; off by default). X reflects across the plane perpendicular to local X, through the center of the active undeformed visual bounds; Y and Z work likewise. Insertion creates a corresponding editable vertex on the opposite native surface in the same undo operation, including edge/ring insertions. Select existing vertices and click **Create missing mirrors** to add counterparts (copies reflected stored offsets and radius). Dragging reflects the movement component normal to the plane and preserves tangential components; on-plane vertices are constrained to the plane. If both members are selected, the last-selected member drives the pair. Radius application and deletion include paired vertices. Mirror mode is an editor control, not saved; the individual edits are saved normally. Existing asymmetric offsets are not automatically equalized. Missing or nonmatching surfaces reject the operation; disable mirroring to edit asymmetric geometry. The normal 64-vertex limit includes counterparts. Ring controls are unchanged.

Individual vertex offsets now deform the collision meshes as well as the visual surface. Existing collider vertices sample the nearby edited surface; additional projected support points refine coarse colliders. Attachment nodes sample displacement and normal rotation from the nearest visible triangle; stack-connected parts follow moved nodes. The calculated volume uses the ratio of enclosed collision-mesh volume before and after individual edits, multiplied by the existing ring/face/axis volume factors. Mass, cost, stock resources, and supported tank switchers use the resulting multiplier. For convex colliders, PartSculptor builds a closed convex hull from the baseline and edited point clouds, independent of the native triangle winding or count. Dense hulls may be simplified to fit the convex budget. The volume ratio uses those hulls; nonconvex colliders retain signed tetrahedral volume. Inward dents that do not change a convex hull may not change capacity. Overlapping collider volumes are still approximate. Invalid or degenerate hulls are rejected before committing. Rejections and unexpected drag cancellations display a reason and log it to KSP.log. The ring tools retain their previous behavior. Edge insertion splits incident triangles within one mesh, including coincident indexed seam copies. Separate mesh objects are not welded. Original native vertices and ring-generated endpoints are not individually deletable. Retained anchors are reclassified after topology changes; ambiguous overlapping surfaces and invalid geometry are rejected. Each edit is associated with its original visual mesh, including the B9 variant where it was created; inactive variant handles are hidden. Vertex edits are propagated to compatible symmetry counterparts in their corresponding mesh-local coordinates. Test asymmetric mirrored parts separately.

**Individual vertex collision fallback:** PartSculptor first searches usable colliders with matching activation/variant, choosing the nearest valid triangle. If none works, it searches other active colliders on the same part. Distinct B9 transform selectors are allowed in this second pass only when both objects are active and no common switch defines them as mutually exclusive. Inactive edits never affect an active alternative; unmatched inactive edits are deferred until that variant is activated. Disabled colliders, triggers, invalid triangles and convex candidates without a valid closed baseline are excluded. On failure, search `KSP.log` for `PG collision fallback` and `PG collision inventory` to see the visual mesh, candidate paths, variant, activation, distance and rejection reasons, including colliders not registered for deformation.

An experimental last resort can be enabled using **Generate vertex collision fallback (experimental)** in the PartSculptor Settings window. The same toggle is available in the Vertices panel below **Apply influence radius**, even with no vertex selected. It saves immediately and updates the Settings window if open. Apply or Save in Settings updates the preference for subsequent geometry operations without restarting; Cancel restores the opening preference, including any PG toggle changes made while the Settings window was open. The default is false, or the value of `generateVertexCollisionFallback` in `GameData/PartSculptor/PluginData/Settings.cfg` when no user override has been saved. Changes made in either location are saved in `UserSettings.cfg` and take precedence over the shipped configuration. If no usable native collider matches an active edited visual mesh, a bounded convex hull is built from that mesh before and after deformation. Only a successful preflight can create a private collider; flat/degenerate meshes are rejected. The volume ratio includes the generated hull, and attachment nodes use the same edited visual surface. The fallback collider is removed when no longer needed, including clearing edits and rebuilding clones. It does not replace or enable disabled native colliders. Generated hulls may overlap existing colliders or fill holes, and the volume ratio is approximate; verify physics and keep the setting enabled when loading a craft that relies on it. Unsupported collider types and unreadable native meshes remain rejected. This setting is not required for the automatic native-collider fallback.

Cross-sections interpolate linearly between the unmodified end faces and the specified rings. Triangle intersections create additional vertices, preserving UV channels, colors, tangents, normals, and submesh/material assignments. This is not full arbitrary topology editing: it cannot add or remove holes, change the number of sides in a surface, or edit animated/skinned/blend-shape meshes. For B9 switchers with exact, named static mesh alternatives, cross-section height is measured relative to each alternative's own Y bounds, avoiding deformation of a shorter variant's end. Regex/wildcard transform selectors and independent collision objects may require manual testing.

Generated visual and collision meshes are private to the part; originals and prefabs are never edited. Mesh collision proxies are recalculated; **convex colliders still use a convex hull**, so deep inward constrictions will not have perfectly matching collision even if the visual mesh does. For individual vertex edits, convex collision hulls are bounded to 252 triangles (below Unity’s 255 limit); the raw source triangle count alone no longer rejects an individual support point. Ring subdivision retains its existing 255-triangle safety check. Subdivided meshes must stay below 65,000 vertices. Some highly detailed or non-readable parts may reject this mode entirely. The ring shape-volume multiplier is approximated by integrating cross-section multipliers; the individual-vertex correction is calculated from signed tetrahedral volumes of the collision hulls or nonconvex meshes. On-screen volume/mass/fuel calculations use these factors, but unusual geometry requires in-game verification. End faces and their attach nodes are intentionally unchanged by procedural interior rings.

Ring parameters are saved as `PARTSCULPTOR_PROCEDURAL_RING` and individual vertex anchors and offsets as `PARTSCULPTOR_PROCEDURAL_VERTEX` in the craft. On editor or flight load, topology is rebuilt from the immutable native mesh before the existing face morph is applied. Test save/load, stock drag cubes/FAR, B9 subtype switching, symmetry and flight collision carefully.


### Limitations
- B9PartSwitch resource and `TEXTURE` changes are supported. `transform`/`baseTransform` inside `TEXTURE` select renderers; `transform = ...` directly inside `SUBTYPE` toggles pre-existing static model variants and is also supported. All existing variant meshes, including inactive ones, are morphed. Nested `NODE` modifiers that move/resize existing stack nodes are now supported (position, positionOffset, size, exact/glob/regex node names). Nested `MODULE` blocks under B9 subtypes are ignored by the face-morph safety checker and left to B9. Nested `TRANSFORM` blocks that move/rotate/scale model geometry, separate `node = ...` visibility switches, and `MATERIAL` blocks remain unsupported for face morphing. Unrecognized B9 fields with numeric scalar or numeric-vector values are rejected, while unknown nonnumeric fields are allowed by the compatibility-value filter; unrecognized nested nodes remain rejected. For variant models with different geometry, verify the full set of meshes, collision shapes, attachment placement, and flight behavior in-game.
- Procedural Parts (`ProceduralPart`) are not supported.  The ModuleManager patch that adds `ModulePartSculptor` skips any part that already has `ModulePartSculptor` or contains `ProceduralPart`.
- The automatic ModuleManager patch also skips parts containing `WingProcedural`, `SmartTankPart`, `DiameterMatcher`, or `TextureDefaulter`. These parts do not receive `ModulePartSculptor` automatically.

- Modular Segmented SRBs: automatic injection skips parts with a `MODEL` path under `ModularSegmentedSRBs/`. Existing module-based exclusions remain. For parts using legacy `mesh =` rather than `MODEL`, verify the loaded ModuleManager cache and add an explicit part-name exclusion if necessary.
