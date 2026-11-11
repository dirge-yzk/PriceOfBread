#!/usr/bin/env bash
# (853) 旗 0 の基線・ヌル腕で G4m の印の割合を数える（G4m の閾値の元）
export PYTHONUTF8=1
S=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
for d in base_ins n1 n2; do python -X utf8 -I "$S/gates_ss853.py" --mask-share D:/PoB_work/keep_852/$d 10 || exit 1; done
