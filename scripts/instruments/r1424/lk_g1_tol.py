# (1424) r1424：c151lk/lk_gate.py の写し（同じフォルダの lk_gate.py）を使い、種の範囲 a-b で G1（許し WP*(1+1e-9)+1e-12）と G1′（許し 2e-9）を並べ、
# 外れた月ごとに d−0.10 の大きさと、計器の印字（G10＝有効 10 桁）の丸めの幅（前と今の値の半目盛りの和）を並べる。読むだけ・コミットしない。
import sys, os, math, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lk_gate import load, groups, WP

def halfulp(txt):
    # 印字の文字列の最後の桁の半目盛り（G10 の印字＝有効 10 桁・指数の印字にも対応）
    t = txt.strip().lstrip('-')
    if 'E' in t.upper():
        m, e = t.upper().split('E'); dec = len(m.split('.')[1]) if '.' in m else 0
        return 0.5 * 10 ** (int(e) - dec)
    dec = len(t.split('.')[1]) if '.' in t else 0
    return 0.5 * 10 ** (-dec)

d = sys.argv[1]; s0, s1 = map(int, sys.argv[2].split('-'))
TOLS = {'G1': lambda: WP * (1 + 1e-9) + 1e-12, 'G1p': lambda: WP + 2e-9}
over = collections.Counter(); usable = 0; first = collections.Counter(); rows_out = []; maxabs = 0.0
for s in range(s0, s1 + 1):
    ix, rows = load(f'{d}/wexit_s{s}.tsv')
    for g, rs in groups(ix, rows).items():
        prev = None; prevtxt = None; prev_collapse = False
        for k, r in enumerate(rs):
            st = r[ix['scoreIndex']]; sc = float(st)
            if k == 0:
                for n, f in TOLS.items():
                    if abs(sc) > f(): first[n] += 1
            elif not (r[ix['forcedFlag']] == '1' or prev_collapse or abs(sc) >= 1.5 - 1e-9 or abs(prev) >= 1.5 - 1e-9):
                dd = sc - prev; usable += 1; maxabs = max(maxabs, abs(dd))
                hit = [n for n, f in TOLS.items() if abs(dd) > f()]
                for n in hit: over[n] += 1
                if hit:
                    rows_out.append((s, g, r[ix['month']], prevtxt, st, abs(dd) - WP, halfulp(prevtxt) + halfulp(st), ','.join(hit)))
            prev = sc; prevtxt = st; prev_collapse = r[ix['oldCollapseAFlag']] == '1' or r[ix['oldCollapseBFlag']] == '1'
print(f'RANGE {s0}-{s1} usable={usable} maxAbsD={maxabs:.12g}')
for n in TOLS: print(f'GATE {n} over={over[n]} firstOver={first[n]}')
print('# 外れた月：種・組・月・前の印字・今の印字・(|d|−0.10)・印字の丸めの幅（両方の半目盛りの和）・外れた門・丸めの幅の内か')
for s, g, m, a, b, ex, hu, hit in rows_out:
    print(f'OVER s{s} {g} m={m} prev={a} now={b} excess={ex:.3g} printHalfUlpSum={hu:.3g} gates={hit} withinPrint={"yes" if ex <= hu else "NO"}')
