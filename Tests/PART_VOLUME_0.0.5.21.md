# Part volume PAW validation

1. Place a closed 1 m cube with no cavity, open the PAW; Part volume should read 1 m³. Stretch X to 2 and Z to 3, wait up to one second; expect 6 m³. Undo and reload the craft; expect 1 m³.
2. Use a 2 m cube with a closed 1 m cubic cavity represented by a second shell. Expect about 7 m³, not 8 m³. With an additional 0.5 m solid island inside the cavity expect 7.125 m³.
3. Use an open-ended square tube of outer side 2 m, inner side 1 m and length 2 m with connected inner and outer walls and end rims. Expect 6 m³, not 8 m³. A surface with no wall thickness/closed boundary must not be treated as a filled solid.
4. Add a ring and resize it, tilt an end face, and move procedural vertices inward/outward. Observe the field after each committed edit. Compare with exported current visual geometry if available. Check symmetry, reset, undo and craft reload. Confirm resource/mass/cost modifiers use their unchanged existing multiplier.
5. Switch stock and B9 variants: only active rendered meshes should be included. Check flight PAW after launch. Verify opening PAWs on a large craft does not trigger geometry measurement for closed PAWs.
6. Inspect a model containing only an open surface: Unavailable. A valid closed shell plus open decorative geometry: partial. Multiple shells: approximate. Nested shells use even/odd containment, not a Boolean union; self-intersecting and overlapping geometry is not guaranteed accurate. Hidden/unmodeled cavities (e.g., exterior-only tanks) cannot be measured from the model.
7. Run `dotnet run --project Tests/RegressionRunner/RegressionRunner.csproj -c Release` to execute the production core reference cases. Full KSP build and UI tests require the game's assemblies.
