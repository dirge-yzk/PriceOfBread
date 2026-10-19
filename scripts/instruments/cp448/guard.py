"""(448) 負債化の行番号の検査。--src にその木の Simulation.cs を渡すと、--line の行が `outside: World.Outside.Credit` の移し方か確かめる。"""
import sys


def check_line(src, line):
    if src is None:
        print("# 注意：--src なし＝行番号が木のものか検査していない（B の読みでは必須）", file=sys.stderr)
        return
    with open(src, encoding="utf-8", newline="") as fh:
        lines = fh.read().split(chr(10))
    if line < 1 or line > len(lines) or "outside: World.Outside.Credit" not in lines[line - 1]:
        raise SystemExit(f"--line {line} は {src} の負債化の行でない（grep -n 'outside: World.Outside.Credit' で探し直す）")
