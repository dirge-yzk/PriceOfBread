# (1266) c129dh 案 B の紙：自家の食べる量に今の主食の値段の係数 π＝clamp((M/r)^0.6, 0.4, 1.4) を掛けたら開幕に売りが立つか
# 近似：国の平均の値段（pxgoods_s の助走の最後の行 w19・priceMean÷refPriceIndex）・需要の門（demandGate）は無視・重み＝主食の BaseNeed
# 使い方: python -X utf8 -I pi1266.py <base_ins> <出力 txt>
import sys, os, math, statistics as st
base, out = sys.argv[1], sys.argv[2]
NEED = {'パン': 0.55, '米': 0.20, '野菜': 0.25, 'ジャガイモ': 0.06, 'トウモロコシ': 0.04}
CE, LO, HI, SIG = 0.6, 0.4, 1.4, 0.7
res = []
for seed in range(1, 11):
    rows = {}
    with open(os.path.join(base, f'pxgoods_s{seed}.tsv'), 'r', encoding='utf-8', newline='') as f:
        hdr = f.readline().rstrip('\r\n').split('\t'); ix = {h: i for i, h in enumerate(hdr)}
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            if c[ix['time']] != 'w19' or c[ix['goodName']] not in NEED: continue
            try:
                ref, pm = float(c[ix['refPriceIndex']]), float(c[ix['priceMean']])
            except ValueError:
                continue
            if ref > 0 and pm > 0 and math.isfinite(pm): rows.setdefault(c[ix['nation']], {})[c[ix['goodName']]] = pm / ref
    for n, r in rows.items():
        if len(r) < 2: continue
        M = math.exp(sum(math.log(v) for v in r.values()) / len(r))
        tot = sum(NEED[g] for g in r); own = SIG * tot
        sell = sum(NEED[g] * max(0.0, SIG - min(HI, max(LO, (M / v) ** CE))) for g, v in r.items())
        eat = own - sell
        res.append((seed, n, sell / own, eat / tot, r.get('パン', float('nan')) / M))
by = {}
for s, n, sh, e, b in res: by.setdefault(n, []).append((sh, e, b))
L = ['# 案 B の紙（開幕＝助走の最後 w19・国の平均の値段・種 1〜10 の中央）: 国 売る割合(自家の産出のうち) 食べる割合(必要量のうち) パンの比÷平均']
sells = []
for n in sorted(by):
    sh = st.median(x[0] for x in by[n]); e = st.median(x[1] for x in by[n]); b = st.median(x[2] for x in by[n]); sells.append((sh, e))
    L.append(f'{n}\t{sh:.4f}\t{e:.4f}\t{b:.3f}')
L.insert(1, f'国 {len(sells)}・売る割合の中央 {st.median(x[0] for x in sells):.4f}・食べる割合の中央 {st.median(x[1] for x in sells):.4f}・食べる割合が 0.68 未満の国 {sum(1 for x in sells if x[1] < 0.68)}')
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
print('\n'.join(L[:3]))
