# -*- coding: utf-8 -*-
"""(715) prd3 の門の読み手（読むだけ）。事前登録 measurements/prd3_715_20261031_prereg.txt の G1a・G1c・G2・G3・G4 と記録を、腕 B の計器の出力から出す。
使い方：python gates.py <腕の出力フォルダ（prds_s・prdf_s がある所か、その下の ins）> <ラベル> <種の起点 1|11>
自己点検：keep_711 の基線・ヌル腕に当てると（python gates.py D:/PoB_work/keep_711/base_ins base 1）G1a・G1c・G4 は全て〈満たさない〉（旗 0 で今の主軸の共産主義は 0 ＝ヌル腕の空振り 0/3）。
門の値は keep_711 の影の N から機械的に導く（s0.py の ⑥ と同じ計算）。手で触らない。
"""
import sys, collections
from common import *

root, label, s0 = sys.argv[1], sys.argv[2], int(sys.argv[3])
refs = ['base', 'n1', 'n2'] if s0 == 1 else ['a11']


def shadow_stats(arm):
    rroot, rs0 = ARMS[arm]
    per = []
    for s in seeds(rs0):
        ix, rows = read(path(rroot, 'prds', s))
        n = k = 0
        for x in rows:
            if float(x[ix['eraComm']]) != 1.0:
                continue
            n += 1
            k += int(x[ix['nBodyComShadow']]) >= 1
        per.append((n, k))
    return per


ref = {a: shadow_stats(a) for a in refs}
pooled = [sum(k for n, k in ref[a]) / sum(n for n, k in ref[a]) for a in refs]
seedmin = min(k / n for a in refs for (n, k) in ref[a])
T_POOL, T_SEED = 0.5 * min(pooled), 0.5 * seedmin

per = []
cell = collections.defaultdict(lambda: [0, 0])
uk = tot = 0
loc_ok = loc_n = 0
vo = dict(Lo=[0.0, 0.0], Mid=[0.0, 0.0], Hi=[0.0, 0.0])   # [Σ popK, Σ popK×share]
has_voice = True
for s in seeds(s0):
    ix, rows = read(path(root, 'prds', s))
    has_voice = has_voice and 'comShareVoiceLo' in ix
    n = k = 0
    for x in rows:
        if float(x[ix['eraComm']]) != 1.0:
            continue
        n += 1
        real = int(x[ix['nBodyComReal']]) >= 1
        k += real
        c = cell[(int(x[ix['polityCode']]), int(x[ix['unionLaw']]))]
        c[0] += 1
        c[1] += real
        if real:
            tot += 1
            uk += x[ix['nation']] == UK
            loc_n += 1
            loc_ok += int(x[ix['unionLaw']]) in (0, 1)
        if 'comShareVoiceLo' in ix:
            for b in ('Lo', 'Mid', 'Hi'):
                pk = float(x[ix['popVoice%sK' % b]])
                vo[b][0] += pk
                vo[b][1] += pk * float(x[ix['comShareVoice%s' % b]])
    per.append((n, k))

N = sum(n for n, k in per)
K = sum(k for n, k in per)
share = K / N if N else float('nan')
seeds_ok = sum(1 for n, k in per if n and k / n >= T_SEED)
print(f'# prd3 の門 ラベル={label} 入力={root} 種={s0}〜{s0 + 9}')
print(f'## G1a（入口の大きさ）：腕の nBodyComReal≧1 の割合（共産主義を見つけた行）{100 * share:.2f}% ≧ {100 * T_POOL:.2f}% （影の最小の半分）→ {"満たす" if share >= T_POOL else "満たさない"}')
print(f'##   種ごと：' + ' '.join(f'{100 * k / n if n else float("nan"):.1f}' for n, k in per))
print(f'## G1c（種ごと）：{100 * T_SEED:.1f}% 以上の種 {seeds_ok}/10 ≧ 8 → {"満たす" if seeds_ok >= 8 else "満たさない"}')
print(f'## G3（場所）：nBodyComReal≧1 の行 {loc_n} のうち組合が非合法か統制 {loc_ok}（{100 * loc_ok / loc_n if loc_n else float("nan"):.1f}%）≧ 90% → {"満たす" if loc_n and loc_ok / loc_n >= 0.9 else "満たさない（行が無い場合も）"}')
if has_voice and all(vo[b][0] > 0 for b in vo):
    m = {b: vo[b][1] / vo[b][0] for b in vo}
    ok = m['Lo'] > 0 and m['Hi'] <= 0.5 * m['Lo']
    print(f'## G2（配線の確認・働いた証拠にしない）：共産主義の重みの平均 低 {m["Lo"]:.4f}・中 {m["Mid"]:.4f}・高 {m["Hi"]:.4f}（人数加重）高≦低の 1/2 かつ 低>0 → {"満たす" if ok else "満たさない"}')
else:
    print('## G2：列（comShareVoice*）が無い＝評価できない（旗 0 の keep_711 の生には無い）')
# G4 と記録（prdf_s）
seeds_pd = 0
tgt = collections.Counter()
pol6_rows = 0
pairs = {}
for s in seeds(s0):
    ix, rows = read(path(root, 'prdf', s))
    has = False
    for x in rows:
        nm = x[ix['nation']]
        if int(x[ix['polityCode']]) == PD:
            pol6_rows += 1
            pairs.setdefault((s, nm), int(x[ix['year']]))
        n = int(x[ix['revoltN']])
        if n > 0:
            t = int(x[ix['revoltTarget']])
            tgt[t] += n
            has = has or t == PD
    seeds_pd += has
print(f'## G4（先）：目標が政体 6 の蜂起のある種 {seeds_pd}/10 ≧ 3 → {"満たす" if seeds_pd >= 3 else "満たさない"}')
print('## 記録')
print(f'  蜂起の目標別 {dict(sorted(tgt.items()))}')
print(f'  政体 6 の行（国×四半期）{pol6_rows}・政体 6 に届いた（種×国）{len(pairs)}・先進国（広）{sum(1 for (s, nm) in pairs if nm in ADV_WIDE)}（1 試合あたり {sum(1 for (s, nm) in pairs if nm in ADV_WIDE) / 10:.2f}）')
print('  政体 6 に最初に届いた年（種・国・年）：' + '；'.join(f'{s}{nm}{y}' for (s, nm), y in sorted(pairs.items(), key=lambda kv: kv[1])[:30]))
print(f'  N≧1 の行のうち英 {uk}/{tot}（{100 * uk / tot if tot else float("nan"):.1f}%）')
print('  政体×組合法ごとの腕の割合 %（基準＝影の割合の最小〜最大〔' + '・'.join(refs) + '〕）')
rc = {a: collections.defaultdict(lambda: [0, 0]) for a in refs}
for a in refs:
    rroot, rs0 = ARMS[a]
    for s in seeds(rs0):
        ix, rows = read(path(rroot, 'prds', s))
        for x in rows:
            if float(x[ix['eraComm']]) != 1.0:
                continue
            c = rc[a][(int(x[ix['polityCode']]), int(x[ix['unionLaw']]))]
            c[0] += 1
            c[1] += int(x[ix['nBodyComShadow']]) >= 1
for kx in sorted(cell):
    n, kk = cell[kx]
    rr = [100.0 * rc[a][kx][1] / rc[a][kx][0] for a in refs if rc[a][kx][0]]
    rs = f'{min(rr):.1f}〜{max(rr):.1f}' if rr else '—'
    print(f'    {POL.get(kx[0], kx[0])}・{LAW.get(kx[1], kx[1])}：行 {n}・腕 {100.0 * kk / n:.1f}%・影 {rs}')
