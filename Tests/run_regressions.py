#!/usr/bin/env python3
"""Run dependency-free static checks and .NET production-math checks when available.

python Tests/run_regressions.py                 # both when dotnet is installed
python Tests/run_regressions.py --require-dotnet # fail if dotnet missing
"""
import shutil
import subprocess
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
checks = [sys.executable, '-m', 'unittest', 'discover', '-s',
          str(root / 'Tests/python_regressions'), '-p', 'test_*.py', '-v']
print('Running offline integration smoke tests...', flush=True)
if subprocess.call(checks, cwd=str(root)) != 0:
    sys.exit(1)
if not shutil.which('dotnet'):
    print('SKIPPED executable C# math tests (install .NET 8 SDK and re-run).', flush=True)
    if '--require-dotnet' in sys.argv:
        sys.exit(2)
    sys.exit(0)
print('Running production C# math tests...', flush=True)
sys.exit(subprocess.call(['dotnet', 'run', '--project',
                          str(root / 'Tests/RegressionRunner/RegressionRunner.csproj'),
                          '-c', 'Release'], cwd=str(root)))
