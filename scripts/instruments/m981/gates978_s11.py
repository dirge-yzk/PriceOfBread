# (981) 合流：c88cs/gates978.py の門 G-S1〜G-S4 を種 11〜20 の生で読む薄い呼び手（規則は 1 字も変えない・種の並び SEEDS だけを差し替える）。
#   python -X utf8 -I gates978_s11.py <腕の計器の生> <出力.txt> [<基準の生>]
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'c88cs'))
import gates978 as g
g.SEEDS = range(11, 21)
sys.exit(g.gates(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None))
