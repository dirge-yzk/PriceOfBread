# (924) 門の素案の観測量：立憲の四半期の〈上げる項の和÷|②′ の和|〉と、PC の一歩の中央（政変・下賜・再ラベルの印の行は除く）
# 旧召集の仮の値（旗 0 の式 0.02×(緊縮−0.5)×2・緊縮>0.5・今の軌道の上の一歩）も並べる＝旗 0 で門が落ちる確かめの紙。
# 使い方: python -X utf8 -I q7_drift.py <out>
import csv, sys, math, os
csv.field_size_limit(10**8)
ROOT = 'D:/PoB_work/keep_919'
ARMS = [('base', 'base_ins', range(1, 11)), ('n1', 'n1', range(1, 11)), ('n2', 'n2', range(1, 11)), ('a11', 'a11', range(11, 21))]
NATS = ['イギリス', '清', 'ロシア', 'プロイセン', '日本', 'フランス', 'オーストリア', 'オスマン']
def f(x):
    try: return float(x)
    except Exception: return math.nan
def nz(v): return 0.0 if math.isnan(v) else v
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
out.write('arm\tnation\t立憲の行(印なし)\t召集Σ\t①Σ\t②′Σ\t(召集+①)÷|②′|\t旧召集の仮Σ\t(旧召集+①)÷|②′|\tPC の一歩の中央\tPC の一歩>0 の割合\n')
for arm, sub, seeds in ARMS:
    acc = {}
    for s in seeds:
        with open(os.path.join(ROOT, sub, f'crownpc_s{s}.tsv'), encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                n = r['nation']
                if n not in NATS or r['polityCode'] != '3' or r['polityPrevCode'] != '3': continue
                if r['regimeEventFlag'] == '1' or r['grantFlag'] == '1' or r['relabelFlag'] == '1' or r['firstRowFlag'] == '1': continue
                a = acc.setdefault(n, dict(k=0, sm=0.0, t1=0.0, t2p=0.0, old=0.0, d=[]))
                a['k'] += 1; a['sm'] += nz(f(r['summonTermShare'])); a['t1'] += nz(f(r['term1Share'])); a['t2p'] += nz(f(r['term2pShare']))
                A = f(r['austerityIndex'])
                if A > 0.5: a['old'] += 0.02 * (A - 0.5) * 2.0
                d = f(r['pcNowShare']) - f(r['pcPrevShare'])
                if not math.isnan(d): a['d'].append(d)
    for n in NATS:
        a = acc.get(n)
        if not a: continue
        dd = sorted(a['d']); med = dd[len(dd) // 2] if dd else math.nan
        pos = sum(1 for x in dd if x > 1e-12) / max(1, len(dd))
        den = abs(a['t2p']) if a['t2p'] != 0 else math.nan
        out.write(f"{arm}\t{n}\t{a['k']}\t{a['sm']:.3f}\t{a['t1']:.3f}\t{a['t2p']:.3f}\t{(a['sm'] + a['t1']) / den:.3f}\t{a['old']:.2f}\t{(a['old'] + a['t1']) / den:.2f}\t{med:.2e}\t{pos:.3f}\n")
out.close()
