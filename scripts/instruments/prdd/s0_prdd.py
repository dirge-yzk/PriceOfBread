# -*- coding: utf-8 -*-
"""(692) prdd 設計書の段0 の読み（読むだけ・シミュを回さない）。
基線の計器つき 10 種の生（prdf_s・monlaw_s・ideology_s・dcont_s）から、
共産主義を発見した国×四半期（prdf_s の knownCom=1）について
  ・組合法・報道法の分布
  ・国の集計の社会主義と共産主義のシェア（意識×発言権の重み＝Transition が読む物）
  ・大衆の闘争度の平均（dcont_s）
  ・主軸が社会主義の POP の人数割合（prdf_s popShareSoc）
を出し、設計書の案（移し替え t = 抑圧 R × 闘争度 × 社会主義の重み）の粗い大きさを見積もる。
粗い見積もり＝国の平均の闘争度を全ての POP に当てる（闘争度と社会主義の重みの正の相関を落とす＝下に偏る）。
使い方：python s0_prdd.py <生のフォルダ> [種の数]
"""
import sys, os, csv, statistics as st

root = sys.argv[1]
nseed = int(sys.argv[2]) if len(sys.argv) > 2 else 10

def rows(path):
    with open(path, 'r', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for x in r:
            yield ix, x

def q(v, ps):
    v = sorted(v)
    if not v: return ['-'] * len(ps)
    out = []
    for p in ps:
        k = min(len(v) - 1, max(0, int(round(p * (len(v) - 1)))))
        out.append(v[k])
    return out

UNION_LEGAL = {0: 0.0, 1: 0.5, 2: 1.0}      # Politics.cs:531 の UnionMobilization の表と同じ写像（Illegal 0・Controlled 0.5・Tolerated 1）
PRESS = {0: 0.0, 1: 0.6, 2: 1.0}            # Politics.PressFreedom（StatePressOnly 0・Censorship 0.6・FreePress 1）

recs = []
for s in range(1, nseed + 1):
    law = {}
    for ix, x in rows(os.path.join(root, f'monlaw_s{s}.tsv')):
        law[(x[ix['quarter']], x[ix['nation']])] = (int(x[ix['LaborUnion']]), int(x[ix['Censorship']]), int(x[ix['Suffrage']]), int(x[ix['polityCode']]))
    ide = {}
    for ix, x in rows(os.path.join(root, f'ideology_s{s}.tsv')):
        ide[(x[ix['quarter']], x[ix['nation']])] = (float(x[ix['ideoNationalSocialismShare']]), float(x[ix['ideoNationalCommunismShare']]),
                                                    float(x[ix['ideoPopMeanSocialismShare']]), float(x[ix['ideoPopMeanCommunismShare']]))
    dc = {}
    for ix, x in rows(os.path.join(root, f'dcont_s{s}.tsv')):
        try:
            dc[(x[ix['quarter']], x[ix['nation']])] = (float(x[ix['militancyMeanShare']]), float(x[ix['discontentMeanShare']]))
        except ValueError:
            pass
    for ix, x in rows(os.path.join(root, f'prdf_s{s}.tsv')):
        if x[ix['knownCom']] != '1':
            continue
        k = (x[ix['quarter']], x[ix['nation']])
        if k not in law or k not in ide or k not in dc:
            continue
        recs.append(dict(seed=s, year=int(x[ix['year']]), nation=k[1], union=law[k][0], press=law[k][1], suff=law[k][2], pol=law[k][3],
                         nSoc=ide[k][0], nCom=ide[k][1], mSoc=ide[k][2], mCom=ide[k][3], mil=dc[k][0], milx=max(dc[k][0], dc[k][1]),
                         shSoc=float(x[ix['popShareSoc']]), shCom=float(x[ix['popShareCom']])))

P = [0.5, 0.9, 0.99, 1.0]
print(f'# (692) prdd 段0 の読み・生={root}・種 1〜{nseed}・共産主義を発見した国×四半期で 4 表が揃う行={len(recs)}')
print(f'# 種×国={len(set((r["seed"], r["nation"]) for r in recs))}')
def show(name, key, sub=None):
    v = [r[key] for r in (sub if sub is not None else recs)]
    print(f'{name}\t中央 {q(v,P)[0]:.4g}\t90% {q(v,P)[1]:.4g}\t99% {q(v,P)[2]:.4g}\t最大 {q(v,P)[3]:.4g}')
for name, key in [('国の集計 社会主義', 'nSoc'), ('国の集計 共産主義', 'nCom'), ('POP 平均 社会主義', 'mSoc'), ('POP 平均 共産主義', 'mCom'),
                  ('大衆の闘争度の平均', 'mil'), ('主軸が社会主義の人数割合', 'shSoc'), ('主軸が共産主義の人数割合', 'shCom')]:
    show(name, key)

print('\n# 組合法×報道法の行数（0 Illegal/Controlled/Tolerated・0 StatePress/Censorship/FreePress）')
from collections import Counter
c = Counter((r['union'], r['press']) for r in recs)
for k in sorted(c): print(f'組合 {k[0]}・報道 {k[1]}\t{c[k]}')
print('\n# 政体コード別の行数（0 絶対 1 専制 2 幕府 3 立憲 4 共和 5 ブル独 6 プロ独 7 軍政）')
c = Counter(r['pol'] for r in recs)
for k in sorted(c): print(f'政体 {k}\t{c[k]}')

# 案の粗い見積もり：R 3 通り（既存の写像だけ）。t = R*mil*社会主義 を共産主義へ移す（国の集計の水準で近似）。
print('\n# 案の粗い見積もり（国の平均の闘争度を全ての POP に当てた近似・闘争度と社会主義の重みの相関を落とすので下に偏る）')
print('# 案の共産主義＝nCom + R*mil*nSoc・引力点に要る共産主義のシェアの目安 0.34〜0.42（(671) の説明の数字）')
defs = {
    'R=1-組合の合法度': lambda r: 1 - UNION_LEGAL[r['union']],
    'R=(1-組合)*(1-報道)': lambda r: (1 - UNION_LEGAL[r['union']]) * (1 - PRESS[r['press']]),
    'R=1-max(組合,報道)': lambda r: 1 - max(UNION_LEGAL[r['union']], PRESS[r['press']]),
}
for name, f in defs.items():
    v = [r['nCom'] + f(r) * r['mil'] * r['nSoc'] for r in recs]
    th = [f(r) * r['mil'] for r in recs]
    over = sum(1 for x in v if x >= 0.34)
    nat = set((r['seed'], r['nation']) for r, x in zip(recs, v) if x >= 0.34)
    print(f'{name}\t案の共産主義 中央 {q(v,P)[0]:.4g}・90% {q(v,P)[1]:.4g}・99% {q(v,P)[2]:.4g}・最大 {q(v,P)[3]:.4g}\tθ 中央 {q(th,P)[0]:.3g}・99% {q(th,P)[2]:.3g}・最大 {q(th,P)[3]:.3g}\t0.34 以上の行 {over}（{over/len(recs):.2%}）・種×国 {len(nat)}')

# rev3 [R5] ③ の見積もり：絶対王政（政体 0）の行で、急進の全量（共和＋社会＋共産＋無政府の近似＝ここでは社会＋共産だけ）のうち共産主義へ移る割合の上限
print('\n# [R5] ③ 絶対王政（政体 0）の行：案の共産主義÷（社会主義＋共産主義）＝左の束のうち共産主義の割合（R=1-組合の合法度）')
f = defs['R=1-組合の合法度']
sub = [r for r in recs if r['pol'] == 0]
v = [(r['nCom'] + f(r) * r['mil'] * r['nSoc']) / max(1e-12, r['nSoc'] + r['nCom']) for r in sub]
v0 = [r['nCom'] / max(1e-12, r['nSoc'] + r['nCom']) for r in sub]
if sub:
    print(f'行 {len(sub)}\t旗 0：中央 {q(v0,P)[0]:.3g}・最大 {q(v0,P)[3]:.3g}\t案：中央 {q(v,P)[0]:.3g}・99% {q(v,P)[2]:.3g}・最大 {q(v,P)[3]:.3g}')

print('\n# 組合法ごとの大衆の闘争度の平均（国×四半期）')
for u in (0, 1, 2):
    sub = [r['mil'] for r in recs if r['union'] == u]
    if sub: print(f'組合 {u}\t行 {len(sub)}\t中央 {q(sub,P)[0]:.3g}・90% {q(sub,P)[1]:.3g}・99% {q(sub,P)[2]:.3g}・最大 {q(sub,P)[3]:.3g}\t0.3 以上 {sum(1 for x in sub if x>=0.3)/len(sub):.1%}')

print('\n# 組合が非合法で大衆の闘争度の平均が 0.3 以上の行：年代と国（上位）')
sub = [r for r in recs if r['union'] == 0 and r['mil'] >= 0.3]
c = Counter(r['year'] // 10 * 10 for r in sub)
print('年代\t' + '・'.join(f'{k}:{c[k]}' for k in sorted(c)))
c = Counter(r['nation'] for r in sub)
print('国\t' + '・'.join(f'{k}:{n}' for k, n in c.most_common(12)))
print(f'種×国 {len(set((r["seed"], r["nation"]) for r in sub))}')

# 思想の入力の mil は max(闘争度, 不満度)（Ideology.cs:58）＝POP ごとの最大の平均は平均どうしの最大以上＝下の見積もりはまだ下に偏る
print('\n# 思想の入力に近い量 max(闘争度の平均, 不満度の平均) で見直した案（R=1-組合の合法度）')
show('max(闘争度,不満度) の平均', 'milx')
v = [r['nCom'] + f(r) * r['milx'] * r['nSoc'] for r in recs]
print(f'案の共産主義 中央 {q(v,P)[0]:.4g}・90% {q(v,P)[1]:.4g}・99% {q(v,P)[2]:.4g}・最大 {q(v,P)[3]:.4g}・0.34 以上 {sum(1 for x in v if x>=0.34)}')
th = [f(r) * r['milx'] for r in recs]
print(f'θ≧0.25（都市の窮乏の POP で主軸が反転する目安）の行 {sum(1 for x in th if x>=0.25)}（{sum(1 for x in th if x>=0.25)/len(recs):.1%}）・種×国 {len(set((r["seed"], r["nation"]) for r, x in zip(recs, th) if x>=0.25))}')

print('\n# 組合が非合法で max(闘争度,不満度) の平均が 0.25 以上の行：報道法×国（R の出所の選び方の根拠）')
sub = [r for r in recs if r['union'] == 0 and r['milx'] >= 0.25]
c = Counter((r['press'], r['nation']) for r in sub)
for pr in (0, 1, 2):
    items = [(n, k) for (pp, n), k in c.items() if pp == pr]
    items.sort(key=lambda t: -t[1])
    print(f'報道 {pr}\t計 {sum(k for _, k in items)}\t' +'・'.join(f'{n}:{k}' for n, k in items[:10]))
