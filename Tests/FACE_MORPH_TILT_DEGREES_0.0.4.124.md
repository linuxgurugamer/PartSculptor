# Face morph tilt-angle display — 0.0.4.124

Use the original 0.0.4.113 behavior as the regression baseline. This update changes readouts only.

1. In VAB, place an FL-T400 and enable **Show face morph handles**. Both PAW rows and the panel should initially display **0.0°** for both faces.
2. Hold **Alt** and tilt an edge on the upper face. During the drag the tooltip should show **Top tilt** in degrees increasing/decreasing continuously; the upper PAW row must reflect the accepted final value. The bottom remains at zero.
3. Tilt the lower face and check it reports independently. Check X/Z components (signed) when pulling along either direction; combined tilt reports a larger total angle.
4. Resize the *same tilted face* in X or Z. Its physical angle should change because the height rise is distributed across a wider or narrower span; the tooltip and PAW should agree.
5. Apply independent X/Y/Z stretch after tilting. Y stretch increases the displayed inclination; X stretch reduces the X contribution; Z stretch reduces the Z contribution.
6. Right-click **Reset face morph** and verify both faces return to 0.0°. Undo, cancel a drag with Esc, and reload a saved craft; the angles should always represent the persisted/active face geometry.
7. With two tanks and **Copy attached morph steps**, verify that each part's readouts update independently when its face is morphed. No new movement/joint-positioning logic should be introduced.

Angle definition: signed X = atan(TiltX * scaleY / (Width * scaleX)), signed Z = atan(TiltZ * scaleY / (Depth * scaleZ)); total = atan(sqrt(slopeX² + slopeZ²)), all in degrees. Whole-part rotation is excluded.
