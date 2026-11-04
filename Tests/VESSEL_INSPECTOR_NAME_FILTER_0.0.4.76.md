# Vessel Inspector name filter — manual validation (0.0.4.76)

1. In the VAB or SPH, place multiple tanks and engines with visibly different titles. Open Vessel Inspector and uncheck **Scaled parts only**.
2. Type a substring (e.g. `tank`) in **Filter by name**; only titles or internal names containing it, ignoring case, should remain. Verify full-name matching and extra leading/trailing whitespace.
3. Turn **Scaled parts only** on while a name filter is active. Only matching scaled parts should remain. When no part matches, confirm **No parts match the name filter.**
4. Click **Clear**: the full list for the current **Scaled parts only** setting should return. Confirm the list scrolls to the beginning.
5. Search so the selected part disappears; the right-hand report should switch to a visible match, or show the selection prompt if none match. Ensure no IMGUI control-count exceptions in KSP.log.
6. With a name filter active, click a differently named part in the editor scene (or its PAW). The text search should clear and the selected part should become visible; if unscaled, **Scaled parts only** should also turn off.
7. Enter a search string, close and reopen the inspector, and check that the search remains usable; test undo/redo and vessel part removal.
8. Repeat basic search, clear, highlighting, selection, and filter-combination checks in flight with the active vessel.
