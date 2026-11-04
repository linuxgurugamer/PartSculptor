> Historical test plan: Cancel behavior changed in 0.0.4.89. For current behavior see `SETTINGS_CANCEL_OPENING_SNAPSHOT_0.0.4.89.md`.

# PartSculptor 0.0.4.88 — Cancel restoration and Morph tip opacity

1. Open Settings, toggle all defaults, change Morph snap, tip opacity, and alternate skin. Press **Cancel**; reopen Settings. Every setting must show its previously saved value. No UserSettings.cfg write should occur on Cancel.
2. Change settings, press **Apply**, change them again, then **Cancel**. Reopen and confirm all options match the last Apply, not the first opening or the uncommitted second edits.
3. Repeat with **Save**: options persist after closing and restarting KSP. Ensure shared Match node sizes and Stretch children options are not unintentionally changed by Cancel.
4. In VAB activate Morph. Set tip opacity to **100%**, Apply. Verify that the Morph help background and live drag tip are **fully opaque**, with no vessel geometry showing through, for both standard and alternate skins.
5. Set tip opacity to **10%**, **50%** and **99%**: only the background fades; instruction text and drag values remain fully opaque. Try the two end faces and move the mouse with a tooltip visible.
6. Ensure the settings window footer is always visible, and other settings, face-morph mathematics, and fuel integrations are unchanged.
