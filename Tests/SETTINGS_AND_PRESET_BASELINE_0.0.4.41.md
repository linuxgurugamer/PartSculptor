# PartSculptor 0.0.4.41 regression checklist

1. In VAB/SPH, open Vessel Inspector and click Settings. Check that **Match node sizes by default** starts off without `PluginData/UserSettings.cfg`.
2. Turn the option on; Save; close/reopen the settings window. Confirm On. Exit and restart KSP; confirm it remains On, including if `Settings.cfg` says false.
3. Turn the default off, Save. Confirm the existing PAW Match node sizes controls update in the current editor session and re-open/restart remains Off.
4. Toggle Match node sizes ON from the PAW without saving a new default. On restart it should use the saved OFF default.
5. Click Settings in flight. Confirm it opens/closes without a selection or altering a craft.
6. Check inspector Close and scene changes close the Settings window without leaving click locks.
7. Scale any tank to Y=1.25, choose built-in Double Y: Y should be 2.0; click Repeat preset multiple times: still 2.0. Verify X/Z return to 1.0.
8. Choose Half Y after Double Y: Y should be 0.5. Repeat should remain 0.5.
9. Create a custom relative Y=2 preset. It should continue multiplying existing Y on each application.
10. Save craft, clear editor and reload, re-apply Double Y; Y should remain 2.0.
