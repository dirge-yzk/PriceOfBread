# -*- coding: utf-8 -*-
"""(700) prd1 影の列 prds_s の読み手（読むだけ・シミュを回さない）。
prds_s<種>.tsv（InstrumentsPrds.cs・INS=1 のとき出る）を読み、
  国×十年で〈t が正の POP の人口の割合〉〈移し替えの後に主軸が共産主義になる POP の人口〉を表にする。
影の列は旗 0 の世界の上の数＝旗 0 の世界は基線（m694）と全ビット同じなので、prds_s は基線の計器つき 10 種の影の列と同じ。
使い方：python shadow.py <計器の出力フォルダ> [種の数=10] [基線の名乗り]
"""
import sys, os, csv, collections

root = sys.argv[1]
nseed = int(sys.argv[2]) if len(sys.argv) > 2 else 10
tag = sys.argv[3] if len(sys.argv) > 3 else ''

def rows(path):
    with open(path, 'r', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for x in r:
            yield ix, x

def num(v):
    try: return float(v)
    except ValueError: return float('nan')

def mean(v): return sum(v) / len(v) if v else float('nan')
def q(v, p):
    v = sorted(v)
    if not v: return float('nan')
    return v[min(len(v) - 1, max(0, int(round(p * (len(v) - 1)))))]

POLITY = {0: '絶対王政', 1: '専制', 2: '幕府', 3: '立憲君主制', 4: '共和制', 5: 'ブルジョワ独裁', 6: 'プロ独裁', 7: '軍事独裁'}
LAW = {0: '非合法', 1: '統制', 2: '容認'}
recs = []   # (seed, year, nation, polity, law, repress, popK, tPos, tPosUrb, shK, shShare, shUrbK, realShare, tLeft, milMean, milTPos)
seeds_found = 0
for s in range(1, nseed + 1):
    p = os.path.join(root, f'prds_s{s}.tsv')
    if not os.path.exists(p): continue
    seeds_found += 1
    for ix, x in rows(p):
        g = lambda k: num(x[ix[k]])
        if g('eraComm') != 1.0: continue          # 共産主義を見つけた行だけ（見つけていない国は t が 0 で影は旗 0 と同じ）
        recs.append((s, int(g('year')), x[ix['nation']], int(g('polityCode')), int(g('unionLaw')), g('repress'), g('popK'),
                     g('tPosShare'), g('tPosUrbanShare'), g('shadowComAxisK'), g('shadowComAxisShare'), g('shadowComAxisUrbanK'),
                     g('realComAxisShare'), g('tShareLeft'), g('milMean'), g('milMeanTPos')))

out = []
P = out.append
P(f'# prd1 (700) 影の列 prds_s の表（読み手 scripts/instruments/prd1/shadow.py・読むだけ）{(" 基線＝" + tag) if tag else ""}')
P(f'# 入力＝{root}　種 {seeds_found}　共産主義を見つけた行（eraComm＝1）{len(recs)} 行（全ての国）')
P('# 影＝旗 0 の世界の上で、式 t＝eraComm·repress·mil·r[5] の移し替えを当てたら各 POP の主軸がどうなるか（世界へは書かない）。')
P('# 人数は千人。割合は人口加重。行は国×四半期（四半期の行を等しく数える）。')
P('')
# 1. 十年ごと
P('## 1. 十年ごと（共産主義を見つけた国×四半期）')
P('| 十年 | 行 | t>0 の割合の平均 | t>0 の割合 P90 | 影の共産主義主軸の割合 平均 | 影 P90 | 影 最大 | 影>0.1% の行 | 影>10% の行 | 今の共産主義主軸の割合 最大 | 影>今 の行 | 種 |')
P('|---|---|---|---|---|---|---|---|---|---|---|---|')
dec = collections.defaultdict(list)
for r in recs: dec[r[1] // 10 * 10].append(r)
for d in sorted(dec):
    v = dec[d]
    sh = [r[10] for r in v]; tp = [r[7] for r in v]; real = [r[12] for r in v]
    P(f'| {d}s | {len(v)} | {mean(tp):.4f} | {q(tp, .9):.4f} | {mean(sh):.5f} | {q(sh, .9):.5f} | {max(sh):.4f} | {sum(1 for a in sh if a > 0.001)} | {sum(1 for a in sh if a > 0.10)} | {max(real):.4f} | {sum(1 for r in v if r[10] > r[12] + 1e-9)} | {len(set(r[0] for r in v))} |')
P('')
# 2. 政体別
P('## 2. 政体ごと（全ての十年）')
P('| 政体 | 行 | t>0 の割合の平均 | 影の共産主義主軸の割合 平均 | 影>0.1% の行 | 影>10% の行 |')
P('|---|---|---|---|---|---|')
pol = collections.defaultdict(list)
for r in recs: pol[r[3]].append(r)
for k in sorted(pol):
    v = pol[k]
    P(f'| {POLITY.get(k, k)} | {len(v)} | {mean([r[7] for r in v]):.4f} | {mean([r[10] for r in v]):.5f} | {sum(1 for r in v if r[10] > 0.001)} | {sum(1 for r in v if r[10] > 0.10)} |')
P('')
# 3. 組合法別
P('## 3. 組合法ごと')
P('| 組合法 | 行 | 抑圧 | t>0 の割合の平均 | 影の共産主義主軸の割合 平均 | 影>0.1% の行 | 影>10% の行 |')
P('|---|---|---|---|---|---|---|')
lw = collections.defaultdict(list)
for r in recs: lw[r[4]].append(r)
for k in sorted(lw):
    v = lw[k]
    P(f'| {LAW.get(k, k)} | {len(v)} | {mean([r[5] for r in v]):.2f} | {mean([r[7] for r in v]):.4f} | {mean([r[10] for r in v]):.5f} | {sum(1 for r in v if r[10] > 0.001)} | {sum(1 for r in v if r[10] > 0.10)} |')
P('')
# 4. 国別（影>0.1% の行の数が多い順）
P('## 4. 国ごと（影>0.1% の行が多い順の上位 20・全ての十年・種をまたいだ行の数）')
P('| 国 | 行 | 影>0.1% の行 | 影>10% の行 | 影の平均 | 影の最大 | t>0 の割合の平均 | 行にいる種 | 影>0.1% の種 |')
P('|---|---|---|---|---|---|---|---|---|')
nat = collections.defaultdict(list)
for r in recs: nat[r[2]].append(r)
rank = sorted(nat, key=lambda k: -sum(1 for r in nat[k] if r[10] > 0.001))
tot_pos = sum(1 for r in recs if r[10] > 0.001)
for k in rank[:20]:
    v = nat[k]
    P(f'| {k} | {len(v)} | {sum(1 for r in v if r[10] > 0.001)} | {sum(1 for r in v if r[10] > 0.10)} | {mean([r[10] for r in v]):.5f} | {max(r[10] for r in v):.4f} | {mean([r[7] for r in v]):.4f} | {len(set(r[0] for r in v))} | {len(set(r[0] for r in v if r[10] > 0.001))} |')
P('')
P(f'影>0.1% の行の全体 {tot_pos}。イギリスの割合 {sum(1 for r in recs if r[10] > 0.001 and r[2] == "イギリス") / tot_pos:.3f}' if tot_pos else '影>0.1% の行は 0')
P('')
# 5. 国×十年の表（影の共産主義主軸の割合の平均・上位 12 の国）
P('## 5. 国×十年（影の共産主義主軸の割合の平均・上の順位の上位 12 の国・行が無い所は -）')
decs = sorted(dec)
P('| 国 | ' + ' | '.join(f'{d}s' for d in decs) + ' |')
P('|---|' + '---|' * len(decs))
for k in rank[:12]:
    cells = []
    for d in decs:
        v = [r[10] for r in nat[k] if r[1] // 10 * 10 == d]
        cells.append(f'{mean(v):.4f}' if v else '-')
    P(f'| {k} | ' + ' | '.join(cells) + ' |')
P('')
P('## 6. 計器の限界（自己申告）')
P('- 運動体のセル（州×束×主軸）の足切り 1% は読まない＝影の N（nBodyCom）は出さない。人数の割合だけ。')
P('- 影は旗 0 の世界の上の数＝腕 B の帰結（闘争度の応答・理想点の変化・政体の遷移）は入らない。')
P('- 行は eraComm＝1 の国×四半期だけ（共産主義を見つけた国）。表の割合は行の等しい重み（人口の大小の国を同じに数える）。')
print('\n'.join(out))
