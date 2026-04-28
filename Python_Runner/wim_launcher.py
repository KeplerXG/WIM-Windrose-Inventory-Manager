"""
Deprecated.

``wim_launcher`` is merged into ``run_wim.py``. Use::

  cd Python_Runner
  python run_wim.py --pick-profile

This file redirects for old shortcuts/scripts.
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path


def main() -> int:
    target = Path(__file__).resolve().parent / "run_wim.py"
    return subprocess.call([sys.executable, str(target), "--pick-profile", *sys.argv[1:]])


if __name__ == "__main__":
    raise SystemExit(main())
