# B9 resource-only face morph (0.0.4.67)

1. Install B9PartSwitch + Streamline Engines Tanks with the provided
   `ModuleB9PartSwitch` (`moduleID=fuelSwitch`, four tankType subtypes and
   `addedMass=-0.03`). Place **DTE OX-50-61 mini Oxidizer Tank** in the VAB.
2. Activate StretchScale's Morph toolbar button. All eight handles should
   appear, rather than `Animated / switchable geometry unsupported`.
3. Tilt and radially resize a face. Change B9 subtype across LF/O,
   LiquidFuel, Oxidizer and MonoPropellant; confirm visible shape, node
   orientations, expected resource amounts, native baseVolume scaling,
   and no duplicate mesh deformation.
4. Save, reload and Alt+Click clone the morphed tank; confirm handles persist,
   faces align to attachment-node directions, and no additional morph occurs.
5. Test a different part with a B9 `SUBTYPE` using `transform` or `node`, or
   nested `TRANSFORM`, `TEXTURE`, `MATERIAL`, `NODE` or `MODULE`.
   Face morph must remain unavailable, giving the offending key/node.
6. Test a part with two B9 modules, one fuel-only and one geometry-changing.
   Morph must be rejected regardless of the active subtype.
7. Test a dynamically created B9 module with no matching final PART config.
   Morph must be rejected rather than guessed safe.
8. Test a stock tank without B9 and existing ordinary Stretch gizmo controls;
   behavior should be unchanged.

Build with KSP 1.12.x assemblies and test in-game; Python source tests cannot
validate Unity mesh, collider and B9 interactions.
