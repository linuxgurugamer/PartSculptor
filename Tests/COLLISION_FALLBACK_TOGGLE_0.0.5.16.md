# Vertex collision fallback toggle

1. With no user override, set `generateVertexCollisionFallback = false` in Settings.cfg. Start KSP and open PartSculptor Settings. The checkbox should be off.
2. Check Generate vertex collision fallback (experimental), then Apply. The next eligible PG vertex operation should use the enabled fallback without restarting.
3. Cancel. The effective setting and UserSettings.cfg should return to their state when the window opened (including a missing file).
4. Open Settings, enable the checkbox and Save. Restart KSP and check that it remains on. Disable and Save; restart and verify it remains off.
5. Verify that changing unrelated settings (including PAW visibility) preserves the saved fallback preference. Without an override, unrelated saves should not mask the value in Settings.cfg.
6. Native collider fallback remains available with the checkbox off. Keep the checkbox enabled for craft that rely on generated colliders. Toggling the setting is not a command to rebuild existing craft geometry.
