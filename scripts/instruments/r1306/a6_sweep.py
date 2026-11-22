# (1306) 助走ごと×勢いの重みごとの鎖の差（重税−軽税・助走の後 20 四半期）
# 各量は試験と同じく〈軽税の世界の規模で重みを固定した階級ごとの平均の差〉（闘争度）・全体の平均の差（充足）・大衆の貯蓄の相対の差
# さらに闘争度の差を (州, 階級) の寄与に分け、上位を出す。使い方: a6_sweep.py <out>
import sys, csv, collections
out = sys.argv[1]
MASS = None
def load(p):
    with open(p, encoding='utf-8', newline='') as f: return list(csv.DictReader(f, delimiter='\t'))
def stats(rows, tax):
    cls = collections.defaultdict(lambda: collections.defaultdict(float)); rc = collections.defaultdict(lambda: collections.defaultdict(float))
    sat = [];
    byq = collections.defaultdict(lambda: [0.0, 0.0, 0.0])
    for r in rows:
        if r['tax'] != tax or int(r['q']) < 1 or r.get(None): continue
        sz = float(r['size']); c = int(r['class'])
        byq[r['q']][0] += sz; byq[r['q']][1] += sz * float(r['sat']); byq[r['q']][2] += sz * float(r['disc'])
        for k in ('mil', 'milFamine', 'milVoice', 'disc', 'livingWant', 'sat'):
            cls[c][k] += sz * float(r[k]); rc[(r['region'], c)][k] += sz * float(r[k])
        cls[c]['size'] += sz; rc[(r['region'], c)]['size'] += sz
        if c in (1, 2, 11, 12): cls[c]['sav'] += float(r['savings'])
    satAvg = sum(v[1] / v[0] for v in byq.values()) / len(byq)
    discAvg = sum(v[2] / v[0] for v in byq.values()) / len(byq)
    return cls, rc, satAvg, discAvg
def fixed(L, H, k):
    num = den = 0
    for c in L:
        if c in H and L[c]['size'] > 0 and H[c]['size'] > 0:
            num += L[c]['size'] * (H[c][k] / H[c]['size'] - L[c][k] / L[c]['size']); den += L[c]['size']
    return num / den if den else 0
print('tag        | dMil(固定) dFamine dVoice | dDisc(固定) dLiving | dSat(試験の形) dDisc(全体) | 大衆の貯蓄の相対差 | 闘争度の差の上位の寄与（州:階級 寄与）')
tot = collections.defaultdict(lambda: collections.defaultdict(float))
for iew in ('main', 'k0'):
    for wu in (20, 40, 60, 80, 100, 120):
        tag = f'{iew}_w{wu}'
        rows = load(f'{out}/pop_{tag}.tsv')
        Lc, Lr, Ls, Ld = stats(rows, '0'); Hc, Hr, Hs, Hd = stats(rows, '0.6')
        dm = fixed(Lc, Hc, 'mil'); df = fixed(Lc, Hc, 'milFamine'); dv = fixed(Lc, Hc, 'milVoice'); dd = fixed(Lc, Hc, 'disc'); dl = fixed(Lc, Hc, 'livingWant')
        ls = sum(Lc[c]['sav'] for c in Lc); hs = sum(Hc[c]['sav'] for c in Hc); dsav = (hs - ls) / max(1e-9, ls)
        den = sum(Lc[c]['size'] for c in Lc if c in Hc and Lc[c]['size'] > 0 and Hc[c]['size'] > 0)
        contrib = []
        for key in Lr:
            if key in Hr and Lr[key]['size'] > 0 and Hr[key]['size'] > 0:
                # 階級の平均の差を州に分ける近似：州の寄与＝(重の州の和/重の階級の規模 − 軽の州の和/軽の階級の規模)×軽の階級の規模/den
                c = key[1]
                contrib.append(((Hr[key]['mil'] / Hc[c]['size'] - Lr[key]['mil'] / Lc[c]['size']) * Lc[c]['size'] / den, key))
        contrib.sort(key=lambda x: -abs(x[0]))
        top = ' '.join(f'{k[0]}:{k[1]} {v:+.4f}' for v, k in contrib[:4])
        print(f'{tag:<10} | {dm:+.4f} {df:+.4f} {dv:+.4f} | {dd:+.4f} {dl:+.4f} | {Hs - Ls:+.4f} {Hd - Ld:+.4f} | {dsav:+.4f} | {top}')
        if wu in (20, 60, 100):
            for k, v in (('dMil', dm), ('dSat', Hs - Ls), ('dDisc', dd), ('dSav', dsav)): tot[iew][k] += v
for iew in tot: print(f'試験の 3 つの助走（20・60・100）の和 {iew}: ' + ' '.join(f'{k}={v:+.4f}' for k, v in tot[iew].items()))
