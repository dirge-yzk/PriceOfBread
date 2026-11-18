# (1222) c123k3 段0 の紙の影の集計：プローブ（D:/PoB_work/c123k3_work/probe・旗 0 の世界・四半期の終わりの盤面で医療の式を旧と新で組み直す）の k3_s<種>.tsv を読む。
#   使い方: python -X utf8 -I shadow_k3.py <k3_s*.tsv ...>
#   出力：識字の項が min を決める国×四半期の割合（件数・人口の重み）・医療の増え（新−旧）・扶養力の増えの割合（1.2×Δ医療÷括弧＝同じ盤面の新÷旧−1）を年の帯と主な国で。
import sys, csv, statistics as st
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '清', '日本', 'マラータ同盟', 'オスマン']
rows = []
for p in sys.argv[1:]:
    with open(p, encoding='utf-8') as f:
        r = csv.DictReader((l for l in f if not l.startswith('#')), delimiter='\t')
        for d in r: rows.append(d)
def fl(x):
    try: return float(x)
    except: return float('nan')
n = len(rows); nb = sum(1 for d in rows if d['litBinds'] == '1')
pw = sum(fl(d['popK']) for d in rows); pb = sum(fl(d['popK']) for d in rows if d['litBinds'] == '1')
print(f'行（国×四半期×種）{n}・識字の項が min を決めた行 {nb}（{nb/n:.3f}）・人口の重み {pb/pw:.3f}')
dev = [abs(fl(d['hOld']) - fl(d['diagH'])) for d in rows if fl(d['diagH']) == fl(d['diagH'])]
print(f'組み直した旧の医療と人口の段が書いた値の差：中央 {st.median(dev):.3g}・最大 {max(dev):.3g}（四半期の終わりの盤面と月の段の盤面のずれ）')
for lo, hi in [(1783, 1830), (1830, 1870), (1870, 1914), (1914, 1951)]:
    sub = [d for d in rows if lo <= int(d['year']) < hi]
    if not sub: continue
    b = [d for d in sub if d['litBinds'] == '1']
    w = sum(fl(d['popK']) for d in sub); wb = sum(fl(d['popK']) for d in b)
    dc = [fl(d['dCapShare']) for d in b if fl(d['dCapShare']) == fl(d['dCapShare'])]
    dh = [fl(d['hNew']) - fl(d['hOld']) for d in b]
    print(f'{lo}〜{hi-1}：決めた行 {len(b)}/{len(sub)}（人口 {wb/w:.3f}）・Δ医療 中央 {st.median(dh) if dh else 0:.3f}・扶養力の増え 中央 {st.median(dc) if dc else 0:.3f}・最大 {max(dc) if dc else 0:.3f}')
print('国（種を通した 1850 年・1913 年の第 4 四半期）：決めたか・識字・社会保障×支払い率・医者の役務・旧→新の医療・扶養力の増え')
for nm in MAJ:
    for y in ('1850', '1913'):
        sub = [d for d in rows if d['nation'] == nm and d['year'] == y]
        if not sub: continue
        last = {}
        for d in sub: last[d['seed']] = d
        L = list(last.values())
        nbx = sum(1 for d in L if d['litBinds'] == '1')
        m = lambda k: st.median(fl(d[k]) for d in L)
        print(f'  {nm} {y}：{nbx}/{len(L)} 種・識字 {m("lit"):.2f}・SS×r {st.median(fl(d["ss"])*fl(d["fr"]) for d in L):.2f}・医者 {m("doctor"):.3f}・医療 {m("hOld"):.3f}→{m("hNew"):.3f}・扶養力 +{m("dCapShare"):.3f}')
# 国ごとの通算：識字の項が決めた四半期の割合（種の中央）
print('国ごとの通算（識字の項が決めた四半期の割合・種の中央・上位 12）')
agg = {}
for d in rows: agg.setdefault((d['nation'], d['seed']), [0, 0]); agg[(d['nation'], d['seed'])][0] += 1; agg[(d['nation'], d['seed'])][1] += d['litBinds'] == '1'
by = {}
for (nm, s), (a, b) in agg.items(): by.setdefault(nm, []).append(b / a)
top = sorted(((st.median(v), nm) for nm, v in by.items()), reverse=True)[:12]
print('  ' + '・'.join(f'{nm} {v:.2f}' for v, nm in top))
