#!/usr/bin/env python
# (827) cearn_s の経路の列（inTop1Route・outTop1Route・outTop2Route＝〈ファイル:行:動詞:種〉）の違いが、MultiNationSim.cs の行の移動だけかを確かめる（読むだけ）。
#   使い方: python -I cearnloc827.py <基準 ins> <腕 ins> <差分の木の旧コミット> <新コミット>（git diff の hunk から旧→新の行の写像を作る）
#   判定: 違う全てのセルが〈同じファイル・同じ動詞と種・行だけが写像どおり〉なら「位置だけ」。それ以外が 1 つでもあれば「中身の違い」。
import sys, os, glob, re, subprocess
sys.stdout.reconfigure(encoding='utf-8')
base, arm, c0, c1 = sys.argv[1:5]
COLS = ('inTop1Route', 'outTop1Route', 'outTop2Route')
diff = subprocess.run(['git', 'diff', '-U0', c0, c1, '--', 'PriceOfBread.SimCore/MultiNationSim.cs'], capture_output=True, text=True, encoding='utf-8').stdout
hunks = [tuple(int(x or 1) for x in m.groups()) for m in re.finditer(r'^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@', diff, re.M)]
def mapline(n):   # 旧の行 n → 新の行（変えた行の中なら None）
    shift = 0
    for os_, ol, ns, nl in hunks:
        ol = ol if ol else 0
        if ol == 0:   # 足しただけの hunk：旧の os_ の後に入る
            if n > os_: shift += nl
            continue
        if os_ <= n < os_ + ol: return None
        if n >= os_ + ol: shift += nl - ol
    return n + shift
# 足しただけの hunk の ol は 0（-U0 の書式 -a,0）。上の int(x or 1) で 0 が 1 に化けないよう取り直す
hunks = []
for m in re.finditer(r'^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@', diff, re.M):
    os_, ol, ns, nl = m.group(1), m.group(2), m.group(3), m.group(4)
    hunks.append((int(os_), 1 if ol is None else int(ol), int(ns), 1 if nl is None else int(nl)))
print('hunks', hunks)
tot = loc = bad = 0; ex = []
for fb in sorted(glob.glob(os.path.join(base, 'cearn_s*.tsv'))):
    fa = os.path.join(arm, os.path.basename(fb))
    A = open(fb, encoding='utf-8').read().split('\n'); B = open(fa, encoding='utf-8').read().split('\n')
    if len(A) != len(B): print('行数が違う', fb, len(A), len(B)); bad += 1; continue
    h = A[0].split('\t'); idx = [h.index(c) for c in COLS]
    if A[0] != B[0]: print('見出しが違う', fb); bad += 1; continue
    for x, y in zip(A[1:], B[1:]):
        if x == y: continue
        X, Y = x.split('\t'), y.split('\t')
        for i, (p, q) in enumerate(zip(X, Y)):
            if p == q: continue
            tot += 1
            if i not in idx: bad += 1; ex.append((os.path.basename(fb), h[i], p, q)); continue
            pp, qq = p.split(':'), q.split(':')
            if len(pp) == len(qq) >= 2 and pp[0] == qq[0] == 'MultiNationSim.cs' and pp[2:] == qq[2:] and mapline(int(pp[1])) == int(qq[1]):
                loc += 1
            else:
                bad += 1; ex.append((os.path.basename(fb), h[i], p, q))
print(f'違うセル {tot}・位置だけ（MultiNationSim.cs の行の写像どおり）{loc}・それ以外 {bad}')
for e in ex[:20]: print('  ', e)
print('判定:', '位置だけ' if bad == 0 and tot > 0 else ('違い無し' if tot == 0 else '中身の違い'))
