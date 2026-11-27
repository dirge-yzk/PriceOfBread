# -*- coding: utf-8 -*-
"""(1407) c150rp 腕 B の機構の観測量の門 G1〜G3（事前登録 measurements/m1407_20261127_prereg_rp.txt [4]）。読むだけ。

使い方：python gate_rp.py <名前>=<ins フォルダ> ...（trrail_s<種>.tsv を列名で引く）
  国の中から買った単位（行ごと）＝boughtCapPerYear÷30−procureGotUnitsPerYear
  G1：種の和の国の中の単位 ≧ 種の州の調達の和（procureGotUnitsPerYear）の 1% の種が 8/10 以上
  G2：国の中の単位 ＞ (procureWantUnitsPerYear−procureGotUnitsPerYear)×(1＋1e-6)＋1e-6 の行 0
  G3：g2Viol の和 0
"""
import sys, os, csv, glob

CAP = 30.0


def fl(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


for arg in sys.argv[1:]:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[len('trrail_s'):-4]) for p in glob.glob(os.path.join(d, 'trrail_s*.tsv')))
    ok1 = 0; g2 = 0; g3 = 0
    print(f'[{name}] seed\tnatUnits\tlocalGot\tnatShare\twantMinusGot\tnatRows')
    for s in seeds:
        nat = loc = gap = 0.0; rows = 0
        with open(os.path.join(d, f'trrail_s{s}.tsv'), encoding='utf-8-sig', newline='') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
            for r in rd:
                b = fl(r[ix['boughtCapPerYear']]); got = fl(r[ix['procureGotUnitsPerYear']]); want = fl(r[ix['procureWantUnitsPerYear']])
                if not (b == b and got == got and want == want):
                    continue
                n = b / CAP - got
                nat += n; loc += got; gap += want - got
                if n > 1e-3: rows += 1
                if n > (want - got) * (1 + 1e-6) + 1e-6: g2 += 1
                g3 += int(fl(r[ix['g2Viol']]) or 0)
        sh = nat / loc if loc > 0 else float('nan')
        if sh >= 0.01: ok1 += 1
        print(f'{s}\t{nat:.6g}\t{loc:.6g}\t{sh:.4g}\t{gap:.6g}\t{rows}')
    print(f'[{name}] G1 {ok1}/{len(seeds)} 種（門：8/10 以上）{"通る" if ok1 >= 8 * len(seeds) / 10 else "落ちる"}・G2 破れの行 {g2}（門 0）・G3 g2Viol の和 {g3}（門 0）')
