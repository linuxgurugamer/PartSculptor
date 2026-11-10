# B9 parent subtype volume and face morph — KSP manual validation (0.0.4.105)

Use a part with two B9PartSwitch modules: `moduleID = fuelSwitch` with a
non-Structural tank, and another module with `parentID = fuelSwitch` and
subtypes carrying positive, zero, and negative `volumeAddedToParent` values.
Test examples include Bluedog Design Bureau DCSS / Apollo service-module
variants, or a small custom part using otherwise morph-safe subtypes.

1. In the VAB or SPH, record native `baseVolume`, subtype contribution,
   selected parent tank capacity and fill amount at 1× dimensions.
2. Morph just one end face to make the computed volume factor 1.25×. Confirm
   a `volumeAddedToParent = -200` contribution behaves as `-250`, and
   `volumeAddedToParent = +200` behaves as `+250` (zero stays zero).
3. Morph down to 0.5×, then restore to 1×. Ensure original values are
   recovered without drift; repeat edits/undo/redo 10+ times.
4. Switch child B9 subtypes while morphed, including an inactive subtype that
   becomes active later. Ensure parent tank volume and managed resource capacity
   update from the correct original subtype amount (no compounded scale).
5. Test a parent with `baseVolume = 0` but positive child contribution. Verify
   resources are owned and sized by B9, not independently double-scaled.
6. Start with partially filled tanks. Check the fraction is preserved in the
   editor; flight reload/quickload should not refill, zero or increase fuel.
7. Save/load a craft, duplicate the part with Alt-copy, add symmetry copies,
   reverse a morph, and switch subtypes. Check every part remains independent
   except intentional symmetry-value propagation.
8. Repeat with texture-only B9 subtype blocks and `transform = ...` model
   toggles; geometry-changing `TRANSFORM` and `NODE` blocks must still be refused.
9. Check KSP.log for `[PartSculptor] B9 UpdateVolume failed` and any B9
   parent/child warnings. If problems appear, record the part config and
   `parentID`/`moduleID` arrangement, plus both selected subtype names.

The offline test runner cannot substitute for these checks; B9 reflection
member access, runtime subtype serialization and parent capacity allocation
must be confirmed in KSP 1.12.x with B9PartSwitch installed.
