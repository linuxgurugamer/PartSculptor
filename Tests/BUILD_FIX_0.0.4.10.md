# StretchScale 0.0.4.10 — CS0136 build fix

1. Build the StretchScale project in Visual Studio using configured KSP/Unity assemblies.
2. Check that `ScaleExponents.cs` no longer reports CS0136 at either former line 550 or 570.
3. In a DEBUG build, change a numeric exponent-driven property, a Vector3 offset,
   and a FloatCurve-based module property and confirm the `[StretchScale][DEBUG]`
   logs report the correct before/after values.
4. Verify the 1.00 → 1.25 → 1.50 → 1.00 mass scaling sequence still works.

This change renames only debug local variables and their uses; scaling formulas
are unchanged. Compilation and in-game verification require a KSP environment.
