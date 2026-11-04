using System;
using PartSculptor;

internal static class Program
{
    private static int checks;
    private static void Equal(double actual, double expected, string name, double tolerance = 1e-8)
    {
        ++checks;
        if (double.IsNaN(actual) || Math.Abs(actual - expected) > tolerance)
            throw new Exception(name + ": expected " + expected + ", got " + actual);
    }
    private static void Equal(int actual, int expected, string name)
    {
        ++checks;
        if (actual != expected)
            throw new Exception(name + ": expected " + expected + ", got " + actual);
    }

    private static void MorphEdgeHandles()
    {
        // The midpoint of an edge on a planar, tilted and translated face
        // must lie halfway between the two transformed corner positions.
        var min = new MorphPoint(-1, -2, -2);
        var max = new MorphPoint(1, 2, 2);
        var xMinusLow = FaceMorphCore.Transform(new MorphPoint(-1, 2, -2),
            min, max, 1, 1, 0, 0, 1.5, 0.75, 0.3, -0.25,
            0, 0, 0.5, -0.25);
        var xMinusHigh = FaceMorphCore.Transform(new MorphPoint(-1, 2, 2),
            min, max, 1, 1, 0, 0, 1.5, 0.75, 0.3, -0.25,
            0, 0, 0.5, -0.25);
        var midpoint = FaceMorphCore.Transform(new MorphPoint(-1, 2, 0),
            min, max, 1, 1, 0, 0, 1.5, 0.75, 0.3, -0.25,
            0, 0, 0.5, -0.25);
        Equal(midpoint.X, (xMinusLow.X + xMinusHigh.X) * .5d,
            "midpoint X stays centered on deformed edge");
        Equal(midpoint.Y, (xMinusLow.Y + xMinusHigh.Y) * .5d,
            "midpoint Y stays on tilted face");
        Equal(midpoint.Z, (xMinusLow.Z + xMinusHigh.Z) * .5d,
            "midpoint Z stays centered on deformed edge");
        for (int edge = 0; edge != 4; ++edge)
        {
            int xs = FaceMorphCore.EdgeWidthSign(edge);
            int zs = FaceMorphCore.EdgeDepthSign(edge);
            Equal(Math.Abs(xs) + Math.Abs(zs), 1,
                "each midpoint controls exactly one axis " + edge);
        }
        Equal(FaceMorphCore.EdgeWidthSign(0), -1, "left midpoint shrinks from X-");
        Equal(FaceMorphCore.EdgeWidthSign(1), 1, "right midpoint grows from X+");
        Equal(FaceMorphCore.EdgeWidthSign(2), 0, "Z- midpoint doesn't change width");
        Equal(FaceMorphCore.EdgeWidthSign(3), 0, "Z+ midpoint doesn't change width");
        Equal(FaceMorphCore.EdgeDepthSign(0), 0, "X- midpoint doesn't change depth");
        Equal(FaceMorphCore.EdgeDepthSign(1), 0, "X+ midpoint doesn't change depth");
        Equal(FaceMorphCore.EdgeDepthSign(2), -1, "front midpoint shrinks from Z-");
        Equal(FaceMorphCore.EdgeDepthSign(3), 1, "back midpoint grows from Z+");
        // Radial adjustment from an X-only edge still uses one common factor.
        Equal(FaceMorphCore.RadialScaleFactor(-1.5, 0, .3, 0, 1.5, .75, 0),
            .8, "midpoint radial move uses a shared factor");
    }

    private static void ProjectedMidpointDrag()
    {
        // An oblique editor camera projects an allowed axis diagonally.
        // A cursor motion of (24,12) pixels across a 0.1-wide probe with
        // a projected delta of (12,6) must produce 0.2, not half-speed.
        Equal(FaceMorphCore.ProjectedHandleParameterDelta(
            24, 12, 12, 6, .1), .2, "projected midpoint tracks pixels");
        Equal(FaceMorphCore.ProjectedHandleParameterDelta(
            -24, -12, 12, 6, .1), -.2, "reverse direction tracks");
        Equal(FaceMorphCore.ProjectedHandleParameterDelta(
            6, -12, 12, 6, .1), 0, "perpendicular mouse ignored");
        Equal(FaceMorphCore.ProjectedHandleParameterDelta(
            20, 0, 0, 0, .1), 0, "edge-on camera safely handled");
        // The same projection helper applies to both single-axis midpoint
        // resizing and the shared-factor mode when Lock X/Z is enabled.
        Equal(FaceMorphCore.ProjectedHandleParameterDelta(
            30, 0, 15, 0, .05), .10, "shared radial factor screen speed");
    }

    private static void RealFuelsTotalVolume()
    {
        // RF v15.16.1: usable 4,000 L -> 5,000 L with 100% utilization.
        Equal(ScaleMath.MFTTotalVolumeTarget(5000, 4000, 4000, 100),
            5000, "RF 15.16.1 reported case 4,000 -> 5,000 L");
        // The API takes pre-utilization volume. At 80% utilization,
        // a 5,000 L usable request requires 6,250 L total physical volume.
        Equal(ScaleMath.MFTTotalVolumeTarget(5000, 4000, 5000, 80),
            6250, "RF 80-percent utilization conversion");
        Equal(ScaleMath.MFTTotalVolumeTarget(4000, 5000, 6250, 80),
            5000, "RF reset does not compound");
        Equal(ScaleMath.MFTTotalVolumeTarget(5000, 0, 0, 80),
            6250, "RF uninitialized volume uses utilization fallback");
        if (!double.IsNaN(ScaleMath.MFTTotalVolumeTarget(5000, 0, 0, 0)))
            throw new Exception("RF target should be invalid without utilization");
        ++checks;
    }

    private static void StageDeltaVStableMass()
    {
        // The same stretched part can be queried using multiple stage-snapshot
        // base masses. Its total dry mass must remain identical in each query.
        double target = 1.25 * 2.0; // absolute prefab-anchored stretched mass
        double tweak = 0.15;
        double b9Extra = -0.03;
        double fuelExtra = 0.07;
        double expected = target + b9Extra + fuelExtra;
        foreach (double stageBase in new[] { 0.25, 0.75, 1.25, 0.25, 1.25, 0.75 })
        {
            double delta = ScaleMath.MassModifierDelta(stageBase, target,
                tweak, b9Extra, fuelExtra);
            Equal(stageBase + tweak + delta, expected,
                "stable dry mass for repeated stage snapshot " + stageBase);
        }
        Equal(ScaleMath.MassModifierDelta(0.5, 1.5, 0, 0, 0), 1,
            "single simple stretched tank modifier");
    }

    private static void Scaling()
    {
        Equal(ScaleMath.Volume(1.5, 1.5, 1.5), 3.375, "cubic volume");
        Equal(ScaleMath.Volume(1, 2, 1), 2, "independent Y volume");
        Equal(ScaleMath.ExponentFactor(8, 2.5), Math.Pow(2, 2.5), "engine exponent");
        Equal(ScaleMath.ExponentFactor(8, -0.5), Math.Pow(2, -0.5), "heat exponent");

        // Every update is computed from the immutable baseline, NEVER from
        // the previous readout. This guards the reported 1.25 -> 1.5 runaway.
        double nativeMass = 1d;
        Equal(ScaleMath.ScaledValue(nativeMass, Math.Pow(1.25, 3), 3), 1.953125, "1.25 dry mass");
        Equal(ScaleMath.ScaledValue(nativeMass, Math.Pow(1.5, 3), 3), 3.375, "1.5 dry mass");
        Equal(ScaleMath.ScaledValue(nativeMass, Math.Pow(1.25, 3), 3), 1.953125, "back to 1.25");
        Equal(ScaleMath.ScaledValue(nativeMass, 1, 3), 1, "back to 1.0");
        for (int i = 0; i != 100; ++i)
            Equal(ScaleMath.ScaledValue(nativeMass, 8, 3), 8, "repeat 2.0 no compounding");

        // Variant/switcher native capacities must not inherit the prior
        // selected subtype's already-resized capacity.
        Equal(ScaleMath.ScaledValue(200, 8, 3), 1600, "B9 tank A capacity");
        Equal(ScaleMath.ScaledValue(100, 8, 3), 800, "B9 tank B capacity");
        Equal(ScaleMath.ScaledValue(200, 8, 3), 1600, "B9 switch back capacity");
    }

    private static void Gizmo()
    {
        Equal(ScaleMath.GizmoScaleValue(1, 30, 60, 0.05, 10, 0), 1.5,
            "gizmo start-based scale");
        Equal(ScaleMath.GizmoScaleValue(1, 30, 60, 0.05, 10, 0), 1.5,
            "gizmo repeated drag does not compound");
        Equal(ScaleMath.GizmoScaleValue(1, -200, 60, 0.5, 10, 0), 0.5,
            "gizmo clamps to minimum");
        Equal(ScaleMath.GizmoScaleValue(1, 2000, 60, 0.05, 3, 0), 3,
            "gizmo clamps to maximum");
        Equal(ScaleMath.GizmoScaleValue(1.23, 27, 60, 0.05, 10, 0.05), 1.68,
            "shift snapping relative to drag start");
        Equal(ScaleMath.GizmoScaleValue(1.23, 0, 60, 0.05, 10, 0.05), 1.23,
            "zero drag must preserve original scale");
        // The handle's screen displacement, rather than total stem distance,
        // determines the new scale. The same cursor position must not move it
        // again after the handle has caught up.
        Equal(ScaleMath.GizmoFollowValue(1, 1, 30, 60, 0.05, 10, 0), 1.5,
            "gizmo mouse follows handle");
        Equal(ScaleMath.GizmoFollowValue(1, 1.5, 0, 60, 0.05, 10, 0), 1.5,
            "gizmo no drift when cursor catches handle");
        Equal(ScaleMath.GizmoFollowValue(1, 1.5, -15, 60, 0.05, 10, 0), 1.25,
            "gizmo moves back toward mouse");
        Equal(ScaleMath.GizmoFollowValue(1.25, 1.5, 0, 60, 0.05, 10, 0.05), 1.5,
            "gizmo snapped handle zero-error stays fixed");
        Equal(ScaleMath.GizmoFollowValue(1.25, 1.5, 4, 60, 0.05, 10, 0.05), 1.55,
            "gizmo shift snap uses drag baseline");
        Equal(ScaleMath.GizmoFollowValue(1, 3, 20, 60, 0.05, 3, 0), 3,
            "gizmo maximum limit remains enforced");
        Equal(ScaleMath.GizmoFollowValue(1, 0.5, -100, 60, 0.5, 10, 0), 0.5,
            "gizmo minimum limit remains enforced");
        Equal(ScaleMath.GizmoFollowValue(1, 1.5, 3, 0, 0.05, 10, 0), 1.5,
            "gizmo invalid projection remains unchanged");
        ScaleMath.Axes axes = ScaleMath.GizmoAxes(1, 2, 1.5, 0, 1.25, false);
        Equal(axes.X, 1.25, "gizmo X changes");
        Equal(axes.Y, 2, "gizmo X leaves Y unchanged");
        Equal(axes.Z, 1.5, "gizmo X leaves Z unchanged");
        axes = ScaleMath.GizmoAxes(1, 2, 1.5, 1, 3, true);
        Equal(axes.X, 1.5, "gizmo lock X");
        Equal(axes.Y, 3, "gizmo lock Y");
        Equal(axes.Z, 2.25, "gizmo lock Z");
        axes = ScaleMath.GizmoAxes(1, 2, 1.5, 4, 9, true);
        Equal(axes.Y, 2, "gizmo invalid axis is no-op");
        axes = ScaleMath.GizmoAxes(1, 2, 1.5, 0, 5, true, 0.5, 3);
        Equal(axes.X, 1.5, "locked gizmo max across axes X");
        Equal(axes.Y, 3, "locked gizmo max across axes Y");
        Equal(axes.Z, 2.25, "locked gizmo max across axes Z");
        axes = ScaleMath.GizmoAxes(1, 2, 1.5, 1, 0.1, true, 0.5, 3);
        Equal(axes.X, 0.5, "locked gizmo min across axes X");
        Equal(axes.Y, 1, "locked gizmo min across axes Y");
        Equal(axes.Z, 0.75, "locked gizmo min across axes Z");
        // Ctrl mode: X/Z linked by one multiplier, with Y independent.
        axes = ScaleMath.GizmoXZAxes(1, 2, 1.5, 0, 1.25, 0.5, 3);
        Equal(axes.X, 1.25, "Ctrl X grows X");
        Equal(axes.Y, 2, "Ctrl X keeps Y fixed");
        Equal(axes.Z, 1.875, "Ctrl X grows Z proportionally");
        axes = ScaleMath.GizmoXZAxes(1, 2, 1.5, 2, 2, 0.5, 3);
        Equal(axes.X, 4d / 3d, "Ctrl Z grows X proportionally");
        Equal(axes.Y, 2, "Ctrl Z keeps Y fixed");
        Equal(axes.Z, 2, "Ctrl Z grows Z");
        axes = ScaleMath.GizmoXZAxes(1, 2, 1.5, 1, 2.5, 0.5, 3);
        Equal(axes.X, 1, "Ctrl Y keeps X fixed");
        Equal(axes.Y, 2.5, "Ctrl Y changes Y alone");
        Equal(axes.Z, 1.5, "Ctrl Y keeps Z fixed");
        axes = ScaleMath.GizmoXZAxes(1, 2, 2, 0, 3, 0.5, 3);
        Equal(axes.X, 1.5, "Ctrl max clamps linked X");
        Equal(axes.Y, 2, "Ctrl max leaves Y alone");
        Equal(axes.Z, 3, "Ctrl max clamps linked Z");
        axes = ScaleMath.GizmoXZAxes(2, 3, 1, 0, 0.25, 0.5, 10);
        Equal(axes.X, 1, "Ctrl min clamps linked X");
        Equal(axes.Y, 3, "Ctrl min leaves Y alone");
        Equal(axes.Z, 0.5, "Ctrl min clamps linked Z");
        axes = ScaleMath.GizmoXZAxes(1, 2, 1.5, -1, 5, 0.5, 3);
        Equal(axes.X, 1, "Ctrl invalid axis keeps X");
        Equal(axes.Z, 1.5, "Ctrl invalid axis keeps Z");
        // Shift+Ctrl snaps the active axis; X/Z use the resulting one factor.
        double snapped = ScaleMath.GizmoFollowValue(1, 1, 14, 60, 0.5, 3, 0.05);
        axes = ScaleMath.GizmoXZAxes(1, 2, 1.5, 0, snapped, 0.5, 3);
        Equal(axes.X, 1.25, "Shift+Ctrl snapped X");
        Equal(axes.Z, 1.875, "Shift+Ctrl preserves X/Z ratio");
    }

    private static void Presets()
    {
        var uniform = new ScalePresetDefinition("1.5x uniform", false, 1.5, 1.5, 1.5, true);
        var a = ScalePresetMath.Apply(uniform, 2, 3, 4);
        Equal(a.X, 1.5, "absolute preset X");
        Equal(a.Y, 1.5, "absolute preset Y");
        Equal(a.Z, 1.5, "absolute preset Z");
        var stretch = new ScalePresetDefinition("Double Y", false, 1, 2, 1, true);
        a = ScalePresetMath.Apply(stretch, 1, 1.25, 1);
        Equal(a.X, 1, "Double Y resets X to native");
        Equal(a.Y, 2, "Double Y uses original baseline, not previous Y");
        Equal(a.Z, 1, "Double Y resets Z to native");
        a = ScalePresetMath.Apply(stretch, a.X, a.Y, a.Z);
        Equal(a.Y, 2, "Double Y repeat must not compound");
        var half = new ScalePresetDefinition("Half Y", false, 1, 0.5, 1, true);
        a = ScalePresetMath.Apply(half, 1.5, 2, 1.5);
        Equal(a.Y, 0.5, "Half Y uses original baseline");
        a = ScalePresetMath.Apply(half, a.X, a.Y, a.Z);
        Equal(a.Y, 0.5, "Half Y repeat must not compound");
        var customRelative = new ScalePresetDefinition("Multiply Y", true, 1, 2, 1, false);
        a = ScalePresetMath.Apply(customRelative, 1, 1.25, 1);
        Equal(a.Y, 2.5, "Custom relative preset still multiplies current Y");
        if (!ScalePresetMath.ValidName("My long tank") ||
            ScalePresetMath.ValidName("Custom") ||
            ScalePresetMath.ValidName(" ") ||
            ScalePresetMath.Valid(new ScalePresetDefinition("bad", false, double.NaN, 1, 1, false)) ||
            ScalePresetMath.Valid(new ScalePresetDefinition("bad", false, 0, 1, 1, false)))
            throw new Exception("Preset input validation failed");
        checks += 5;
    }

    private static void Nodes()
    {
        Equal(ScaleMath.NominalNodeDiameter(0), 0.625, "tier 0");
        Equal(ScaleMath.NominalNodeDiameter(1), 1.25, "tier 1");
        Equal(ScaleMath.NominalNodeDiameter(2), 2.5, "tier 2");
        Equal(ScaleMath.NodeSizeTier(1, 0, 1, 0, 1, 1, 1), 1, "no scale");
        Equal(ScaleMath.NodeSizeTier(1, 0, 1, 0, 1, 2, 1), 1, "axial stretch does not grow node");
        Equal(ScaleMath.NodeSizeTier(1, 0, 1, 0, 2, 1, 2), 2, "face widened to 2.5");
        Equal(ScaleMath.NodeSizeTier(0, 0, 1, 0, 2, 1, 2), 1, "tier-zero widened to 1.25");
        Equal(ScaleMath.NodeDiameter(1, 0, 1, 0, 1.5, 1, 1.5), 1.875, "actual fractional diameter");
        Equal(ScaleMath.NodeDiameter(1, 0, 1, 0, 1, 3, 1), 1.25, "Y-only stack diameter");

        // A 2.5m parent and 1.25m child: stack normal Y, match X and Z
        // without changing Y length. Then check the resulting FACE diameter.
        ScaleMath.Axes ratio = ScaleMath.NodeMatchMultipliers(0, 1, 0, 2.5, 1.25);
        Equal(ratio.X, 2, "match X");
        Equal(ratio.Y, 1, "preserve Y length");
        Equal(ratio.Z, 2, "match Z");
        Equal(ScaleMath.NodeDiameter(1, 0, 1, 0, ratio.X, ratio.Y, ratio.Z), 2.5,
            "matched diameter equals parent");

        // Parent already stretched 1.5x: use true diameter, not integer tier
        // rounded up to 2 (2.5m) in KSP's AttachNode.size property.
        ratio = ScaleMath.NodeMatchMultipliers(0, 1, 0, 1.875, 1.25);
        Equal(ratio.X, 1.5, "fractional parent match X");
        Equal(ratio.Y, 1, "fractional parent retain length");
        Equal(ratio.Z, 1.5, "fractional parent match Z");

        ratio = ScaleMath.NodeMatchMultipliers(1, 0, 0, 2.5, 1.25);
        Equal(ratio.X, 1, "X-normal retains X");
        Equal(ratio.Y, 2, "X-normal matches Y");
        Equal(ratio.Z, 2, "X-normal matches Z");
        ratio = ScaleMath.NodeMatchMultipliers(0.707, 0.707, 0, 2.5, 1.25);
        Equal(ratio.X, 2, "oblique node uniform X");
        Equal(ratio.Y, 2, "oblique node uniform Y");
        Equal(ratio.Z, 2, "oblique node uniform Z");
        ratio = ScaleMath.NodeMatchMultipliers(0, 1, 0, 0, 1.25);
        Equal(ratio.X, 1, "invalid node safe no-op");
        Equal(ratio.Y, 1, "invalid node Y safe no-op");
    }

    private static void Docking()
    {
        Equal(ScaleMath.DockingFaceScale(0, 1, 0, 2, 1, 2), 2,
            "Y-oriented docking face width");
        Equal(ScaleMath.DockingFaceScale(0, 1, 0, 1, 2, 1), 1,
            "Y-only length does not change docking face");
        Equal(ScaleMath.DockingFaceScale(0, 1, 0, 2, 1, 1), Math.Sqrt(2),
            "nonuniform docking face area");
        Equal(ScaleMath.DockingFaceScale(0, 1, 0, 1.5, 1, 1.5), 1.5,
            "fractional face scaling");
        ++checks;
        if (ScaleMath.DockingNodeType("size1", 2.5) != "size2")
            throw new Exception("normal-to-Senior stock docking compatibility");
        ++checks;
        if (ScaleMath.DockingNodeType("size1", 0.625) != "size0")
            throw new Exception("normal-to-Junior stock docking compatibility");
        ++checks;
        if (ScaleMath.DockingNodeType("size1", 1.875) != "PartSculptorDock_1875mm")
            throw new Exception("intermediate size must not claim stock compatibility");
        ++checks;
        if (ScaleMath.DockingNodeType("size1", 1.875) !=
            ScaleMath.DockingNodeType("size0", 1.875))
            throw new Exception("matching physical size must yield same type");
        ++checks;
        if (ScaleMath.DockingNodeType("customPort", 2.5) != "customPort")
            throw new Exception("mod-defined docking port type must remain untouched");
        ++checks;
        if (ScaleMath.DockingNodeType("size1", 1.25) != "size1")
            throw new Exception("1x stock docking type must not change");
        Equal(0.5 * ScaleMath.DockingFaceScale(0, 1, 0, 2, 1, 2), 1,
            "capture range absolute result after 2x");
        Equal(0.5 * ScaleMath.DockingFaceScale(0, 1, 0, 1.5, 1, 1.5), 0.75,
            "capture range must use original baseline at 1.5x");
        Equal(0.5 * ScaleMath.DockingFaceScale(0, 1, 0, 1, 1, 1), 0.5,
            "capture range returns to 1x baseline");
    }

    private static void LoadedStackAlignment()
    {
        // The 1.5x FL-T800's upper node is farther from its part center.
        // KSP initially loads the unscaled center spacing, putting the tank
        // into the Mk1 pod. Post-load alignment must translate the tank once.
        double oldTankNodeWorld = 0.5;
        double restoredTankNodeWorld = 0.75;
        double podNodeWorld = 0.5;
        double delta = ScaleMath.AttachmentCorrection(podNodeWorld, restoredTankNodeWorld);
        Equal(delta, -0.25, "loaded 1.5x tank moves away from pod");
        Equal(ScaleMath.AttachmentCorrection(podNodeWorld,
            restoredTankNodeWorld + delta), 0, "alignment never compounds");
        // A connection deliberately offset by the construction gizmo must
        // use its saved gap, rather than silently snapping to zero offset.
        double gap = 0.125;
        delta = ScaleMath.AttachmentCorrection(podNodeWorld + gap, restoredTankNodeWorld);
        Equal(delta, -0.125, "preserve saved stack gizmo offset");
        Equal(ScaleMath.AttachmentCorrection(podNodeWorld + gap,
            restoredTankNodeWorld + delta), 0, "saved-offset restoration stable");
    }

    private static void Attachments()
    {
        object parentA = new object();
        object parentB = new object();
        NodeAttachWatcher watcher = new NodeAttachWatcher();
        watcher.Seed(parentA); // Existing craft: must never auto-match on load.
        Equal(watcher.Observe(parentA) ? 1 : 0, 0, "loaded craft no match");
        watcher.Observe(null);  // New part is picked up/detached.
        Equal(watcher.Observe(null) ? 1 : 0, 0, "detached part no match");
        Equal(watcher.Observe(parentB) ? 1 : 0, 1, "fresh attachment detected");
        watcher.Complete();
        Equal(watcher.Observe(parentB) ? 1 : 0, 0, "same parent not repeated");
        Equal(watcher.Observe(parentA) ? 1 : 0, 1, "new parent detected");
        watcher.Complete();
        watcher.Observe(null);
        watcher.Observe(parentB);
        for (int i = 0; i < 11; ++i)
        {
            Equal(watcher.Observe(parentB) ? 1 : 0, 1, "await late node reference");
            watcher.Retry();
        }
        watcher.Retry();
        Equal(watcher.Observe(parentB) ? 1 : 0, 0, "retry bounded");
    }

    private static void B9VolumeScaling()
    {
        // baseVolume is derived from the prefab / saved native value, not
        // the value written by a previous stretch or subtype change.
        Equal(ScaleMath.B9BaseVolume(100, 1), 100, "B9 unscaled baseVolume");
        Equal(ScaleMath.B9ManagedCapacity(122.222, 2), 244.444,
            "B9 active subtype capacity from scaled total volume");
        Equal(ScaleMath.B9ManagedCapacity(61.111, 2), 122.222,
            "B9 original subtype capacity");
        Equal(ScaleMath.B9ManagedCapacity(122.222, 2), 244.444,
            "B9 repeated reconciliation does not compound");
        if (!double.IsNaN(ScaleMath.B9ManagedCapacity(-1, 2)))
            throw new Exception("B9 invalid total volume should be rejected");
        Equal(ScaleMath.B9BaseVolume(100, 3.375), 337.5, "B9 1.5x uniform baseVolume");
        Equal(ScaleMath.B9BaseVolume(100, 8), 800, "B9 2x uniform baseVolume");
        Equal(ScaleMath.B9BaseVolume(100, 3.375), 337.5, "B9 reverse to 1.5x");
        Equal(ScaleMath.B9BaseVolume(100, 1), 100, "B9 reverse to 1x");
        Equal(ScaleMath.B9BaseVolume(100, 2), 200, "B9 independent Y scaling");
        for (int i = 0; i < 100; ++i)
            Equal(ScaleMath.B9BaseVolume(100, 8), 800, "B9 repeated scaling no growth");
        // B9's live modifier already includes the scaled volume contribution.
        // PartSculptor must only add a correction for fixed subtype mass/cost.
        Equal(ScaleMath.B9ModifierExtra(35.75, 2, 100d / 337.5d, 3.375), 4.75,
            "B9 mass no second scaling");
        Equal(ScaleMath.B9ModifierExtra(35.75, 2, 100d / 337.5d, 1), -23.75,
            "B9 exponent zero correction");
        Equal(ScaleMath.B9ModifierExtra(12, 2, 1, 1), 0,
            "B9 unchanged scale delta");
    }

    private static void FuelSwitches()
    {
        // Covers MFT, FSfuelSwitch and InterstellarFuelSwitch: all share
        // the same immutable native-capacity math but use distinct setup ids.
        Equal(FuelSwitchMath.AbsoluteCapacity(200, 1, 3.375), 675,
            "Firespitter switch A at 1.5x");
        Equal(FuelSwitchMath.AbsoluteCapacity(100, 1, 3.375), 337.5,
            "Firespitter switch B at 1.5x");
        Equal(FuelSwitchMath.NativeCapacity(675, 1, 200, true, 3.375, 100, false),
            200, "saved setup A restores native baseline");
        Equal(FuelSwitchMath.NativeCapacity(337.5, 1, 100, true, 3.375, 200, false),
            100, "saved setup B restores native baseline");
        Equal(FuelSwitchMath.NativeCapacity(400, 1, 0, false, 3.375, 200, false),
            400, "new IFS setup same resource name gets new native baseline");
        Equal(FuelSwitchMath.NativeCapacity(675, 1, 0, false, 3.375, 200, true),
            200, "mesh-only switch does not rebase scaled fuel");
        Equal(FuelSwitchMath.NativeCapacity(1350, 2, 0, false, 3.375, 200, false),
            200, "already scaled capacity not rebased with TweakScale");
        Equal(FuelSwitchMath.AbsoluteCapacity(200, 2, 3.375), 1350,
            "TweakScale and PartSculptor multipliers combined once");
        for (int i = 0; i < 50; ++i)
            Equal(FuelSwitchMath.AbsoluteCapacity(200, 1, 8), 1600,
                "repeated MFT scaling never compounds");
    }

    private static void CrewCapacity()
    {
        // A part with four physically authored IVA seats can safely grow
        // from one crew seat to four, but repeated edits never compound.
        var result = CrewCapacityMath.Calculate(1, 8, 2, 4, 0, 1);
        Equal(result.Requested, 4, "crew exponent 2 at 2x");
        Equal(result.Applied, 4, "four IVA seats available");
        result = CrewCapacityMath.Calculate(1, 3.375, 2, 4, 0, 4);
        Equal(result.Applied, 2, "crew back down to 1.5x");
        result = CrewCapacityMath.Calculate(1, 8, 2, 1, 0, 1);
        Equal(result.Requested, 4, "mk1 requests four");
        Equal(result.Applied, 1, "mk1 cannot create fictitious seats");
        result = CrewCapacityMath.Calculate(1, 8, 2, 0, 0, 1);
        Equal(result.Applied, 1, "unknown IVA falls back to native");
        result = CrewCapacityMath.Calculate(4, .125, 2, 4, 0, 4);
        Equal(result.Applied, 1, "smaller crewed part can shrink empty crew count");
        result = CrewCapacityMath.Calculate(1, 8, 2, 4, 1, 1);
        Equal(result.Applied, 1, "occupied pod remains unchanged");
        result = CrewCapacityMath.Calculate(1, 8, 2, 4, 3, 3);
        Equal(result.Applied, 3, "occupied loaded craft keeps existing crew");
        for (int i = 0; i < 100; ++i)
            Equal(CrewCapacityMath.Calculate(1, 8, 2, 4, 0, 4).Applied, 4,
                "crew never compounds on repeated scale edits");
    }

    private static void FaceMorph()
    {
        var min = new MorphPoint(-1, -2, -1);
        var max = new MorphPoint(1, 2, 1);
        // Original vertices must be the input on EVERY call, not the result.
        var p = new MorphPoint(1, 2, 1);
        var tapered = FaceMorphCore.Transform(p, min, max,
            1, 1, 0, 0, .5, .5, 0, 0);
        Equal(tapered.X, .5, "top x is narrower");
        Equal(tapered.Y, 2, "top y unchanged without tilt");
        Equal(tapered.Z, .5, "top z is narrower");
        for (int i = 0; i < 100; ++i)
            Equal(FaceMorphCore.Transform(p, min, max, 1, 1, 0, 0,
                .5, .5, 0, 0).X, .5, "morph never compounds");
        var bottom = FaceMorphCore.Transform(new MorphPoint(1, -2, 1), min, max,
            1, 1, 0, 0, .5, .5, 0, 0);
        Equal(bottom.X, 1, "bottom width remains native");
        var middle = FaceMorphCore.Transform(new MorphPoint(1, 0, 1), min, max,
            1, 1, 0, 0, .5, .5, 0, 0);
        Equal(middle.X, .75, "midpoint interpolates taper");
        var slanted = FaceMorphCore.Transform(p, min, max,
            1, 1, 0, 0, 1, 1, .2, -.1);
        Equal(slanted.Y, 2.1, "top face tilt is planar");
        // Linked faces translate all 4 corners equally, without changing
        // their dimensions or Y/tilt; offsets are interpolated along the tank.
        var linkedA = FaceMorphCore.Transform(new MorphPoint(-1, 2, -1), min, max,
            1, 1, 0, 0, 1, 1, 0, 0, 0, 0, .4, -.6);
        var linkedB = FaceMorphCore.Transform(new MorphPoint(1, 2, 1), min, max,
            1, 1, 0, 0, 1, 1, 0, 0, 0, 0, .4, -.6);
        Equal(linkedA.X, -.6, "bottom-left top corner translated X");
        Equal(linkedA.Z, -1.6, "bottom-left top corner translated Z");
        Equal(linkedB.X, 1.4, "top-right top corner translated X");
        Equal(linkedB.Z, .4, "top-right top corner translated Z");
        Equal(linkedB.X - linkedA.X, 2d, "linked corners retain width");
        Equal(linkedB.Z - linkedA.Z, 2d, "linked corners retain depth");
        Equal(linkedA.Y, 2d, "linked translation does not change height");
        var halfway = FaceMorphCore.Transform(new MorphPoint(0, 0, 0), min, max,
            1, 1, 0, 0, 1, 1, 0, 0, 0, 0, .4, -.6);
        Equal(halfway.X, .2, "face offset interpolates halfway in X");
        Equal(halfway.Z, -.3, "face offset interpolates halfway in Z");
        var unchanged = FaceMorphCore.Transform(new MorphPoint(0, -2, 0), min, max,
            1, 1, 0, 0, 1, 1, 0, 0, 0, 0, .4, -.6);
        Equal(unchanged.X, 0, "moving top does not change bottom X");
        Equal(unchanged.Z, 0, "moving top does not change bottom Z");
        Equal(FaceMorphCore.SnapDelta(0.074, .05), .05, "0.05 snap step");
        Equal(FaceMorphCore.SnapDelta(0.074, .01), .07, "0.01 snap step");
        Equal(FaceMorphCore.SnapDelta(0.074, .10), .1, "0.10 snap step");
        Equal(FaceMorphCore.SnapDelta(0.074, .25), 0, "0.25 snap step");
        Equal(FaceMorphCore.SnapDelta(0, .05), 0, "zero motion preserves baseline");
        Equal(FaceMorphCore.SnapDelta(-.074, .05), -.05, "negative snap symmetric");
        // Ctrl+Alt: a single factor moves all 4 corners radially.
        // An equivalent inward drag from opposite corners must give the
        // same resulting face, not translate it or invert either axis.
        double inward = FaceMorphCore.RadialScaleFactor(1, 1, -.2, -.2, 1, 1, 0);
        Equal(inward, .8, "radial inward scales both axes");
        Equal(FaceMorphCore.RadialScaleFactor(-1, -1, .2, .2, 1, 1, 0), .8,
            "opposite corner inward gives same scale");
        Equal(FaceMorphCore.RadialScaleFactor(-1, 1, -.2, .2, 1, 1, 0), 1.2,
            "different quadrant outward gives same expansion");
        Equal(FaceMorphCore.RadialScaleFactor(1, 1, .2, -.2, 1, 1, 0), 1,
            "tangential motion does not change face size");
        // A rectangular face MUST preserve its proportions.
        double rect = FaceMorphCore.RadialScaleFactor(2, 1, -.4, -.2, 2, 1, 0);
        Equal(rect, .8, "rectangular radial factor");
        Equal(2 * rect / (1 * rect), 2, "rectangular aspect ratio unchanged");
        Equal(FaceMorphCore.RadialScaleFactor(1, 1, -.074, -.074, 1, 1, .01), .93,
            "radial 0.01 snap");
        Equal(FaceMorphCore.RadialScaleFactor(1, 1, -.074, -.074, 1, 1, .05), .95,
            "radial 0.05 snap");
        Equal(FaceMorphCore.RadialScaleFactor(1, 1, -.074, -.074, 1, 1, .10), .9,
            "radial 0.10 snap");
        Equal(FaceMorphCore.RadialScaleFactor(1, 1, -.074, -.074, 1, 1, .25), 1,
            "radial 0.25 snap");
        Equal(FaceMorphCore.RadialScaleFactor(1, 1, 0, 0, 1, 1, .05), 1,
            "radial zero-drag does not jump");
        Equal(FaceMorphCore.RadialScaleFactor(1, 1, -9, -9, 2, 1, 0), .25,
            "radial contraction respects most restrictive lower bound");
        Equal(FaceMorphCore.RadialScaleFactor(1, 1, 9, 9, 2, 1, 0), 2,
            "radial growth respects most restrictive upper bound");
        // A normal Morph corner drag with Lock X/Z uses this same shared
        // factor, even for an already non-square face and Shift snapping.
        double lockFactor = FaceMorphCore.RadialScaleFactor(2, 1, -.16, -.08, 2, 1, .05);
        Equal(lockFactor, .9, "locked X/Z snapped multiplier");
        Equal((2 * lockFactor) / (1 * lockFactor), 2, "locked X/Z preserves initial ratio");
        Equal(FaceMorphCore.RadialScaleFactor(4, 1, 4, 1, 4, 1, 0), 1,
            "locked X/Z cannot exceed either axis max");
        Equal(FaceMorphCore.RadialScaleFactor(1, 1, -.05, -.05, 1, 1, .25), 1,
            "locked X/Z snap starts at native dimensions");
        // Core geometry still uses unchanged offsets and tilt when only
        // widths/depths change. Top scales while bottom stays unchanged.
        var radialTop = FaceMorphCore.Transform(new MorphPoint(1, 2, 1), min, max,
            1, 1, 0, 0, inward, inward, 0, 0, 0, 0, .4, -.6);
        Equal(radialTop.X, 1.2, "radial top point moves toward offset center X");
        Equal(radialTop.Z, .2, "radial top point moves toward offset center Z");
        var radialBottom = FaceMorphCore.Transform(new MorphPoint(1, -2, 1), min, max,
            1, 1, 0, 0, inward, inward, 0, 0, 0, 0, .4, -.6);
        Equal(radialBottom.X, 1, "radial resize leaves opposite face X unchanged");
        Equal(radialBottom.Z, 1, "radial resize leaves opposite face Z unchanged");
        // Attach-node orientations are face SURFACE normals. A naive
        // short-line transform leaves Y-oriented nodes vertical under tilt.
        // The correct answer is the inverse-transpose normal at that face.
        var topNormal = FaceMorphCore.TransformNormal(new MorphPoint(0, 2, 0),
            new MorphPoint(0, 1, 0), min, max,
            1, 1, 0, 0, 2, .5, .3, -.2, 0, 0, .4, -.6);
        double topNormalLength = Math.Sqrt(.15 * .15 + 1 + .4 * .4);
        Equal(topNormal.X, -.15 / topNormalLength, "tilted top node normal X");
        Equal(topNormal.Y, 1 / topNormalLength, "tilted top node normal Y");
        Equal(topNormal.Z, .4 / topNormalLength, "tilted top node normal Z");
        // A cloned part must build its mesh from native prefab corners, not
        // from the source part's already-morphed MeshFilter.sharedMesh.
        // The top face normal should be perpendicular to BOTH deformed edges.
        var cloneNativeA = FaceMorphCore.Transform(new MorphPoint(-1, 2, -1), min, max,
            1, 1, 0, 0, 2, .5, .3, -.2, 0, 0, .4, -.6);
        var cloneNativeX = FaceMorphCore.Transform(new MorphPoint(1, 2, -1), min, max,
            1, 1, 0, 0, 2, .5, .3, -.2, 0, 0, .4, -.6);
        var cloneNativeZ = FaceMorphCore.Transform(new MorphPoint(-1, 2, 1), min, max,
            1, 1, 0, 0, 2, .5, .3, -.2, 0, 0, .4, -.6);
        var edgeX = new MorphPoint(cloneNativeX.X - cloneNativeA.X,
            cloneNativeX.Y - cloneNativeA.Y, cloneNativeX.Z - cloneNativeA.Z);
        var edgeZ = new MorphPoint(cloneNativeZ.X - cloneNativeA.X,
            cloneNativeZ.Y - cloneNativeA.Y, cloneNativeZ.Z - cloneNativeA.Z);
        // edgeZ x edgeX points outward (+Y) for the top of the tank.
        double crossX = edgeZ.Y * edgeX.Z - edgeZ.Z * edgeX.Y;
        double crossY = edgeZ.Z * edgeX.X - edgeZ.X * edgeX.Z;
        double crossZ = edgeZ.X * edgeX.Y - edgeZ.Y * edgeX.X;
        double crossLength = Math.Sqrt(crossX * crossX + crossY * crossY + crossZ * crossZ);
        Equal(crossX / crossLength, topNormal.X, "clone face normal aligned with native mesh X");
        Equal(crossY / crossLength, topNormal.Y, "clone face normal aligned with native mesh Y");
        Equal(crossZ / crossLength, topNormal.Z, "clone face normal aligned with native mesh Z");
        var bottomNormal = FaceMorphCore.TransformNormal(new MorphPoint(0, -2, 0),
            new MorphPoint(0, -1, 0), min, max,
            1, 1, 0, 0, 2, .5, .3, -.2, 0, 0, .4, -.6);
        Equal(bottomNormal.X, 0, "bottom node unaffected by top tilt X");
        Equal(bottomNormal.Y, -1, "bottom node unaffected by top tilt Y");
        Equal(bottomNormal.Z, 0, "bottom node unaffected by top tilt Z");
        var topDown = FaceMorphCore.TransformNormal(new MorphPoint(0, 2, 0),
            new MorphPoint(0, -1, 0), min, max,
            1, 1, 0, 0, 2, .5, .3, -.2, 0, 0, .4, -.6);
        Equal(topDown.X, -topNormal.X, "opposite stack node normal X");
        Equal(topDown.Y, -topNormal.Y, "opposite stack node normal Y");
        Equal(topDown.Z, -topNormal.Z, "opposite stack node normal Z");
        var flatWithTranslation = FaceMorphCore.TransformNormal(
            new MorphPoint(0, 2, 0), new MorphPoint(0, 1, 0), min, max,
            1, 1, 0, 0, 2, .5, 0, 0, 0, 0, .4, -.6);
        Equal(flatWithTranslation.X, 0, "face translation never rotates top node X");
        Equal(flatWithTranslation.Y, 1, "face translation never rotates top node Y");
        Equal(flatWithTranslation.Z, 0, "face translation never rotates top node Z");
        var resetNormal = FaceMorphCore.TransformNormal(new MorphPoint(0, 2, 0),
            new MorphPoint(0, 1, 0), min, max,
            1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0);
        Equal(resetNormal.Y, 1, "reset restores native attach node orientation");
        for (int i = 0; i < 50; ++i)
            Equal(FaceMorphCore.TransformNormal(new MorphPoint(0, 2, 0),
                new MorphPoint(0, 1, 0), min, max,
                1, 1, 0, 0, 2, .5, .3, -.2, 0, 0, .4, -.6).X,
                topNormal.X, "normal absolutely derived from original on every drag");
        Equal(FaceMorphCore.VolumeFactor(1, 1, 1, 1), 1,
            "neutral morph has unit volume");
        Equal(FaceMorphCore.VolumeFactor(1, 1, .5, .5), 7d/12d,
            "taper volume integrates cross sections");
        Equal(FaceMorphCore.VolumeFactor(2, 1, 2, 1), 2,
            "uniformly expanded face doubles approximate volume");
    }

    private static void MFTVolume()
    {
        Equal(ScaleMath.MFTManagedVolume(1000, 2, 1), 2000,
            "MFT stretched height doubles allocated volume");
        Equal(ScaleMath.MFTManagedVolume(1000, 1, 1), 1000,
            "MFT reset returns native volume");
        Equal(ScaleMath.MFTManagedVolume(1000, 2, 1), 2000,
            "MFT repeated update is absolute, not compounded");
        Equal(ScaleMath.MFTManagedVolume(1000, 2, 1.953125), 3906.25,
            "MFT stretch with TweakScale uniform volume");
        Equal(ScaleMath.MFTManagedVolume(1000, 0.5, 1), 500,
            "MFT shrink from immutable baseline");
        ++checks;
        if (!double.IsNaN(ScaleMath.MFTManagedVolume(0, 2, 1)))
            throw new Exception("Missing MFT baseline should not be used");
    }

    private static int Main()
    {
        try
        {
            MFTVolume();
            RealFuelsTotalVolume();
            Scaling();
            StageDeltaVStableMass();
            Presets();
            Nodes();
            Gizmo();
            Attachments();
            LoadedStackAlignment();
            Docking();
            FuelSwitches();
            B9VolumeScaling();
            CrewCapacity();
            FaceMorph();
            MorphEdgeHandles();
        ProjectedMidpointDrag();
            Console.WriteLine("PASS: " + checks + " production math regression checks");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("FAIL: " + e);
            return 1;
        }
    }
}
