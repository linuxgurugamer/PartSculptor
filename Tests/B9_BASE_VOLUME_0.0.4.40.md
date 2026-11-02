# B9PartSwitch baseVolume regression checklist (0.0.4.40)

1. Install B9PartSwitch and a tank that uses `ModuleB9PartSwitch` with a
   nonzero `baseVolume`. Record its baseVolume and capacity at 1.0x.
2. Resize to 1.5x locked (expect `baseVolume × 3.375`). Resize to 2.0x
   (expect `baseVolume × 8`); back to 1.0x (original), repeat.
3. Change only Y to 2.0x, expect exactly `baseVolume × 2`.
4. Change between two subtypes at 1.5x. The tank contents should use each
   subtype's B9 resource ratios at the same scaled baseVolume, not compound.
5. Scale a half-full B9 tank. Verify its fill fraction remains half full.
6. Test two B9 switchers with different moduleID values on one part.
7. Save the craft at 1.5x, clear editor and reload; capacity, B9 baseVolume,
   native baseline, cost and mass must remain unchanged through two reloads.
8. Launch the craft. Verify fuel amounts do not reset on entering flight.
9. Inspect the Scaled Values window and confirm native and current baseVolume.
10. Check with Firespitter/IFS/MFT installed: B9 disabled resource switching
    must not cause another adapter's resource scaling to be skipped.
11. Check B9 addedMass/addedCost and volumeAdded variants across 1.0x,
    1.5x and 2.0x, ensuring module modifiers do not scale twice.
12. With Debug change logging enabled, check baseVolume writes happen only
    on real scale changes and remain stable while idle.

These are in-game test instructions; the offline tests exercise only the
production math and structural integration points.
