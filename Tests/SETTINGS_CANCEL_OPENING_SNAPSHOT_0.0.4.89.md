# PartSculptor 0.0.4.89 — Settings Cancel opening snapshot tests

1. Open Settings with Morph opacity 100%, alternate skin off, and a known snap increment. Change all three, then Cancel. Reopen: original values should be restored. The settings file should remain unchanged.
2. Open Settings, change opacity to 50% and alternate skin on, click **Apply**. Confirm the changes take effect. Then change opacity to 20% and click **Cancel**. Reopen: the opacity and skin should be the original values from before opening, not 50% or 20%. Restart KSP and check that the original preferences persist.
3. Open Settings, change Match node sizes and Stretch children, click Apply, then Cancel. Confirm both the defaults and live editor PAW toggles return to the values they held on opening.
4. With no UserSettings.cfg present, open Settings, click Apply, then Cancel. Confirm that the newly created UserSettings.cfg is removed and the original default settings are active. Save and Apply should still function normally.
5. Open Settings, modify a setting, click Apply, and then click **Save**. Confirm the final values persist after restarting KSP. Apply alone should still leave the window open.
6. Check window footer visibility at reduced resolution and ensure 100% Morph tip opacity still produces solid backgrounds with fully legible text.
