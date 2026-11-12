"""0.0.4.65: don't suppress Morph handles on Canvas existence, and draw Stretch in front."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MORPH = (ROOT / 'PartSculptor/FaceMorphGizmo.cs').read_text()
STRETCH = (ROOT / 'PartSculptor/ScaleGizmo.cs').read_text()
EDITOR = (ROOT / 'PartSculptor/EditorStretchTool.cs').read_text()
MODULE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()

class BothGizmoOcclusion465(unittest.TestCase):
    def test_morph_screen_markers_draw_unconditionally_at_repaint(self):
        self.assertIn('if (Event.current.type == EventType.Repaint)', MORPH)
        self.assertNotIn('screenOverlay == null', MORPH)
        self.assertNotIn('CreateScreenOverlay', MORPH)
        self.assertIn('GUI.DrawTexture(new Rect(screen[i].x - radius', MORPH)
        self.assertIn('GUI.DrawTexture(new Rect(screen[i].x - radius - 2f', MORPH)
        self.assertIn('DrawScreenLine(', MORPH)

    def test_morph_cage_uses_same_projection_as_picking(self):
        self.assertIn('new Vector2[HandleCount]', MORPH)
        self.assertIn('new bool[HandleCount]', MORPH)
        self.assertIn('screen[i] = new Vector2(projected.x, projected.y);', MORPH)
        self.assertIn('(screen[i] - mouse).sqrMagnitude', MORPH)
        self.assertIn('screen[a].x, Screen.height - screen[a].y', MORPH)
        self.assertIn('screen[b].x, Screen.height - screen[b].y', MORPH)

    def test_stretch_draws_its_axes_onscreen_above_attached_parts(self):
        self.assertIn('private Vector2 screenCenter;', STRETCH)
        self.assertIn('screenCenter = new Vector2(centerScreen.x, centerScreen.y);', STRETCH)
        self.assertIn('DrawForegroundLine(from, handle, Colors[i]);', STRETCH)
        self.assertIn('GUI.DrawTexture(new Rect(handle.x-half', STRETCH)
        self.assertIn('screenHandles[i].x', STRETCH)
        self.assertIn('handleVisible[i]', STRETCH)

    def test_both_drawn_by_editor_addon_not_part_module(self):
        self.assertIn('FaceMorphGizmo.DrawActive(helpRect);', EDITOR)
        self.assertIn('ScaleGizmo.DrawActive(helpRect);', EDITOR)
        self.assertNotIn('ScaleGizmo.DrawFor(this);', MODULE)
        self.assertLess(EDITOR.index('FaceMorphGizmo.DrawActive(helpRect);'),
                        EDITOR.index('ScaleGizmo.DrawActive(helpRect);'))

    def test_overlays_are_visual_only_and_dispose_cleanly(self):
        self.assertNotIn('RawImage', MORPH)
        self.assertNotIn('GraphicRaycaster', MORPH + STRETCH)
        self.assertIn('Lock(hovered >= 0);', MORPH)
        self.assertIn('LockInput(hoveredAxis >= 0);', STRETCH)
        self.assertIn('UnityEngine.Object.Destroy(root)', MORPH)
        self.assertIn('UnityEngine.Object.Destroy(root)', STRETCH)
        self.assertNotIn('screenOverlay', MORPH)

    def test_no_changes_to_drag_modes(self):
        self.assertIn('dragRadial = alt && ctrl;', MORPH)
        self.assertIn('dragLinked = alt && shift && !ctrl;', MORPH)
        self.assertIn('dragTilt = alt && !shift && !ctrl;', MORPH)
        self.assertIn('dragLockXZ = !alt && module.lockMorphXZ;', MORPH)
        self.assertIn('module.SetGizmoAxis(draggingAxis, desired,\n                    temporaryLockProportions, temporaryLockXZ);', STRETCH)

    def test_version_metadata(self):
        for p in (ROOT/'PartSculptor.version',ROOT/'GameData/PartSculptor/PartSculptor.version'):
            self.assertRegex(p.read_text(), r'"BUILD": 130')
        assembly=(ROOT/'PartSculptor/AssemblyVersion.cs').read_text()
        self.assertIn('AssemblyVersion("0.0.4.130")', assembly)
        self.assertIn('AssemblyFileVersion("0.0.4.130")', assembly)

if __name__ == '__main__': unittest.main()
