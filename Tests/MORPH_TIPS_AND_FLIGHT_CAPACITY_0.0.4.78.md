# PartSculptor 0.0.4.78: Morph tips, skins and in-flight resource bounds

## Morph tip transparency

1. In the VAB, open Vessel Inspector → Settings.
2. Set Morph tip opacity to 25% and save. Select the Morph tool and a compatible tank.
3. Confirm both the instruction box below the editor tools and the active drag tip are translucent, while the handles remain opaque.
4. Repeat at 100% opacity. Exit and restart KSP; verify the saved value persists.
5. Verify the same tip opacity is applied when an unsupported part shows a reason in the help box.

## Alternate skin

1. Toggle **Use alternate skin**, save, and reopen Vessel Inspector, Settings, Presets, Scaled Values and Preset Editor.
2. Check that PartSculptor windows follow the selected skin while stock KSP windows are unchanged.
3. Switch off, save, confirm the original appearance returns, and restart to verify persistence.

## In-flight resource capacity upper bound

1. In the VAB, place a fuel tank with stock resources. Stretch it to double its initial capacity; save the craft.
2. Launch and open the tank's PAW. The resource readout must display the stretched **amount / maximum**, not the prefab maximum.
3. Burn some fuel and watch the readout. The maximum should remain the stretched value and the resource amount should decrease normally.
4. Quick-save and quick-load; verify the restored maximum remains correct. Repeat with a B9 resource-only tank and a saved craft whose resource maximum already equals the stretched maximum at launch.
5. Repeat the launch with an unstretched tank, and confirm its original capacity remains unchanged.

This is a manual in-game plan. Offline Python checks are source-level only and do not establish flight UI behavior.
