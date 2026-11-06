# (908) lawgate2_s の門（計器つき 10 種の生の上で）：python -X utf8 gate908.py <生のフォルダ> [種の数=10]
#   下側：nation_s の openFrontStates の L.*:6 の数 = lawgate2_s の engineCode 6 の行の数（種ごと・全体）／L.*:5 = engineCode 5／reasonCode 0 の行 = 0
#   上側：reasonCode 11・8 の行の数を、s19_ss6.py の近似（monlaw_s の官僚制度と社会保障の段と目標の段から導く）と並べる（近似の側を直して名乗る＝門を替えない）
#   理由の内訳の表を出す。
import sys, os, collections
d = sys.argv[1]; n = int(sys.argv[2]) if len(sys.argv) > 2 else 10
def rd(p):
    f = open(p, 'rb'); h = f.readline().decode().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    return ix, (l.decode().rstrip('\r\n').split('\t') for l in f)
tot = collections.Counter(); reasons = collections.Counter(); ssr = collections.Counter(); bad = []
for s in range(1, n + 1):
    ix, rows = rd(os.path.join(d, f'nation_s{s}.tsv'))
    f6 = f5 = 0
    for r in rows:
        for tok in r[ix['openFrontStates']].split(';'):
            if tok.startswith('L.') and tok.endswith(':6'): f6 += 1
            elif tok.startswith('L.') and tok.endswith(':5'): f5 += 1
    p = os.path.join(d, f'lawgate2_s{s}.tsv')
    e6 = e5 = z = 0
    if os.path.exists(p):
        jx, lr = rd(p)
        for r in lr:
            c = int(r[jx['engineCode']]); rc = int(r[jx['reasonCode']]); reasons[(c, rc)] += 1
            if r[jx['axis']] == 'SocialSecurity' and c == 6: ssr[rc] += 1
            if c == 6: e6 += 1
            if c == 5: e5 += 1
            if rc == 0: z += 1
    tot['f6'] += f6; tot['f5'] += f5; tot['e6'] += e6; tot['e5'] += e5; tot['zero'] += z
    if f6 != e6 or f5 != e5 or z: bad.append((s, f6, e6, f5, e5, z))
print('下側：戦線の符号6', tot['f6'], '行の engineCode6', tot['e6'], '／符号5', tot['f5'], '行の engineCode5', tot['e5'], '／reasonCode 0 の行', tot['zero'])
print('種ごとの食い違い（種・f6・e6・f5・e5・zero）：', bad)
print('内訳（engineCode, reasonCode）：', dict(sorted(reasons.items())))
print('社会保障の符号6の理由（上側の門＝s19_ss6.py の近似 官僚の前提=11・緊縮の上限=8 と並べる）：', dict(sorted(ssr.items())))
