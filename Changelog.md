Changelog

0.0.5.23
	Persist and restore selected scale preset and repeat command; recover matching absolute presets from older craft without rescaling

0.0.5.22
	Move PartVolumeCore and PartVolumeResult into existing FaceMorphCore.cs to avoid missing new source-file references

0.0.5.21
	Add read-only Part volume PAW field in editor and flight, measured from live visual mesh shells rather than collider hulls or resource multipliers
	Subtract modeled hollow interiors, identify open/non-manifold geometry and refresh visible PAWs after geometry changes

0.0.5.20
	Exclude EVA Kerbals, planted flags and standalone flag panels from the ModuleManager patch and ModulePartSculptor runtime initialization
	Return zero mass and cost adjustments for excluded parts

0.0.5.19
	Use Settings.screenMessageTime for all screen messages

0.0.5.18
	Put the influence radius label, slider and compact numeric entry on one row
	Add visible hover tooltips for PG mode, mirror, soft-selection and collision fallback toggles

0.0.5.17
	Limit soft-selection radius entry and slider to three decimal places
	Allow dragging Settings from anywhere not occupied by interactive controls
	Add immediate vertex collision fallback toggle below the radius controls

0.0.5.16
	Added a Settings window toggle for experimental vertex collision fallback, with immediate Apply/Save and Cancel rollback

0.0.5.15
	Added synchronized soft-selection radius slider and numeric entry
	Added optional X/Y/Z within-part mirrored vertex insertion, movement, radius editing and deletion

0.0.5.14
	Move ProceduralConvexHull into FaceMorphSupport.cs to avoid missing source-file compilation errors

0.0.5.13
	Search nearest usable native collider first, then compatible active B9 collision surfaces
	Add opt-in visual-derived convex collision fallback, with volume and cleanup
	Log collider paths, activation, eligibility and rejection reasons

0.0.5.12
	Fix individual vertex edits rejected by raw convex collider subdivision limits or inconsistent collider winding
	Build bounded convex collision hulls and calculate their enclosed volume
	Show and log the exact vertex edit rejection or unexpected drag cancellation

0.0.5.11
	Added Ctrl-click multiple vertex selection and shared centroid movement gizmo
	Group drag compensates for inherited and overlapping soft-selection displacement; commits once
	Added select all visible, deselect all, group deletion and group influence radius editing

0.0.5.10
	Doubled selected vertex gizmo length
	Added edge and procedural ring vertex insertion, splitting adjoining triangles and preserving seam attributes
	Delete selected vertices and rebuild topology from native geometry while preserving remaining edits

0.0.5.9
	Added a three-axis drag gizmo to selected PG vertices
	Added per-vertex soft selection with a configurable influence radius and smooth falloff

0.0.5.8
	Individual vertex edits now deform collision meshes and move affected attachment nodes
	Calculated volume, resources, mass and cost reflect the changed collision surface
	Validate collision subdivision and volume limits before committing an edit

0.0.5.7
	Changed PG ring tilt modifier from Shift to Alt
	Added individual surface vertex insertion and yellow draggable handles in a separate PG Vertices mode
	Individual vertex edits are saved, undoable and visual-only; ring collision and volume calculations remain unchanged

0.0.5.6
	Shift-drag side and corner procedural ring handles now tilts the selected ring like face morphing instead of moving the entire ring vertically
	Shift-drag the green center handle still moves the whole procedural ring in Y
	Procedural ring tilt values are now persisted with the craft and applied during procedural geometry deformation

0.0.5.5
	Added vertical ring movement using Shift-drag

0.0.5.4
	Added four yellow corner handles for independent two-axis X/Z procedural ring resizing

0.0.5.3
	Added draggable X, Z and height handles for procedural rings, with preview, snapping and native undo

0.0.5.2
	Fixed static TryGetCareerLimits invocation in procedural geometry

0.0.5.1
	Version change only

0.0.4.149
	Added Procedural Geometry mode via a seventh editor widget; inserts new mesh vertices and editable interior cross-sections
	Saved geometry profiles restore in the editor and flight, with symmetry, undo and collision geometry support

0.0.4.148
	Fixed post-load attachment alignment for stretched B9PartSwitch parts, including late node updates and one-sided stack links

0.0.4.147
	Improved support for B9partSwitch, now supports NODE and MODULE stanza

0.0.4.142
	Improved logging support
	Added logging of all screen messages
	Added wordwrap to the onscreen messages under the widgets

0.0.4.139
	Fixed B9 issue with nodes being lost when switching between different B9PS meshes

0.0.4.137
	Added window containing all controls in the PAW for easier access to all controls.
	Added ability to hide the PAW window and controls when not needed.
	Added ability to specify face tilt in PAW instead of dragging
	Fixed issue with morphing not working correctly when the attached part is a child of another part.
	Fixed issue when moving handle quickly and the morphing would not update correctly.
	Added attached part morphing for parts that have morphing enabled. This allows the attached part to morph along with the parent part.
	Added option to settings to have this morphing option enabled or disabled.
	Added face morphing for attached parts which adjusts the face of the attached part to match the morphed face.
	Improved support for B9partSwitch

0.0.4.134
	Added window containing all controls in the PAW for easier access to all controls.
	Added ability to hide the PAW window and controls when not needed.

0.0.4.133
	Added ability to specify face tilt in PAW instead of dragging

0.0.4.131
	Fixed issue with morphing not working correctly when the attached part is a child of another part.
	Fixed issue when moving handle quickly and the morphing would not update correctly.

0.0.4.130
	Added attached part morphing for parts that have morphing enabled. This allows the attached part to morph along with the parent part.
	Added option to settings to have this morphing option enabled or disabled.

0.0.4.109 
	Added face morphing for attached parts which adjusts the face of the attached part to match the morphed face.

0.0.4.106 
	Improved support for B9partSwitch

0.0.4.101
	Initial release
