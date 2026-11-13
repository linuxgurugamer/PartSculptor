# PartSculptor

**PartSculptor** is a part-scaling and face-morphing mod for **KSP 1.12.x**. In the VAB and SPH, you can stretch a part independently along its local X, Y, and Z axes, or reshape its two end faces using interactive handles. Your changes are saved with the craft and restored when it is loaded.

PartSculptor is more than a visual resize: it also updates supported attachment geometry, mass, cost, resource capacities, and other scale-dependent properties.

This started as a derivative of a mod uploaded to Spacedock called StretchScale (https://spacedock.info/mod/4718/StretchScale). Changes made via StretchScale do not affect parts' stats or aero models like TweakScale does, only changing the visible model and its collision.

The mod is currently in an alpha test with a few users. I do welcome more people who would be willing to test and provide feedback.

Forum page is located here:  https://forum.kerbalspaceprogram.com/topic/232076-112x-linuxgurugamers-part-sculptor/

## Features

- **Independent X/Y/Z scaling**, or **Lock proportions** to resize uniformly. Use numeric fields, built-in or custom presets, or the three-axis editor gizmo.
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

The single ModuleManager patch adds `ModulePartSculptor` to **eligible parts**, including command pods and cockpits, without requiring PatchManager or an opt-in step. It excludes Procedural Parts components (`ProceduralPart`) and parts that already contain `ModulePartSculptor`. Crew-capacity scaling remains limited by available IVA seats.

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

Face morphing works on parts with readable static meshes and supported colliders. A `ModuleAnimateGeneric` module alone no longer blocks morphing: the part still undergoes normal mesh/collider validation. Skinned or dynamically regenerated meshes are not supported, and animation-driven transform movement may produce distorted deployed geometry or misaligned colliders; test animated parts carefully. B9 Part Switch subtypes can change resources, use `TEXTURE` blocks (including `transform` / `baseTransform` texture targets), or use `transform = someModelName` to show/hide **pre-existing static model variants**. PartSculptor captures both active and inactive variant meshes, so they remain morphed when the subtype changes. Nested `TRANSFORM { ... }` blocks that reposition, rotate, or scale transforms, attachment-node switches, and unsupported `MATERIAL` blocks are not supported.

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

These preferences are stored in `GameData/PartSculptor/PluginData/UserSettings.cfg`. The Morph tip opacity slider adjusts only the **background transparency** of the help box under the editor toolbar and the tip shown while dragging; its text remains fully opaque. At 100%, tip backgrounds are completely solid even if the selected KSP skin uses translucent box textures. The alternate-skin checkbox changes the appearance of PartSculptor windows without changing KSP's own UI. The Settings window keeps **Save**, **Apply**, and **Cancel** accessible in a fixed footer: Save commits and closes; Apply saves without closing; Cancel restores all settings to their values when the window opened, even if Apply was clicked, and rolls back those changes in `UserSettings.cfg`. The settings list scrolls if necessary on smaller screens.

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

### Limitations
- B9PartSwitch resource and `TEXTURE` changes are supported. `transform`/`baseTransform` inside `TEXTURE` select renderers; `transform = ...` directly inside `SUBTYPE` toggles pre-existing static model variants and is also supported. All existing variant meshes, including inactive ones, are morphed. Nested `TRANSFORM` blocks that move/rotate/scale geometry, node switches, `MATERIAL` blocks, and unknown B9 operations remain unsupported. For variant models with different geometry, verify the full set of meshes, collision shapes, attachment placement, and flight behavior in-game.
- Procedural Parts (`ProceduralPart`) are not supported.  The ModuleManager patch that adds `ModulePartSculptor` skips any part that already has `ModulePartSculptor` or contains `ProceduralPart`.
- The automatic ModuleManager patch also skips parts containing `WingProcedural`, `SmartTankPart`, `DiameterMatcher`, or `TextureDefaulter`. These parts do not receive `ModulePartSculptor` automatically.

- Modular Segmented SRBs: automatic injection skips parts with a `MODEL` path under `ModularSegmentedSRBs/`. Existing module-based exclusions remain. For parts using legacy `mesh =` rather than `MODEL`, verify the loaded ModuleManager cache and add an explicit part-name exclusion if necessary.
