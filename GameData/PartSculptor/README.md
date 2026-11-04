# PartSculptor

**PartSculptor** is a part-scaling and face-morphing mod for **KSP 1.12.x**. In the VAB and SPH, you can stretch a part independently along its local X, Y, and Z axes, or reshape its two end faces using interactive handles. Your changes are saved with the craft and restored when it is loaded.

PartSculptor is more than a visual resize: it also updates supported attachment geometry, mass, cost, resource capacities, and other scale-dependent properties.

This started as a derivative of a mod uploaded to Spacedock called StretchScale (https://spacedock.info/mod/4718/StretchScale). Changes made via StretchScale do not affect parts' stats or aero models like TweakScale does, only changing the visible model and its collision.

The mod is currently in an alpha test with a few users. I do welcome more people who would be willing to test and provide feedback.

Forum page is located here:  https://forum.kerbalspaceprogram.com/topic/232076-112x-linuxgurugamers-part-sculptor/

## Features

- **Independent X/Y/Z scaling**, or **Lock proportions** to resize uniformly. Use numeric fields, built-in or custom presets, or the three-axis editor gizmo.
- **Face morphing** with sixteen handles (four corners and four edge midpoints on each end face). Create tapered, tilted, offset, and radially expanded or contracted shapes.
- **Craft persistence and symmetry** for part scales and morph settings. Optional **Stretch children** also scales attached descendant parts.
- **Mass, cost, and resource capacity scaling** from the part's original values, so repeated edits never compound. Fuel fill levels are kept as capacities change.
- **Attachment handling** for stack and surface nodes: node size tiers, repositioning of attached parts, attach nodes that follow tilted morph faces, and optional automatic stack-node size matching.
- **Modular Fuel Tanks / RealFuels volumes:** `ModuleFuelTanks` tank volume follows stretched and morphed geometry. The optional adapter uses RealFuels 15.16.1 `ChangeTotalVolume()` with utilization-aware total volume; older MFT callback/message variants remain as fallbacks, and MFT/RealFuels retains responsibility for fuel allocation. Validate individual installations in the editor and flight.
- **Mod and part compatibility**: docking ports, B9 Part Switch, supported fuel-switcher modules, and engine visual effects (including optional Waterfall effects).
- **TweakScale coexistence**, including support for `TWEAKSCALEEXPONENTS`, `TWEAKSCALEBEHAVIOR`, and compatible part/module scaling rules. PartSculptor includes `Patches/ScaleExponents.cfg` with fallback `TWEAKSCALEEXPONENTS` and `TWEAKSCALEBEHAVIOR` definitions when TweakScale is not installed.
- **Vessel Inspector** with per-part reports of scaled values, change filtering, highlighting, and access to presets and settings in the editor. Inspection is also available in flight.
- **Career Mode scale limits** based on VAB/SPH upgrade level, plus configurable safety limits.
- **Crewed parts included by default**, with crew-capacity increases limited by the seats actually available in the part's IVA.

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

The PAW also offers **Show scaling gizmo** and **Show scaled values**. The scaled-values window compares original and current values, including resources and attachment data.

### Other PAW options

- **Stretch children:** Also stretch attached descendant parts that support PartSculptor.
- **Match node sizes:** Automatically fit a newly attached stack part to the cross-section of the node it attaches to.
- **Scale preset:** Apply an **absolute** preset. **Browse presets** opens the full list of absolute and relative presets.
- **Repeat preset:** Reapply the last preset used, including relative presets.

**Stretch children** and **Match node sizes** apply to the whole editor session, not to one part. Changing them in the PAW lasts until you leave the editor; their starting values come from **Vessel Inspector → Settings**.

## Face morphing

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

Midpoint resize handles follow the mouse along their permitted on-screen movement direction, including at oblique camera angles. With **Lock X/Z** enabled they move using the common face scale factor; when Shift snapping is active they move in the configured increments. A handle will not follow mouse motion perpendicular to the axis it controls, or beyond the face size limits.

The extra edge handles control the same rectangular face geometry, not independently deformable or curved edges. **Shift** snapping, **Shift+Alt** translation, and **Ctrl+Alt** radial resizing also work from the midpoint handles. Each end face is controlled independently. A help box below the editor toolbar summarizes these controls while the Morph tool is active.

Morph shapes, including face offsets, tilt, and size, are saved with the craft and kept when a part is duplicated or mirrored by symmetry.

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

Face morphing works on static parts with readable meshes and supported colliders. Parts that are animated, skinned, or whose shape is changed by a part switcher can't be morphed safely. B9 Part Switch subtypes that only change resources (such as fuel type or added mass) are fine; subtypes that change meshes, nodes, or materials are not.

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
- Stretch children
- Lock proportions
- Lock X/Z
- Face morph snap increment
- Morph tip opacity (10–100%)
- Alternate skin (host IMGUI instead of KSP skin)

These preferences are stored in `GameData/PartSculptor/PluginData/UserSettings.cfg`. The Morph tip opacity slider adjusts only the **background transparency** of the help box under the editor toolbar and the tip shown while dragging; its text remains fully opaque. At 100%, tip backgrounds are completely solid even if the selected KSP skin uses translucent box textures. The alternate-skin checkbox changes the appearance of PartSculptor windows without changing KSP's own UI. The Settings window keeps **Save**, **Apply**, and **Cancel** accessible in a fixed footer: Save commits and closes; Apply saves without closing; Cancel restores all settings to their values when the window opened, even if Apply was clicked, and rolls back those changes in `UserSettings.cfg`. The settings list scrolls if necessary on smaller screens.

Advanced options are in `GameData/PartSculptor/PluginData/Settings.cfg`: per-axis limits and UI steps, mass and volume safety caps, compatibility and effects toggles, debug logging, and Career Mode limits. Changes to this file usually require a KSP restart.

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

## Troubleshooting

### Morph handles don't appear

Select the Morph tool and click the part. If the part can't be morphed, PartSculptor shows the reason, such as:

- the part doesn't have PartSculptor,
- its mesh can't be read,
- its collider type isn't supported, or
- its shape is controlled by an animation or part switcher.

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

### Limitations
- B9PartSwitch subtypes that change the part's mesh, nodes, or materials are not supported. Only subtypes that change resources (such as fuel type or added mass) are compatible.
- Procedural Parts (`ProceduralPart`) are not supported.  The ModuleManager patch that adds `ModulePartSculptor` skips any part that already has `ModulePartSculptor` or contains `ProceduralPart`.
