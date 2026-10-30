"""(681) prdf_s の読み手（読むだけ）。python an_prdf.py <計器の出力フォルダ>
事前登録 measurements/prdf_681_20261030_prereg.txt [4]：分母＝knownCom=1 の国×四半期・N＝nBodyCom>=1 の行・K＝N の行のうち revoltTarget=6。"""
import sys, glob, os, statistics as st
d = sys.argv[1]
files = sorted(glob.glob(os.path.join(d, 'prdf_s*.tsv')))
rows = []
for f in files:
    seed = int(os.path.basename(f)[len('prdf_s'):-4])
    with open(f, encoding='utf-8') as fh:
        hdr = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            v = ln.rstrip('\n').split('\t')
            assert len(v) == len(hdr), (f, len(v), len(hdr))   # 列の幅の検査（ヘッダーと行の列がずれたら止める）
            r = dict(zip(hdr, v)); r['seed'] = seed
            for k in hdr:
                if k not in ('nation',):
                    try: r[k] = float(r[k])
                    except: pass
            rows.append(r)
print(f'ファイル {len(files)}・行 {len(rows)}・種 {sorted(set(r["seed"] for r in rows))}')
def pct(a, q):
    a = sorted(a)
    return a[min(len(a) - 1, int(q * len(a)))] if a else float('nan')
kc = [r for r in rows if r['knownCom'] == 1]
print(f'分母 D（knownCom=1 の行）{len(kc)}（{len(kc)/max(1,len(rows)):.3%}）・共産主義を発見した（種×国）{len(set((r["seed"], r["nation"]) for r in kc))}')
N = [r for r in kc if r['nBodyCom'] >= 1]
K = [r for r in N if r['revoltTarget'] == 6]
print(f'N（発見した行のうち nBodyCom>=1）{len(N)}（D 比 {len(N)/max(1,len(kc)):.4%}）・K（N のうち revoltTarget=6）{len(K)}')
print(f'発見していない行で nBodyCom>=1: {sum(1 for r in rows if r["knownCom"]==0 and r["nBodyCom"]>=1)}')
print(f'全行で revoltTarget=6 {sum(1 for r in rows if r["revoltTarget"]==6)}・polityCode=6 の行 {sum(1 for r in rows if r["polityCode"]==6)}')
from collections import Counter
rt = Counter(int(r['revoltTarget']) for r in rows if r['revoltTarget'] >= 0)
print('蜂起の目標（政体コード→回）', dict(sorted(rt.items())))
print('polityCode の行数', dict(sorted(Counter(int(r['polityCode']) for r in rows).items())))
for col in ('popShareCom', 'popShareComSub', 'popShareSoc', 'maxCellFracCom', 'maxCellFracSoc', 'maxCohCom', 'nCellCom', 'nBodyCom', 'nBodySoc', 'nBody'):
    a = [r[col] for r in kc]
    allv = [r[col] for r in rows]
    if a: print(f'{col:16s} 発見した行: 最大 {max(a):.5g}・p99 {pct(a,.99):.5g}・p90 {pct(a,.9):.5g}・中央 {pct(a,.5):.5g} ／ 全行 最大 {max(allv):.5g}')
print(f'(a′) N/D = {len(N)/max(1,len(kc)):.4%}（見込み 5% 未満）・(b′) popShareCom の最大 {max([r["popShareCom"] for r in kc] or [0]):.5f}（見込み 0.05 未満）・中央 {pct([r["popShareCom"] for r in kc],.5):.6f}（見込み 0.001 未満）')
print(f'manifestoCom=1 の行 {sum(1 for r in rows if r["manifestoCom"]==1)}（発見した行のうち {sum(1 for r in kc if r["manifestoCom"]==1)}）')
for i in range(4):
    a = [r[f'track{i}'] for r in rows]
    print(f'track{i}: 最小 {min(a):.4f}・最大 {max(a):.4f}・中央 {pct(a,.5):.4f}')
gap = [(r['track3'] - max(r['track0'], r['track1'])) for r in rows]
print(f'急進の系統3 − 穏健(0,1)の高い方: 常に正 {all(g>0 for g in gap)}・正の行 {sum(1 for g in gap if g>0)}/{len(gap)}・最大 {max(gap):.4f}')
# 国ごと・種ごとの N
bn = Counter((r['nation']) for r in N)
print('N の国別（上位10）', bn.most_common(10))
bs = Counter(r['seed'] for r in N)
print('N の種別', dict(sorted(bs.items())))
print('N の最初の年（種×国）の中央', st.median([min(x['year'] for x in N if x['seed']==s and x['nation']==n) for (s,n) in set((x['seed'],x['nation']) for x in N)]) if N else 'なし')
# 社会主義との比
S = [r for r in rows if r['nBodySoc'] >= 1]
print(f'参考：nBodySoc>=1 の行 {len(S)}（全行比 {len(S)/max(1,len(rows)):.3%}）')
# 共産主義セルが 1% に届く直前の大きさ：maxCellFracCom が 0.005 以上の行
for t in (0.002, 0.005, 0.01):
    print(f'  maxCellFracCom >= {t}: 発見した行 {sum(1 for r in kc if r["maxCellFracCom"]>=t)}')
# 判定
if len(N) == 0: br = 'N=0：束が一度も立たない→絞りは主軸の判定そのもの（段E・式）'
elif len(K) == 0: br = 'N>=1 かつ K=0：束は立つが蜂起の発火で止まる→蜂起の門を読む線'
elif sum(1 for r in rows if r['polityCode'] == 6) == 0: br = 'N>=1 かつ K>=1 かつ polityCode 6 が 0：蜂起は起きるが内戦の決着で止まる→決着を読む線'
else: br = 'N>=1・K>=1・polityCode 6 がある（規則の外＝作者に聞く）'
print('分岐：', br)
