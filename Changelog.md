## 0.0.4.72 — Midpoint Morph Handles

- Added eight new morph handles: one at the midpoint of each perimeter edge on both end faces (16 total handles).
- With **Lock X/Z off**, X-edge handles change width only and Z-edge handles change depth only; corner handles retain their original two-axis behavior.
- With **Lock X/Z on**, midpoint dragging scales X and Z together, maintaining face proportions.
- **Alt** tilts only the edge's corresponding axis, **Shift** snaps, **Shift+Alt** translates the full face, and **Ctrl+Alt** radially resizes the whole face, just as with corners.
- Handles are projected and drawn by the same foreground GUI/picking path used by the original corners. No new craft fields or morph geometry formats are introduced.
- Updated README and added midpoint regression/manual tests.


## 0.0.4.71 — Renamed to PartSculptor

- Renamed the mod, source project, DLL, GameData folder, version files, and user-facing interface from StretchScale to PartSculptor.
- Added the new `ModulePartSculptor` KSP module, inheriting the existing scaling and morph implementation. The legacy `ModuleStretchScale` implementation remains available for older saved craft. All persistent per-part values and internal baseline node names are preserved.
- Updated the universal ModuleManager patch to install `ModulePartSculptor` on all parts, excluding parts already using either module. Retained the bundled `ScaleExponents.cfg`; PatchManager is still removed.
- Updated paths, default settings/preset root nodes, toolbar icons, build scripts, README and CKAN/KSP-AVC style version metadata. Added fallbacks for legacy custom presets and user settings.
- Upgrade: remove the old `GameData/StretchScale` installation after backing up custom settings. Rename or create the GitHub repository to match the new version file URLs.

## Restored bundled scale exponent definitions (0.0.4.70)

- Restored `GameData/StretchScale/Patches/ScaleExponents.cfg` unchanged from 0.0.4.68, including its original attribution and license header.
- The bundled `TWEAKSCALEEXPONENTS` and `TWEAKSCALEBEHAVIOR` definitions apply only when TweakScale is absent (`:NEEDS[!TweakScale]`).
- Retained the single `Patches/StretchScale.cfg` installation patch from 0.0.4.69, adding `ModuleStretchScale` to all parts without duplication; PatchManager files remain removed.
- Updated source solution, README, and regression/manual checks. No production C# behavior changed.

## Single all-parts ModuleManager patch (0.0.4.69)

- Removed PatchManager integration and both PatchManager crewed-parts files.
- Replaced the two uncrewed-only installation rules with one `@PART[*]`
  ModuleManager patch that adds `ModuleStretchScale` to **all parts**, including
  parts with positive `CrewCapacity`, while skipping parts where it is already
  present. No PatchManager opt-in or separate crewed-parts patch is necessary.
- Removed the separately bundled `ScaleExponents.cfg` patch so the mod ships
  only the all-parts module-injection patch. The code still supports external
  `TWEAKSCALEEXPONENTS` rules supplied by other mods, but no longer provides
  its own fallback stock-module exponent definitions when TweakScale is absent.
- Updated README, solution entries and release tests. Historical changelog
  entries and old test-plan documents remain for reference.
- For existing KSP installations, remove the old StretchScale `PatchManager`
  folder and `Patches/ScaleExponents.cfg` as part of the upgrade; merely copying
  a new GameData folder over the old one does not remove obsolete files.

## Stage delta-v mass stability and README replacement (0.0.4.68)

- Fix the mass modifier used by KSP's stage delta-v simulator: calculate the
  correction relative to the `defaultMass` supplied to each `GetModuleMass`
  query instead of subtracting the live `part.mass`. A staging snapshot may
  supply a different base, and using the live mass could produce alternating
  stage delta-v figures even when the vessel is stationary.
- Keep dry mass anchored to the prefab, and preserve the independent TweakScale,
  B9PartSwitch, and other fuel-switcher corrections; no resource amounts or
  capacities are changed by this fix.
- Add source-level and C# production-math regression tests for repeated
  stage mass queries with varying base masses.
- Replace `README.md` with the user-supplied README (including its original
  `ThisMod` placeholders), without editing its contents.

## Resource-only B9 Part Switch supports face morphing (0.0.4.67)

- Allow face morphing on parts whose B9PartSwitch instances only change resource
  tank type, capacity, mass/cost or subtype UI settings (for example Streamline
  Engines Tanks' DTE OX-50-61 mini oxidizer tank with `moduleID = fuelSwitch`).
- Read each B9 switcher's final ModuleManager-resolved `PART/MODULE` config,
  match by ordinal and moduleID, and inspect **all** configured subtypes.
- Remain conservative: geometry/node/material/texture/module-changing B9
  subtypes, unrecognized fields/nodes, or missing config remain unsupported;
  show the specific rejected key/node in the Morph gizmo explanation.
- Preserve existing B9 baseVolume/resource handling, non-compounding morph
  geometry and all editor tool behavior. No B9 DLL reference added.

## Face-morph handles on more part shapes / clearer diagnostics (0.0.4.66)

- Prefer the KSP editor camera for face-morph projection even when a part uses a
  nonstandard rendering layer or its bounding-box center is outside the view.
  The eight handles are tested individually after projection, so a long or
  offset part no longer loses all controls just because its center is offscreen.
- When clicking a part that cannot be morphed, show a visible reason naming the
  part (missing StretchScale module, edit restriction, unsupported mesh or
  collider, animation/switchable geometry). Also log it to KSP.log.
- When the camera cannot project any corners in front of it, show a camera
  orientation hint instead of leaving an unexplained blank tool.
- Preserve all geometry/collider safety requirements; this update does not
  enable face morphing on arbitrary incompatible parts.

## Always-visible Morph and Stretch handles (0.0.4.65)

- Restored screen-space IMGUI face-morph handles and cage on every editor repaint.
  The experimental independent screen Canvas in 0.0.4.64 could be created without
  actually drawing anything; its existence incorrectly suppressed the fallback.
- Added foreground screen-space axis handles and stem lines to the Stretch gizmo,
  so attached parts do not obscure drag targets.
- Render both from the editor addon's OnGUI callback instead of requiring a
  selected part's OnGUI callback. The overlays are visual only and cannot block
  clicks; existing camera projection and hit testing continue unchanged.
- Preserved all scaling/morph modes, toolbar positions and persisted values.

## Morph handles visible through attached parts (0.0.4.64)

The VAB/SPH face-morph cage now also renders in a non-interactive, high-priority
screen-space Unity UI overlay. This prevents a tank or other part attached above
or below the selected part from covering its orange/cyan corner handles and cage
lines. Positions match the editor camera projections used for picking and drag
operations; the original world-space handles remain for 3D perspective, and the
older IMGUI screen-space cage remains available if canvas setup fails.

No UI raycaster is added and all handle images have raycasts disabled, so
KSP's stock editor clicks, PAW and mouse-drag input remain managed by the
existing editor gizmo hit tests. The overlay is removed on gizmo exit or scene
change. No geometry, attach-node, morphing, or craft-persistence logic changed.

## Smaller editor toolbar buttons (0.0.4.58)

The fifth **Stretch** and sixth **Morph** editor toolbar buttons are now
**28 × 28 KSP UI units** (previously 32 × 32), while continuing to scale
with the editor UI. The icon covers the entire button without padding.
Positioning uses the new button width, so the two custom buttons remain
flush against each other and the right edge of the fourth stock button
without a gap. Grey backgrounds and active-state highlights are unchanged.
This only affects VAB/SPH editor tool presentation and hit targets.

## Editor toolbar button padding removal (0.0.4.57)

The fifth **Stretch** and sixth **Morph** editor toolbar buttons have no
additional icon insets: each transparent PNG fills its button's entire
`RectTransform` instead of leaving four pixels of padding on every edge.
The extra five-pixel gaps between the fourth stock button, Stretch and Morph
are also removed, so all three buttons are immediately adjacent. The button
backgrounds remain KSP-like grey when inactive and green/cyan when active.
Only VAB/SPH toolbar layout is affected; gizmo operation, face morphing and
saved craft data are unchanged.

## Editor toolbar background fix (0.0.4.56)

In the VAB/SPH, the fifth **Stretch** and sixth **Morph** tool buttons now
render their transparent PNG icon pixels over a grey button background instead
of a default white Unity `Image`. Selected buttons still use the green/cyan
highlights. The change is limited to these two editor toolbar buttons; it does
not change the gizmo operations or saved craft data. In-game rendering must
still be checked against KSP's stock tool strip.

## Changes in 0.0.4.45

- Changed `ScalePresetEditor.cs` Delete action to initialize `string error = "";`.
- Moved the distributed settings file into `GameData/StretchScale/PluginData/Settings.cfg` and load it directly using `ConfigNode.Load`; `UserSettings.cfg` remains a separate user-preference override.
- Added **Show absolute** and **Show relative** filters in **Browse presets**. The window defaults to relative-only, but either or both modes can be shown; duplicate names remain distinguished by mode. Filters update on the next IMGUI Layout pass.
- Added a readout-only **Custom** choice to the PAW absolute-preset selector. Manual X/Y/Z changes, gizmo edits, node-matching changes, external scale adjustments and relative preset applications switch the display to Custom. Named absolute preset selections restore their name when applied without clipping, and **Reset scale** restores `1x uniform`.
- Relative presets still remain repeatable using **Repeat preset**, which retains their exact name and mode independently of the PAW display.

## Optional fuel-switcher integration (0.0.4.39)

StretchScale now recognizes the installed `ModuleFuelTanks` (ModularFuelTanks /
RealFuels), `FSfuelSwitch` (Firespitter), and `InterstellarFuelSwitch`
PartModules without adding any hard assembly dependencies. These modules may
rebuild or reuse `PartResource` objects when the tank type or selected setup
changes. The compatibility adapter records the switcher identity plus selected
setup, waits until the next frame after a setup change, and separately persists
native resource-capacity baselines in each craft under
`STRETCHSCALE_FUEL_SWITCH_RESOURCE` (inside `ModuleStretchScale`). This
prevents an already stretched tank from becoming the native baseline when
switching from A to B and back. Capacities and resource fill fractions continue
to use the existing absolute scaling path. The adapter also adds the **extra**
StretchScale mass/cost delta for the switchers' `IPartMassModifier` and
`IPartCostModifier` values (KSP adds the native modifier itself), and lists
selected setups and modifiers in the inspector.

**Scope and limitations:** ModularFuelTanks retains responsibility for its own
internal tank-volume accounting and fuel allocation UI; this release adjusts
PartResource capacities but does not invoke a private MFT tank-volume update
API. This may cause MFT's reported free/used tank volume to disagree with the
external capacity adjustment on some versions/configurations. Firespitter and
IFS retain responsibility for switching fuels and meshes. Behavior with
RealFuels, fuel boiloff, tank upgraders, and complex mixed-switcher parts
requires **in-game verification**; this is not a guarantee of full compatibility.

In VAB/SPH, test A -> B -> A at 1x and 1.5x, save/reload, and repeat with
partially filled tanks and unchanged resource names. Check MFT's own tank UI
and KSP mass/cost readouts in addition to the StretchScale inspector.

## Gizmo reach and cursor tracking (0.0.4.38)

The three editor scaling handles now extend farther from the part: the extra
stem-distance multiplier is **3x** (formerly 1.5x). Handle cubes and click
radius remain unchanged.

Dragging a handle now drives the **rendered handle toward the mouse** instead of
converting the mouse's total drag distance using a fixed sensitivity based on
the stem length. The implementation samples the handle's screen-space motion
for a unit of part scaling, preserves the grab offset when clicking near the
edge of the cube, and updates the geometry after the scaling operation. This
allows the handle to follow the pointer even as the tank grows and the camera
projection changes. No mouse-warping or UnityEditor dependency is used.

The existing Shift (0.05) snapping, Escape/right-click cancellation, editor
pick lock, min/max scaling and Career restrictions remain. The pointer can
separate from the handle when a limit is reached or when snapping, as expected.

**In-game check:** In the VAB, show the gizmo on an FL-T800, grab an axis handle
at its edge and drag it slowly to 1.5x and 2x, then back to 1x. The handle
should stay under the pointer as closely as allowed by camera projection.
Repeat with locked proportions, Shift snapping, and a low Career limit.

## Build fix (0.0.4.32)

Fixes a missing local `prefab` variable in `OnStartFinished()` when initializing specialized docking-port scaling. No scaling calculations changed.

# StretchScale

## Preset editor and configuration (0.0.4.35)

The **Browse presets** PAW button opens a draggable Presets window. The PAW
quick selector offers absolute presets and a display-only **Custom** entry.
The browser defaults to relative presets, with separate absolute/relative
visibility toggles; both can be enabled together. Select a preset and click **Apply
selected** to scale the part through the usual safety checks. Preset definitions load from
`GameData/StretchScale/PluginData/Presets.cfg` (`STRETCHSCALE_PRESETS` with `PRESET` children),
not a hardcoded switch statement. Sixteen shipped presets are included: eight absolute and eight relative, each with an explicit mode.
Each entry has `name`, `mode` (`absolute` or `relative`), and positive `x`, `y`,
`z` values. For `absolute`, values replace the three axes; for `relative`,
values multiply the part's current axes. Built-in Double Y and Half Y now use absolute scales, so they are repeatable without accumulating.

In VAB/SPH, open the **Vessel Inspector** from the ToolbarController button,
select a part with ModuleStretchScale, and click **Edit presets** above the part
list and values. The button is disabled when the selected part has no StretchScale
module and is not shown in flight. The former PAW **Edit scale presets** event
has been removed. The draggable ClickThroughBlocker-protected editor lists defaults and custom
presets. Use **New from current scale** to capture X/Y/Z, or **Duplicate
selected** to copy a default. Supply a unique name and values, choose absolute
or relative operation, and click **Save custom preset**. Custom entries can
be updated or deleted; shipped defaults are read-only but duplicable. **Apply
selected to part** uses the same CommitChange pipeline as the PAW, including
safety checks, symmetry, children, and Career Mode limits. **Reload files**
reloads the default and custom preset files directly from PluginData.

User-created presets are written to:

`GameData/StretchScale/PluginData/CustomPresets.cfg`

This writable file is not included with the release, so updating/reinstalling
the mod does not overwrite saved custom presets. It is deliberately stored
under PluginData to avoid ModuleManager processing. Custom **absolute** presets become
available in the PAW selector for all parts after saving; relative presets appear in the full browser. Preset data is
shared across saves; part scale values remain in their normal craft fields.
Changes to `PluginData/Presets.cfg` can be loaded via **Reload files** in the preset editor. Presets do not bypass configured scale restrictions.

Example file entry:

```cfg
STRETCHSCALE_PRESETS
{
    PRESET
    {
        name = Long fuel tank
        mode = absolute
        x = 1
        y = 2.5
        z = 1
    }
}
```

## Specialized docking-port scaling (0.0.4.31)

StretchScale now handles stock `ModuleDockingNode` capture geometry in addition to the
existing global `TWEAKSCALEEXPONENTS` force/torque scaling. The docking face is measured
across its transverse axes, so stretching the part *lengthwise* does not increase
its magnetic capture distance, whereas widening the face does. `acquireRange`,
`captureRange`, and `minDistanceToReEngage` are recalculated from their **prefab**
values, never multiplied from previously scaled values. Both StretchScale and an
existing uniform TweakScale factor contribute to the actual physical face size.

When `matchDockingPortTypes` is enabled, a **single-type stock port** can become
`size0`, `size1`, or `size2` only when its actual face diameter is within 5%
(configurable) of the relevant stock diameter (0.625, 1.25, or 2.5 m).
Intermediate sizes (for example 1.875 m) get a deterministic compatible string
such as `StretchScaleDock_1875mm`. This allows two genuinely matching StretchScale
ports to dock without falsely claiming compatibility with a physically different
stock docking port. Distinct native stock sizes that reach the same physical size
receive the same identifier. **Custom port types, multi-type ports, B9-managed
ports, and live docked/transitioning ports are not relabeled. Where B9 is
present, docking distance fields are also left to B9 until subtype-specific
docking baselines can be confirmed.** Gender, rotation,
and deployment rules are never changed. The new settings in `Settings.cfg` are:

```cfg
scaleDockingCaptureDistances = true
matchDockingPortTypes = true
dockingStockSizeTolerance = 0.05
```

The original global exponent rules remain responsible for
`acquireForce`, `acquireTorque`, and `undockEjectionForce`. StretchScale
changes the three geometric **distance** fields separately; avoid also
assigning custom exponent rules to those exact fields or the mechanisms
may compete. The inspector has a new **Docking** category reporting live
compatibility type, face multiplier, and capture/acquisition distances.

**Testing caveat:** The docking FSM, port transforms, and matching logic must be
validated in KSP against stock Junior/standard/Senior ports, saved crafts,
undocking, and docking while in flight. Arbitrary stretched sizes are only
compatible with ports of the same size/type; they do not become universal adapters.


**StretchScale** is a Kerbal Space Program 1 mod that lets you resize parts independently along their X, Y, and Z axes in the VAB or SPH. Make a tank longer without making it wider, flatten a structural part, or adjust a part's proportions to fit a craft.

Unlike a visual-only resize, StretchScale also adjusts the part's **dry mass, dry cost, and stored resource capacity** as its volume changes.

## Features

- **Independent X, Y, and Z scaling:** Change width, height, and depth separately.
- **Proportional scaling:** Enable **Lock proportions** to change all three axes together.
- **Mass and cost scaling:** Adjusts dry mass and dry part cost from the volume multiplier. The adjustment is calculated through KSP's mass/cost modifier interfaces; other mass/cost modifiers are taken into account.
- **Resource scaling:** Updates tank capacity and stored amounts while retaining the existing fill percentage. Tracks each resource instance and notices capacities replaced by tank-switching mods.
- **Attachment handling:** Resizes the model, colliders, and attachment nodes, including stack and surface attach-node **size tiers**, and repositions attached parts in the editor.
- **Symmetry support:** Applies scale changes to symmetry counterparts.
- **Stretch children:** Optionally scales attached descendant parts with their parent, including rotated parts that have StretchScale installed.
- **Stock engine FX and Waterfall:** Scales stock particle/legacy emitters and Waterfall visual meshes using the part's X/Y/Z axes. Effects parented beneath the resized `model` already inherit geometry scaling; effects instantiated outside it are placed under independent absolute-scale wrappers. Late-created effects are discovered in flight, and Waterfall is an optional soft dependency. Does not alter thrust, sound, Waterfall controllers or effect config nodes.
- **TweakScale coexistence:** Can apply per-axis stretching on top of an existing uniform TweakScale factor; integration is optional.
- **Live calculations:** The Part Action Window shows the stretch volume multiplier, calculated dry mass, dry cost, and tank capacities.
- **Scale presets:** The PAW **Scale preset** selector offers built-in and custom **absolute** presets only. A separate **Browse presets** button opens the complete catalog, including relative presets; Vessel Inspector **Edit presets** manages custom presets.
- **Event-driven resource refresh:** Changes announced with `OnPartResourceChanged` are processed on the next editor update; general editor ship changes use a single global listener and rescan only likely dynamic tanks. Silent fuel-switchers receive a staggered ten-second compatibility check, rather than an eight-frame poll across the craft.
- **Batched editor notifications:** Changes to a selected part, symmetry counterparts, and descendants queue a single `onEditorShipModified` notification per frame; StretchScale suppresses its own callback when delivering that notification.
- **Scaling safety:** Configurable maximum dry-mass and geometric-volume multipliers, read-only scale warning/status, and optional editing restrictions on wheels, docking ports, and animated parts.
- **Configurable controls:** Set the minimum and maximum scale and the UI adjustment increments.

## Career Mode progression

In **Career Mode**, new rescaling in the VAB is limited by the **VAB upgrade level**; in the SPH it is limited by the **SPH upgrade level**. This follows KSP's facility progression rather than a global "career level." Upgrading the corresponding building unlocks a greater resizing range. Levels above 3 (for example with facility-level mods) use the tier-3 configuration. **Science and Sandbox** games ignore these career restrictions.

Default limits (change them in `GameData/StretchScale/PluginData/Settings.cfg`):

| Facility level | Each axis minimum | Each axis maximum | Stretch volume max (`X*Y*Z`) |
| --- | ---: | ---: | ---: |
| 1 (starter) | 0.75× | 1.5× | 3× |
| 2 | 0.5× | 3× | 27× |
| 3+ | 0.25× | 10× | 1,000× |

The `Career scale limits` line in the PAW shows the current VAB/SPH tier. New edits through the axis controls, presets, symmetry, and **Stretch children** are limited; global `maxMassMultiplier` / `maxVolumeMultiplier` continue to apply. A requested change that exceeds an allowed range is clamped where possible and reported under **Scale safety**. Earlier crafts with scales beyond the current career limit **retain those dimensions when loaded** and can be scaled back toward the permitted range without being forcibly reset. Upgrading facilities takes effect when you next edit in that building.

Set `enableCareerLimits = false` to disable Career restrictions. The options are `careerLevel1MinScale`, `careerLevel1MaxScale`, `careerLevel1MaxVolume`, and the corresponding fields for levels 2 and 3. If you disable facility upgrades entirely, StretchScale uses the highest tier. Mass and geometric-volume global safety limits are independent of progression.

## Automatic stack-node matching (0.0.4.29)

**Match node sizes** is enabled by default and can be toggled in the PAW on
any StretchScale part; the toggle is shared by all parts during this editor
session. To change the default set `matchNodeSizesDefault = false` in
`GameData/StretchScale/PluginData/Settings.cfg`.

When a part with StretchScale is newly attached by a **stack node** to another
part, the newly attached part is resized to fit the destination node's
cross-section. For cardinal stack normals (top/bottom/side), only the two axes
across the joint are changed, leaving its length unchanged. Diagonal node
normals use a uniform resize. The physical diameter is calculated from the
original node and its X/Y/Z stretch, rather than using the rounded integer
`AttachNode.size` tier, for better matching at e.g. 1.5x.

This feature does not resize the parent, does not match surface attachments,
does not change craft on load, and respects Career building limits, scaling
restrictions, existing symmetry rules, and the global mass/volume safety caps.
A new connection is detected by observing a parent transition; the code checks
for valid connected stack nodes over several editor frames. If another mod
reattaches a part without changing its parent, automatic matching will not run.

## Automated regression tests (0.0.4.29)

Run `python Tests/run_regressions.py` from the repository root. This executes
static integration checks and, when the **.NET 8 SDK** is installed, a
self-contained C# console regression runner. Run with `--require-dotnet` in CI
to require the executable math suite and fail if dotnet is unavailable.

The C# regression project directly links the production `ScaleMath.cs` source,
so it tests the same exponent, volume, node diameter/tier and match-factor
calculations used by StretchScale. No KSP or Unity DLLs, NuGet packages, or
third-party test frameworks are required for the math tests. Detailed editor,
B9PartSwitch, Waterfall and scene-switch behavior still requires in-game testing
(see `Tests/AUTO_NODE_MATCH_0.0.4.29.md`).

## Part Action Window defaults and attach nodes

- **Debug build** (Visual Studio `Debug`, with `DEBUG` defined): **Stretch Scale** starts **expanded** in the Part Action Window.
- **Release build** (Visual Studio `Release`): **Stretch Scale** starts **collapsed**. Either build allows you to manually expand/collapse the group afterward.
- Stack and surface attach-node **sizes** now change when the part's cross-section changes. Their size is recalculated from the part's prefab on every rescale to avoid cumulative rounding. The available KSP node sizes are **integer tiers**, so small dimension changes may not cause an immediate visible size change.
- The size is based on the effective area *perpendicular* to the node's attachment direction. Stretching a vertical tank only along Y moves its top and bottom nodes without enlarging them; widening X/Z can enlarge those nodes. Uniform TweakScale scaling is included in the size calculation.

## How to use

1. In the **VAB** or **SPH**, place a part that has the `ModuleStretchScale` module.
2. Right-click the part to open its **Part Action Window**.
3. Expand the **Stretch Scale** section.
4. Adjust **Scale X (width)**, **Scale Y (height)**, and **Scale Z (depth)**.
5. Use **Lock proportions** if you want the axes to change together.
6. Use the PAW **Scale preset** selector for absolute presets (including custom absolute presets). Click **Browse presets** to open the smaller full-list window for both absolute and relative presets; select an entry and click **Apply selected**. **Repeat preset** repeats the last preset applied, including relative ones, while the PAW selector shows **Custom** after a relative preset or direct axis edit. Built-in Double Y always sets X/Y/Z to 1/2/1 and Half Y to 1/0.5/1; custom relative presets multiply current axes.
7. Watch the read-only **Volume multiplier**, **Dry mass**, **Dry cost**, and **Tank capacity** values update.
8. Turn on **Stretch children** if attached descendant parts should stretch with the selected part. This setting is shared across parts during the current session.

Two additional actions are available:

- **Reset scale** returns all three axes to `1.0` and sets the last-used preset to **1x uniform** (including symmetry counterparts).
- **Swap X / Z** exchanges the X and Z scale values.

### How mass and storage change

StretchScale uses the volume multiplier:

`volume multiplier = scaleX * scaleY * scaleZ`

For example, setting X = 1, Y = 2, and Z = 1 doubles the part's volume. Its dry mass, dry cost, and resource capacity are correspondingly doubled. A half-full tank remains half-full, but now contains twice the amount of resource it did before the resize.

StretchScale adds only its own **non-uniform multiplier** on top of the size/capacity calculated by other modules such as TweakScale. The mass/cost interface returns the **additional** dry mass/cost, leaving KSP to combine module modifiers. This avoids directly changing the part's base mass or cost. KSP calculates propellant mass and resource cost separately from dry mass/cost.

The capacity display lists up to three resources; other resources are still scaled. Dry mass and dry cost are estimates based on the active modifiers and do not include stored propellants.

## Applying StretchScale to parts

The included `GameData/StretchScale/Patches/StretchScale.cfg` is a **ModuleManager** patch that adds StretchScale to parts with no crew capacity. It includes parts that omit `CrewCapacity` and parts where `CrewCapacity = 0`; it excludes parts with positive crew capacity. It also avoids adding the module twice.

The patch requires **ModuleManager** and the StretchScale mod. Parts must have a compatible `model` transform for StretchScale to work; the module hides its controls if it cannot find the required model or part prefab.

### Optional crewed-part patch (PatchManager, 0.0.4.33)

By default, StretchScale only attaches to uncrewed parts. To enable scaling on **crewed parts** (command pods, cockpits and modded parts with `CrewCapacity > 0`), install the **KSP1 PatchManager** mod and enable **StretchScale → Enable Crewed Parts** in its in-game window. Click **Apply All** and restart KSP so ModuleManager loads the newly active patch. Disable the same entry and apply/restart to remove the optional injection for future loads.

The PatchManager definition is at `GameData/StretchScale/PatchManager/PM_CrewedParts.cfg`; the dormant ModuleManager patch is at `GameData/StretchScale/PatchManager/PluginData/CrewedParts.cfg`. It is **off by default** (`installedWithMod = false`), and only patches parts with positive `CrewCapacity` which do not already contain `ModuleStretchScale`. The default `Patches/StretchScale.cfg` continues to cover parts with zero/missing crew capacity. PatchManager is optional; no PatchManager DLL reference is necessary for StretchScale itself.

**Caution:** Scaling crewed parts (IVAs, hatches, EVA exits, animations, docking features and crew capacity) can have KSP limitations. Make a craft backup and test IVA/hatch/EVA access before relying on this option in a save. Turning the patch off changes the part definition only after restarting the game and may affect existing craft saved with crewed StretchScale modules.


## Configuration

StretchScale reads an optional `STRETCHSCALE_SETTINGS` node from a `.cfg` file under `GameData`. For example:

```cfg
STRETCHSCALE_SETTINGS
{
    minScale = 0.05
    maxScale = 100
    incrementLarge = 0.25
    incrementSmall = 0.05
    incrementSlide = 0.001
    scaleChildrenDefault = false
    maxMassMultiplier = 10000
    maxVolumeMultiplier = 10000
    warnMassMultiplier = 100
    warnVolumeMultiplier = 100
    enableCareerLimits = true
    careerLevel1MinScale = 0.75
    careerLevel1MaxScale = 1.5
    careerLevel1MaxVolume = 3
    careerLevel2MinScale = 0.5
    careerLevel2MaxScale = 3
    careerLevel2MaxVolume = 27
    careerLevel3MinScale = 0.25
    careerLevel3MaxScale = 10
    careerLevel3MaxVolume = 1000
    restrictWheels = false
    restrictDockingPorts = false
    restrictAnimatedParts = false
    resourceFallbackSeconds = 10
    checkAllResourcesPeriodically = false
}
```

| Setting | Purpose |
| --- | --- |
| `minScale` | Smallest allowed value for an individual axis |
| `maxScale` | Largest allowed value for an individual axis |
| `incrementLarge` | Large UI adjustment increment |
| `incrementSmall` | Small UI adjustment increment |
| `incrementSlide` | Fine slider adjustment increment |
| `scaleChildrenDefault` | Initial state of the shared **Stretch children** toggle |
| `maxMassMultiplier` | Maximum additional dry-mass factor `X × Y × Z` when editing |
| `maxVolumeMultiplier` | Maximum total geometric volume factor, including TweakScale's uniform scale cubed |
| `warnMassMultiplier` | Warn at or beyond this stretch mass factor, or below its reciprocal |
| `warnVolumeMultiplier` | Warn at or beyond this total volume factor, or below its reciprocal |
| `enableCareerLimits` | Enable facility-upgrade tier limits on new scaling in Career Mode |
| `careerLevel[1-3]MinScale` | Minimum individual axis for each facility level |
| `careerLevel[1-3]MaxScale` | Maximum individual axis for each facility level |
| `careerLevel[1-3]MaxVolume` | Maximum additional stretch volume multiplier per tier |
| `restrictWheels` | Disable *new* editor scaling when a wheel module is present |
| `restrictDockingPorts` | Disable *new* editor scaling on docking port modules |
| `restrictAnimatedParts` | Disable *new* editor scaling on common animation/deployable modules |
| `resourceFallbackSeconds` | Interval for silent tank-switcher compatibility scans (minimum 1 second) |
| `checkAllResourcesPeriodically` | Opt-in periodic fallback on ordinary tanks, too; off by default |

This configuration is included at `GameData/StretchScale/PluginData/Settings.cfg` and loaded directly with `ConfigNode.Load` (PluginData is not indexed by GameDatabase). The example shows the code's built-in defaults. Individual axis scales are stored with the craft; **Stretch children** is a session-wide toggle rather than a per-part saved setting.

The **Scale safety** readout reports extreme sizes and restrictions. If an edit would exceed either multiplier cap, StretchScale limits the requested axes (preserving the other two for a simple single-axis edit where possible). Restriction toggles block new editor resizing on recognized modules, but preserve scales saved in existing craft files. `warn*Multiplier` thresholds warn on unusually large **or small** factors. Limits are applied to new edits, not retroactively on loading an existing craft; resizing with TweakScale itself is not governed by these StretchScale edit limits.

**Recognition rules:** wheel names beginning `ModuleWheel` or `KSPWheel`; docking modules containing `DockingNode`; and animation names beginning `ModuleAnimate`, `ModuleAnimation`, or `ModuleDeployable`. These rules are intentionally conservative: other modded modules may need explicit support. All three restrictions default to `false`.

## Notes and limitations

- Designed for **KSP 1.12.x** and its VAB/SPH editors.
- StretchScale changes dry mass, dry part cost and stored resources, but **does not change engine thrust or other module statistics**.
- Resource handling targets KSP `PartResource` storage. When another module replaces a resource or writes a new capacity, StretchScale treats that capacity as the new native capacity and reapplies the additional stretch multiplier. Some specialized tank systems (RealFuels, B9PartSwitch, Modular Fuel Tanks, Interstellar Fuel Switch) may require tailored adapters; they are **not claimed fully compatible**.
- Saved crafts now include per-resource native capacity metadata (`STRETCHSCALE_RESOURCE` nodes) to improve restore reliability. Older craft files without metadata use fallback detection. Back up crafts when updating from an earlier release.
- A delayed change from another tank mod may temporarily override the displayed capacity until that mod sends `OnPartResourceChanged` or the next compatibility check (10 seconds by default on recognized switchers). Enable `checkAllResourcesPeriodically` to include unrecognized, silent modules at the cost of additional scans.
- The mass/cost readouts display **dry** values, not mass/cost of the propellants. KSP adds those separately.
- **Stretch children** only propagates through descendant parts that also have a working `ModuleStretchScale` module; it stops at an unsupported part.
- Extreme or non-uniform scales may produce unusual results for animations, FX, docking ports, wheels, and other systems that assume stock part dimensions.
- Back up important craft and saves before experimenting with substantial rescaling.

## Verification

The included `Tests/SAFETY_AND_EVENTS_0.0.4.0.md` provides manual regression tests for resource events, deferred editor notifications, volume/mass caps, module restrictions, and saved crafts. The source has not been compiled against a local KSP installation or tested in game.

## Dependencies

- **Kerbal Space Program 1** (targeted at 1.12.x)
- **ModuleManager** (for the included automatic part patch)

**Optional:** TweakScale, for using uniform scaling alongside StretchScale.


### 0.0.4.1 editor event registration fix

Fixes a `NullReferenceException` on `GameEvents.onEditorShipModified.Add` caused by
passing a static callback to KSP's `EventData` system. One persistent instance
listener now forwards events to StretchScale's shared handler. The subscription
is retried after editor initialization if necessary, and is removed when the
last StretchScale part is destroyed. Event batching and deferred resource checks
are unchanged.

### 0.0.4.3 Career Mode limits

Restricts new scaling based on the current editor's VAB or SPH upgrade tier, with configurable axis ranges and stretch-volume maximums. Existing saved dimensions remain intact. Career limits are enabled by default and can be disabled in `Settings.cfg`. A PAW field displays the current building's permitted range.

### Vessel Inspector (0.0.4.24)

The **Scaled parts only** filter is enabled by default when opening a new
Vessel Inspector. You can turn it off to include unscaled parts. Hover over
any part in the left list to highlight that same physical part on the craft
(VAB/SPH) or active vessel (flight); the highlighting clears on leaving the
row, hiding/filtering that part, or closing the inspector. The original
highlight state, color, and highlighting mode are restored on exit. Toolbar
registration now advertises VAB, SPH, FLIGHT and MAPVIEW explicitly.

### 0.0.4.25 — Complete cell-value tooltips

The PAW scaled-values window and toolbar Vessel Inspector now use a
full-precision hover tooltip for original/current fields. Tooltips are
rendered above the panes and can scroll when a property (such as a FloatCurve)
contains more text than fits inside the window. The Copy report action also
exports full-precision numeric values instead of the shortened table display.

### 0.0.4.26 — Consolidated row tooltips

Hovering over any cell in the scaled-values report now shows the property's
name, original value, current value, and notes together in a single tooltip.
Numeric values retain full precision where the table display is shortened.
Both the PAW scaled-values window and the Vessel Inspector use this behavior.

## B9 Part Switch compatibility (0.0.4.27)

StretchScale detects `ModuleB9PartSwitch` modules automatically. B9 Part Switch is
**optional**; no reference or runtime dependency on its DLL is added. When it is
installed, StretchScale tracks the selected `currentSubtype` of every B9 switcher
on a part (including independent `moduleID` switchers).

- Tank resources follow the B9-selected type, including B9 changes that replace
  existing resource objects or alter the capacity of an existing resource. The
  original capacities are cached **per subtype combination** and saved in the
  craft as `STRETCHSCALE_B9_RESOURCE` nodes. The StretchScale capacity exponent
  is applied to those native capacities, never to an already scaled value.
- B9's own `IPartMassModifier` and `IPartCostModifier` contributions are retained.
  StretchScale supplies only the additional delta required by its mass/cost
  exponent, without editing the B9 module or its tank definitions. The vessel
  inspector shows the native and scaled B9 subtype modifiers.
- Switching a subtype that replaces module fields causes StretchScale to rebase
  **only the fields that B9 changed**, before reapplying exponents. Ordinary
  scale changes never rebase their native values.
- If B9 changes attachment nodes, StretchScale captures their positions and
  node sizes for the active subtype and persists native node baselines in
  `STRETCHSCALE_B9_NODE` nodes. It remains the user's responsibility to test
  B9 variants with unusual attach-node / model-transform arrangements.
- The selected subtype is checked automatically in editor and flight and changes
  are handled on a later frame so B9 has time to finish replacing resources.

**Compatibility notes:** B9's internal `baseVolume` and subtype fields are not
scaled directly, preventing double-scaling. B9 resources are managed by the
existing StretchScale resource path, based on the active B9 subtype. The
integration does not reproduce B9's own TweakScale hooks or engine-effect
resizing. Other fuel switchers installed alongside B9 may disable B9's resource
switching (as documented by B9). See `Tests/B9_PARTSWITCH_0.0.4.27.md` for
in-game checks, including craft reload, symmetry, multiple B9 modules and
switching already-scaled tanks.

## Engine FX and optional Waterfall scaling (0.0.4.28)

FX follow the same three non-uniform stretch axes used for the physical model. Most stock
and Waterfall engine effects are parented to `thrustTransform` inside the model, so their
size and offsets change automatically with `model.localScale`. Scaling them a second time
would produce incorrect effects; StretchScale explicitly excludes those from its FX pass.

Some stock FX systems and Waterfall-rendered meshes can be instantiated elsewhere under
the part. StretchScale detects `ParticleSystem`, `KSPParticleEmitter` and renderers using
Waterfall shaders outside the resized model, and wraps the emitted object in an identity
parent transform which receives the **absolute** X/Y/Z stretch. Existing particle and
Waterfall animation/controller transforms are left intact. The module checks for newly
spawned or rebuilt effects every `effectRefreshSeconds` seconds (default 2). Other parts
on the vessel are excluded. Waterfall is detected by the `ModuleWaterfallFX` module name;
**Waterfall.dll is not a required reference**.

Options in `GameData/StretchScale/PluginData/Settings.cfg`:

```
scaleEngineEffects = true
scaleWaterfallEffects = true
effectRefreshSeconds = 2
```

Limitations: This is **visual transform scaling** for effects belonging to the part;
custom FX instantiated in the world, without a transform under that part, are not
tracked. Waterfall shader/controller settings, brightness, lighting distances, emission
rates and audio are not changed. Specialized effect mods may require a separate adapter.
Use the stock and Waterfall engine regression checklist in `Tests/` to validate engine
plumes across repeated resize / reload / switching sequences.


## Scaling gizmo (0.0.4.30)

In the **VAB or SPH**, open a part's action window and click **Show scaling gizmo**
inside the **Stretch Scale** group. Three colored handles appear at the part:
**red = local X**, **green = local Y**, **blue = local Z**. Drag the cube at
an axis end to change only that axis. The current scale is displayed beside
all three handles; **Lock proportions** makes any handle scale all three axes.

- Hold **Shift** to snap to 0.05 increments relative to the starting value.
- Press **Esc** or right-click during a drag to return to the starting scale.
- Click **Hide scaling gizmo**, or open the gizmo on another part, to close it.
- The gizmo handle stems extend farther beyond the visible part bounds for easier picking.
- Only the currently active gizmo temporarily locks editor picking when the
  pointer is over a handle or a handle is being dragged. It does not modify
  KSP editor construction-mode bindings or the stock offset/rotation tools.
- All changes go through the same safety, career, child-part, symmetry and
  resource/mass/exponent update path as the PAW axis controls. Values are
  recomputed from original scaling baselines, not from preceding mouse moves.
- The gizmo is editor-only; its enabled state is session-local and is not
  persisted in craft files. Visuals are ordinary Unity runtime objects
  (no UnityEditor assembly or additional mod dependency).

The gizmo has not yet been validated in KSP itself. In particular, test handle
picking against stock offset/rotation controls and installed editor mods.

## Craft reload stack connection fix (0.0.4.36)

On some editor craft loads KSP rebuilds stack connections using unscaled prefab
attachment nodes before StretchScale restores the modified node positions. For
example, an FL-T800 stretched to 1.5x may reload with the Mk1 pod intersecting
the tank. After module initialization, StretchScale now performs **one** editor-
only, root-to-leaf alignment pass for stack joins touching a stretched part. It
translates the connected child subassembly; it does not alter scale, resources,
relative rotation, or node dimensions. Surface attachments are excluded.

New saves also include `STRETCHSCALE_STACK_GAP` values in the module's saved
configuration. These preserve intentional stack offsets on subsequent reloads.
Older crafts lack this information and assume flush-aligned stack nodes during
the first corrected load. The operation is not repeated when using the PAW,
gizmo or presets, and does not run in flight.

Test a Mk1 pod -> FL-T800 -> engine craft with 1.5x locked scaling, save it,
clear the editor, reload, then repeat for both 2x and an offset-gizmo edit.
The existing `.craft` should be backed up before testing.


### B9PartSwitch baseVolume integration (0.0.4.40)

For parts with `ModuleB9PartSwitch`, StretchScale multiplies each switcher's
`baseVolume` by **X × Y × Z** when a scale changes. The original, unscaled
volume for each switcher is retained in the part's `ModuleStretchScale`
`STRETCHSCALE_B9_BASE_VOLUME` records, rather than multiplying the live value.
The prefab baseline is used for older craft files that do not yet have records.
This works with multiple independent B9 switchers and subtype changes.

B9's public `UpdateVolume()` method is called in the editor after a volume
change, preserving the existing resource fill proportion. StretchScale does
not separately scale resources managed by B9; doing so would double the volume
multiplier. It also corrects its mass/cost modifier for the amount of native
B9 volume already reflected in B9's own calculations. In flight, StretchScale
updates the `baseVolume` field if necessary but does **not** call B9's
`UpdateVolume()`, because its flight implementation empties resources.
B9PartSwitch remains an optional dependency.

B9 subtypes that use only `volumeAdded`/`volumeAddedToParent` with a zero
`baseVolume` remain subject to B9's native behavior. New and old craft files
should be backed up before testing this change in-game.

### Vessel Inspector settings (0.0.4.41)

Click **Settings** on the Vessel Inspector to open a separate ClickThroughBlocker settings window.
**Match node sizes by default** starts **off** for fresh installs. The checkbox is saved
with **Save** to `GameData/StretchScale/PluginData/UserSettings.cfg`; this file overrides
`matchNodeSizesDefault` in `Settings.cfg` on future launches, so upgrades preserve it.
The existing **Match node sizes** PAW checkbox remains a shared session toggle.
Saving a new default updates the current session immediately.

Built-in **Double Y** and **Half Y** presets now use absolute axes `1/2/1` and
`1/0.5/1`, respectively; repeated use does not compound. Users can still define
custom relative presets in the preset editor to multiply current values.

### Settings and presets windows (0.0.4.42)

The Vessel Inspector **Settings** button offers three saved defaults:
**Match node sizes**, **Stretch children**, and **Lock proportions**. All three
start off. They are stored atomically under
`GameData/StretchScale/PluginData/UserSettings.cfg` so mod updates do not
reset them. The first two defaults also update the current shared editor
options; **Lock proportions** applies only to new parts without a saved lock
value, preserving existing craft choices.

The PAW **Scale preset** selector has been restored and displays **only
absolute** presets (including custom absolute definitions). The separate
**Browse presets** button opens a narrower, draggable window (410 px instead
of 610 px) containing **all** presets, including relative ones, without a
second slider. Clicking an item and pressing **Apply selected** uses the
standard scaling pipeline. Relative presets never enter the PAW quick selector;
**Repeat preset** still repeats the most recently applied preset, including
relative ones. The Vessel Inspector **Edit presets** button manages custom
presets.

### Preset selector and compact browser (0.0.4.43)

The absolute-only PAW `UI_ChooseOption` selector is dynamically populated from
`Presets.cfg` and `CustomPresets.cfg`. The separate **Browse presets** event
opens a 410-pixel-wide full catalog without a slider. The last applied
relative preset and its mode are tracked for **Repeat preset**; using it does not
add a relative choice to the PAW control.

### Complete absolute and relative preset catalog (0.0.4.44)

Shipped definitions now live at `GameData/StretchScale/PluginData/Presets.cfg`
(and are loaded directly with `ConfigNode.Load`, not `GameDatabase`). The
configuration includes eight absolute plus eight relative versions of the
original preset names. The **Browse presets** window can list all 16 (plus
custom presets), distinguishes identically named entries with `[absolute]`
and `[relative]`, and applies the specific selected mode. The PAW quick
selector remains **absolute-only**. **Repeat preset** also remembers whether
the last command was relative or absolute. The Presets window height increased
by 24 px to 554 px; Settings is now 339 px tall. The preset editor likewise
identifies entries by name and mode so editing/deleting custom definitions
doesn't affect their same-name counterparts.

### Crewed parts and IVA seats (0.0.4.48)

With the optional PatchManager crewed-parts patch enabled, `CrewCapacity` now uses
`CrewCapacityScaling.cs` instead of the general-purpose exponent field writer.
It calculates the **requested** seat count from the original part's capacity
and the configured `TWEAKSCALEEXPONENTS { name = Part; CrewCapacity = ... }`
rule. It checks real `InternalSeat` definitions in the active IVA (including
seat-bearing IVA props), if available.

**Safety rule:** It will only advertise additional capacity when the IVA has
at least that many real seat definitions. If the IVA has fewer seats, or its
seat layout cannot be verified, it caps the value to the verified/original
capacity rather than creating overlapping seats, broken IVA portraits, or
unusable EVA occupants. For example, a stock Mk1 pod with one IVA seat
**stays a one-seat pod** even when the mathematical exponent requests four.
To actually increase usable seats, the part must provide a suitable custom
IVA containing the additional `InternalSeat` definitions and physical seat
transforms (or a compatible IVA replacement mod).

Existing crew are never removed or reseated; occupied pods keep their current
capacity until emptied in the editor. In flight, crew capacity is not modified.
The Scaled Values inspector shows requested capacity, actual capacity, real
IVA seat count and the fallback reason. Capacity changes queue a single
editor ship-modified notification to refresh KSP's crew assignment UI.

This conservative integration does **not** generate new seat transforms,
portrait cameras, hatch locations or crew-transfer animations. Verify IVA and
crew assignments with the intended internal configuration before launching.


### Fifth editor tool: Stretch (0.0.4.49)

In the VAB or SPH, StretchScale adds a **Stretch** tool immediately to the right of
KSP's Place, Offset, Rotate, and Re-root tools. Select Stretch and click an
attached part with ModuleStretchScale to display its existing three-axis gizmo.
Click a different part to move the gizmo. Drag X/Y/Z handles to scale via the
normal safety/geometry/mass/resources pipeline. A temporary editor pickup lock
prevents ordinary part detachment. To exit Stretch Mode, click the Stretch tool
again, select any stock editor tool, use hotkeys 1-4, or press Esc when not dragging.
A handle drag can be canceled with Esc without leaving the mode.

The fifth button follows the native editor tool strip's screen position and
uses the existing StretchScale toolbar icon. It is editor-only, does not replace
any stock tool, and is removed on scene exit. The original PAW gizmo toggle
remains supported. The extra UI button is discovered from KSP's native
EditorToolsUI at runtime, so appearance/placement should be verified in both
VAB and SPH, including with editor UI-scaling mods.

### Face-based morphing (0.0.4.50)

The fifth VAB/SPH **Stretch** editor tool now supports **Face morph**. Press **M**
(or click its on-screen mode switch) after entering Stretch Mode, then select a
supported part. Four linked corners mark each of the upper and lower Y faces.
Drag a corner to resize the entire face in X and Z, creating a taper. Hold Alt
while dragging a corner to **tilt that face** (a planar shear), Shift for
0.05-step snapping, and Esc/right-click to cancel. Use the PAW **Show face morph
handles** button as another way to open the face handles. Morph values are saved
with the craft and replayed from native mesh snapshots in editor and flight.

Static parts with readable visual meshes and MeshColliders or standard stock
BoxCollider, SphereCollider and CapsuleCollider collision geometry are supported.
When a primitive collider's part is morphed, StretchScale substitutes a private
convex, low-poly MeshCollider matching the original shape and restores the
original collider at the default (unmorphed) face shape. Incompatible colliders,
unreadable/skinned visual meshes, and animated or B9-switched geometry are
still excluded. The editor shows an explanatory message instead of silently
failing to display handles. Vertex and collider meshes are cloned
per part, never changed on the prefab. See
`Tests/FACE_MORPH_0.0.4.50.md` for validation and limitations.

### Face morph stock-tank compatibility (0.0.4.52)

Face handles were previously hidden on many stock fuel tanks because they use
primitive colliders rather than a convex MeshCollider. This update supports
BoxCollider, SphereCollider and CapsuleCollider using instance-only collision
proxies, and accepts stock nonconvex MeshCollider layouts without automatically
rewriting their convex flag. The original collider returns when a morph is reset
or the part is unloaded. The result still requires in-game validation against
Unity PhysX convex-cooking limits, flight collision behavior and craft reloads.


### Dedicated Morph editor tool (0.0.4.53)

The VAB/SPH toolbar now adds **two** buttons after the stock four: **Stretch** (axis scaling) and **Morph** (end-face deformation). Select Morph, then click a compatible StretchScale part to show eight corner handles. Orange corners resize/tilt the bottom end face; cyan corners control the top end face. Drag for taper, Alt-drag for tilt, Shift for 0.05 snapping; Esc/right click cancels a drag. Switch tools using their toolbar buttons (or M). Clicking the selected tool again exits. The existing PAW **Show face morph handles** command remains available. Morph restrictions, data persistence, and the face-morph math are unchanged.


### Morph face linking and snapping (0.0.4.54)

In the VAB/SPH select the sixth editor tool (Morph) and click a compatible part.
- Drag a corner: change that end face's X/Z dimensions (taper).
- Alt+drag: tilt the planar end face.
- Shift+drag: snap the resize increment.
- **Shift+Alt+drag:** translate all four corners of the selected end face together in the local X/Z plane. The face retains its width, depth, tilt and Y position. This overrides the older Alt+Shift tilt-snapping gesture.
- The Shift+Alt translation is snapped to the selected increment while Shift is held. The orange and cyan faces move independently, with changes smoothly interpolated between them.
- In Vessel Inspector → Settings choose the **Face morph snap increment**: **0.01, 0.05 (default), 0.10, 0.25**. Click Save to persist it in `GameData/StretchScale/PluginData/UserSettings.cfg`. It applies to the next and current face-morph drags.
- Offset values are normalized to the part's original half-width/half-depth and saved per craft as `morphBottomOffsetX`, `morphBottomOffsetZ`, `morphTopOffsetX`, `morphTopOffsetZ`. Existing craft files default to zero offsets. The Reset face morph PAW action resets offsets as well.
- Esc/right-click cancels a drag and restores the original values.

### Radially linked face-morph corners (0.0.4.55)

With the sixth Morph editor tool active, **hold Ctrl+Alt BEFORE clicking** a
corner and drag it toward or away from the center of that end face. All four
corners of **that face** move radially inward or outward around the face's
center, using ONE uniform factor for width and depth. The original X:Z aspect
ratio stays constant; face offset/position, tilt, and Y height are preserved.
The opposite face is unchanged, and deformation interpolates through the tank.
The inward/outward component of the mouse movement controls the radius;
movement perpendicular to that radial direction does not resize the face.

**Ctrl+Alt+Shift** on mouse-down activates the same radial mode with Shift
snapping enabled, using the current 0.01/0.05/0.10/0.25 setting from Vessel
Inspector → Settings. Snapping quantizes the common *scale factor* (from 1.0 at
mouse-down), not X and Z separately, to avoid distorting rectangular faces.
Release Shift during the gesture for continuous radial scaling; press Shift to
snap again. A drag cannot change modes until you release the mouse button.
The editor continues to support Alt tilt, Shift+Alt whole-face translation,
and standard taper drags. Esc or right-click restores the original shape.


## Morph gizmo help panel placement (0.0.4.60)

The face-morph instruction box now appears directly under the complete six-button
editor tool strip (Place, Offset, Rotate, Re-root, Stretch, Morph). Its left
edge is aligned with the leftmost stock tool, and its width ends at the right
edge of the Morph button rather than extending across the screen. The help
text wraps vertically within that width. The panel moves with the native
editor tools and respects KSP UI scaling. Face-morph rejection and camera
diagnostics use the same area. This is a UI-only change; morphing itself
is unchanged.

## Face morph attachment node normals (0.0.4.61)

- Attach nodes on tilted end faces now rotate to match the actual deformed
  face plane instead of remaining aligned to the original Y axis.
- Compute stack and surface node normals via the deformation Jacobian's
  cofactor (inverse-transpose) using immutable non-morphed node orientations.
- Restore prefab node orientations before each ordinary StretchScale update
  and face-morph rebase, preventing cumulative angular drift.
- No changes to the deformation controls, node positions, snapping, or
  attachment size calculations. Live editor connections still use the existing
  positional attachment-following behavior; this change updates the attach-node
  orientation field, not the orientations of already-connected child parts.

## Cloned face-morph geometry and attach-node alignment (0.0.4.62)

- Fixed VAB/SPH duplication of an already-morphed part: Unity copies the
  currently deformed visual and collision mesh into the new part. Reapplying
  the persisted morph to that copied mesh bent the face a second time even
  though the attach-node normal was rebuilt only once. The result looked like
  the cloned node leaned upward relative to the face.
- Detect StretchScale-owned private morph meshes and rebuild the cloned
  visual and native mesh-collider geometry from matching prefab mesh data.
  Prefab transform lookup handles repeated sibling names.
- For tanks using primitive colliders, discard copied generated mesh-collider
  proxies, restore the stock primitive collider's enabled state, and make
  a fresh per-part proxy from native dimensions.
- If a clone's native prefab mesh cannot be resolved, reject its face morph
  with a diagnostic rather than applying a second deformation.
- Node normals remain based on the unmodified prefab orientation; no change
  to the live face-tilt controls, snap increments, or deformation mathematics.

## Lock X/Z for face-morph handles (0.0.4.63)

- Added persistent per-part **Lock X/Z** toggle in StretchScale's editor PAW, default enabled. Ordinary face-morph drags then use one shared X/Z multiplier to preserve the selected face's existing aspect ratio; Shift snaps that shared multiplier. Existing Alt tilt, Shift+Alt translation, and Ctrl+Alt radial modes remain unchanged.
- Added **Lock X/Z by default** in Vessel Inspector → Settings; stored in user preferences, with a shipped config fallback of `lockMorphXZDefault = true`. Existing craft-part choices take priority over the new default.
- Removed the line displaying the user-settings file location from the settings window.
