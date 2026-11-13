# c106dc：rg の探す先の件数と大きさ（バイト）。引数 before＝.ignore を使わない／after＝使う。
# rg --files [--no-ignore-dot] の出力を標準入力で渡す
import os, sys
mode = sys.argv[1]
files = sys.stdin.buffer.read().decode('utf-8').splitlines()
skip = {'.ignore', 'docs/README_線の担当.md'}
tot = 0
n = 0
for f in files:
    f = f.strip().replace(chr(92), '/')
    if not f:
        continue
    if mode == 'before' and (f in skip or f.startswith('scripts/instruments/c106dc/')):
        continue
    tot += os.path.getsize('D:/PoB_work/wt_c106dc/' + f)
    n += 1
print(mode, n, tot)
