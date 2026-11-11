# (965) 読むだけ：輸出の受け取り（cearn の inExportPerQuarter＝'B'・'M'・'F' の経路＝InstrumentsCearn.cs:93）の中身を、
#   cearn の inTop1Route（州の流入の最大の経路・ファイル:行:種類:相手）で分ける。輸出の受け取りの最大の経路が分かるのは
#   inTop1 が輸出の経路のときだけ＝下限の数（計器の窓の限界）。
#   使い方: python -X utf8 -I read965d.py <ins フォルダ> <種の起点> <名札> <出力フォルダ>
import sys, os, collections as C
M, SB, TAG, OUT = sys.argv[1], int(sys.argv[2]), sys.argv[3], sys.argv[4]
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def kind(route):
    if route.startswith('Simulation.cs:2976'): return 'mint'          # 造幣局が州の在庫の金銀を買う（PaySales・minted）
    if route.startswith('Simulation.cs:2978'): return 'mintImp'       # 同・在庫のうち輸入の分（捌き手へ）
    if route.startswith('BilateralMarket.cs'): return 'trade'          # 2 国間の清算（売上・仲介料・運賃・港湾料・保険料）
    return 'notExp'
close = C.defaultdict(set); lastexp = C.defaultdict(set)
for l in open(os.path.join(OUT, f'{TAG}_close.tsv'), encoding='utf-8').read().split('\n')[1:]:
    if not l: continue
    r = l.split('\t'); close[int(r[0])].add((r[1], int(r[2]) - 1))
for l in open(os.path.join(OUT, f'{TAG}_dead.tsv'), encoding='utf-8').read().split('\n')[1:]:
    if not l: continue
    r = l.split('\t')
    if int(r[5]) > 0: lastexp[int(r[0])].add((r[1], int(r[2]) - int(r[5])))
S = C.Counter(); amt = C.Counter()
for s in range(SB, SB + 10):
    p = os.path.join(M, f'cearn_s{s}.tsv')
    if not os.path.exists(p): continue
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    iq, ire, it, iti, ie, ia, ip = ix['quarter'], ix['region'], ix['tier'], ix['inTop1Route'], ix['inExportPerQuarter'], ix['inTop1PerQuarter'], ix['popK']
    for l in f:
        r = l.rstrip('\n').split('\t')
        if len(r) < len(h): continue
        try: q = int(r[iq])
        except ValueError: continue
        e = fl(r[ie])
        if not (e > 0): continue
        k = kind(r[iti]); top = fl(r[ia])
        k2 = k if (k != 'notExp' and top >= 0.5 * e) else 'unknown'   # 最大の経路が輸出の受け取りの半分以上を持つときだけ帰属
        key = (r[ire], q)
        if r[it] == 'Colonial' and fl(r[ip]) >= 100:
            S['col_' + k2] += 1; amt['col_' + k2] += e
        if key in close[s]: S['close_' + k2] += 1
        if key in lastexp[s]: S['last_' + k2] += 1
o = open(os.path.join(OUT, f'{TAG}_route.txt'), 'w', encoding='utf-8')
for pre, nm in [('col_', '植民地格・人口 100 以上・輸出＞0 の州×四半期'), ('close_', '輸出が閉じる前の四半期'), ('last_', '0 の続きの州の最後の輸出の四半期')]:
    d = {k[len(pre):]: v for k, v in S.items() if k.startswith(pre)}; n = sum(d.values())
    print(f'{TAG}\t{nm}\t計 {n}\t' + '\t'.join(f'{k} {v} ({v / max(1, n):.3f})' for k, v in sorted(d.items())), file=o)
    if pre == 'col_':
        ta = sum(v for k, v in amt.items() if k.startswith('col_'))
        print(f'{TAG}\t同・額の割合\t' + '\t'.join(f'{k[4:]} {v / ta:.3f}' for k, v in sorted(amt.items())), file=o)
o.close(); print(open(os.path.join(OUT, f'{TAG}_route.txt'), encoding='utf-8').read())
