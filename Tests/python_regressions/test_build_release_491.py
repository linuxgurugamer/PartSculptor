"""Check the requested release 7-Zip executable."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]

class BuildReleasePath491(unittest.TestCase):
    def test_buildrelease_uses_requested_7zip_path(self):
        script = (ROOT / "buildRelease.bat").read_text(encoding="utf-8")
        self.assertIn(r'set ZIP="d:\Program Files\7-zip\7z.exe"', script)
        self.assertNotIn(r'set ZIP="c:\Program Files\7-zip\7z.exe"', script)
        self.assertIn('%ZIP% a -tzip %FILE% GameData', script)
