# PartSculptor 0.0.3.2 — preset navigation fix

## Cause

`UI_ChooseOption` maintains the selected option and arrow/index state internally. `OnPresetChanged()` used to set `scalePreset = "Custom"` immediately after a choice. This left the selector state inconsistent with the field value, so the label stayed on Custom and arrows navigated incorrectly.

## Fix

- Do not overwrite `scalePreset` in the option-change callback.
- Leave the chosen preset visible as the **last used command**, including after manual axis changes.
- Remove the unnecessary preset refresh flag.
- Add **Repeat preset** (`KSPEvent`) to allow repeated relative Y operations without changing the selector.
- Keep all preset operations routed through `CommitChange()` (symmetry, children, resources and calculations).

## Manual test

With a new tank, start at Custom. Click the preset right arrow through all six choices, and then left back to Custom. Each step should display the corresponding choice and apply it once. Select Double Y and use Repeat preset twice; Y should double twice. Test the Half Y action similarly. Then change one axis manually and confirm the selector arrows still work. Check symmetry and resource capacity while repeating.

Not compiled or tested in KSP in this environment.
