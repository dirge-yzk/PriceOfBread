# (1138) c111tp 門の集計：tport_s（国×四半期・全ての国）を読み、G1〜G3 と計器の自己確かめと記録の数を出す。
#   使い方：python -X utf8 count_tport.py <ins> <旗 0|1> [種の数=10] [種の起点=1]
#   旗 1：G1・G2・G3 を判定する（事前登録 §4）。旗 0：同じ数を出し、G1・G3 が落ちること（何もしない実装で通らないこと）と計器の自己確かめを読む。
import sys, os
from collections import defaultdict
ins, flag = sys.argv[1], sys.argv[2]
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
SB = int(sys.argv[4]) if len(sys.argv) > 4 else 1
WATCH = ['イギリス', 'フランス', '日本', '清', 'オスマン', 'ロシア']
c = defaultdict(int); g3max = 0.0; selfmax = 0.0; seeds = 0
nat_rows = defaultdict(int); nat_eng = defaultdict(int); nat_co = defaultdict(int); nat_off = defaultdict(int)
for s in range(SB, SB + NS):
    p = os.path.join(ins, f'tport_s{s}.tsv')
    if not os.path.exists(p): print('無い', p); continue
    seeds += 1; prev = {}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            x = ln.rstrip('\n').split('\t')
            if len(x) < len(h): c['shortRows'] += 1; continue
            nat = x[ix['nation']]
            al, ho, en, co = (x[ix[k]] == '1' for k in ('treatyAllFlag', 'treatyHomeFlag', 'treatyEngineFlag', 'colonialOnlyFlag'))
            fa, fh, ts = float(x[ix['portForeignAllShare']]), float(x[ix['portForeignHomeShare']]), float(x[ix['tariffScaleEngineShare']])
            c['rows'] += 1
            if co != (al and not ho): c['colonialOnlyDef'] += 1
            if co: c['Nco'] += 1; c['NcoEngine1'] += en
            if ho: c['home1'] += 1
            if ho and not en: c['G2homeLost'] += 1
            if (not ho) and en: c['G2newNotHome'] += 1
            if al != en: c['engineNeAll'] += 1
            d3 = abs(ts - (1.0 - fh)); g3max = max(g3max, d3)
            if d3 > 1e-12: c['G3rows'] += 1
            ds = abs(ts - (1.0 - fa)); selfmax = max(selfmax, ds)
            if ds > 1e-12: c['tariffNeAll'] += 1
            if abs(fa - fh) > 1e-9: c['G3control'] += 1
            nat_rows[nat] += 1; nat_eng[nat] += en; nat_co[nat] += co
            if prev.get(nat) and not en: nat_off[nat] += 1
            prev[nat] = en
print(f'旗 {flag}・種 {seeds}・行 {c["rows"]}（列の足りない行 {c["shortRows"]}・colonialOnly の定義の食い違い {c["colonialOnlyDef"]}）')
print(f'N_co（植民地格の港だけで旧の旗）{c["Nco"]}・そのうち engine=1 {c["NcoEngine1"]}')
print(f'G2：home=1 の行 {c["home1"]}・home=1 かつ engine=0 {c["G2homeLost"]}・home=0 かつ engine=1 {c["G2newNotHome"]}')
print(f'G3：|tariffEngine−(1−homeShare)|＞1e-12 の行 {c["G3rows"]}・最大 {g3max:.3g}・対照 |all−home|＞1e-9 の行 {c["G3control"]}')
print(f'計器の自己確かめ（旧の規則とエンジン）：engine≠all の行 {c["engineNeAll"]}・|tariffEngine−(1−allShare)|＞1e-12 の行 {c["tariffNeAll"]}・最大 {selfmax:.3g}')
if flag == '1':
    g1 = c['Nco'] >= 1 and c['NcoEngine1'] == 0
    g2 = c['G2homeLost'] == 0 and c['G2newNotHome'] == 0
    g3 = c['G3rows'] == 0
    print(f'判定（旗 1）：G1 {"緑" if g1 else "赤"}・G2 {"緑" if g2 else "赤"}・G3 {"緑" if g3 else "赤"}'
          + ('' if c['home1'] else '（G2 の対照 0＝M-a を盤面で拾えない）') + ('' if c['G3control'] else '（G3 の対照 0＝M-b を盤面で拾えない）'))
else:
    g1fall = c['Nco'] == 0 or c['NcoEngine1'] > 0
    print(f'旗 0：G1 は{"落ちる" if c["NcoEngine1"] > 0 else "空（N_co=0）"}・G3 は{"落ちる" if c["G3rows"] > 0 else "落ちない"}・計器は旧の規則と{"一致" if c["engineNeAll"] == 0 and c["tariffNeAll"] == 0 else "食い違う"}')
print('国\t国×四半期\tengine の旗\t割合\tcolonialOnly\t真→偽の移り')
for nat in WATCH:
    n = nat_rows.get(nat, 0)
    print(f'{nat}\t{n}\t{nat_eng.get(nat, 0)}\t{(nat_eng.get(nat, 0) / n if n else float("nan")):.4f}\t{nat_co.get(nat, 0)}\t{nat_off.get(nat, 0)}')
print(f'全ての国\t{c["rows"]}\t{sum(nat_eng.values())}\t{(sum(nat_eng.values()) / c["rows"] if c["rows"] else float("nan")):.4f}\t{c["Nco"]}\t{sum(nat_off.values())}')
