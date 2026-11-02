# Stock Engine FX and Waterfall regression checklist — 0.0.4.28

## Stock ModuleEnginesFX, no Waterfall installed

1. Load a fresh stock liquid engine in VAB; observe its FX hierarchy in Unity debugging tools if available.
2. Resize 1.0 -> 1.25 -> 1.5 -> 2.0 -> 1.0 and confirm effect offsets / sizes are non-compounding.
3. Test non-uniform 1x/2y/1z; confirm plume is adjusted by the part model geometry.
4. Test an effect emitted outside model and verify it is wrapped once, not wrapped every resize.
5. Clone a resized engine, reload a craft, switch to flight and observe running/flameout FX.
6. Verify a child-part engine does not have its FX taken over by the parent StretchScale module.

## Waterfall installed

1. Test an engine with ModuleWaterfallFX and TEMPLATE plus one with inline EFFECT settings.
2. Confirm Waterfall model-parented effects follow stretch and are not scaled twice.
3. Confirm standalone Waterfall meshes (shader name includes Waterfall) are wrapped and scaled.
4. Change throttle/atmospheric conditions: Waterfall modifiers should still animate correctly.
5. Test mode switching and B9 subtype rebuilds; no repeated wrappers or scale multiplication.
6. Test loaded craft at 2x -> 1x -> 2x, staging, flight and scene reload.
7. Disable scaleWaterfallEffects and restart: model-parented effects still follow model geometry.
8. Test without Waterfall installed and verify no missing-DLL exceptions.

## Known limitations

FX spawned outside the part's transform hierarchy cannot be tracked. Emission rates,
sound pitch, shader brightness and controller curves are not scaled. In-game validation
is required against KSP 1.12.x and the Waterfall versions actually installed.
