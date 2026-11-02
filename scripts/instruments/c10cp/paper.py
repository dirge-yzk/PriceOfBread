# (791) c10cp 紙の台本：旗 0 の盤面（m784・r788 のプローブの表）に、事前登録の門を式で当てる。
#   使い方: python -I paper.py <プローブの ins フォルダ> [種の起点=1] [種の数=10]
#   入力＝r788_s<seed>.tsv（講和 1 回ごとの行・scripts/instruments/r788/patch_probe.py の計器）。エンジンの新しい実装は読まない（旗 0 の盤面そのもの）。
#   事象＝同じ月・同じ敗者で、敗者の側が崩壊と判定され（aCol／bCol）賠償の枝まで進んだ（hit）講和の集合。N＝その数。
#   式（事前登録 measurements/m791_20261103_prereg_cp.txt）：
#     unit_j = 0.2 × 敗者の GdpPerMonth × 支払い率 r（講和の時点）・sa_j = |クランプ後の戦勝点|・saMax = max sa_j
#     fsh_j = 勝者の押しの累計 ÷ 事象の和（勝者が A なら pushA・B なら pushB・和が 0 なら等分）
#     repOld = Σ unit_j sa_j（旧＝戦争ごとに満額）・repNew = Σ unit_j saMax fsh_j（1 回分）・repReal = 旗 0 の実際の Σ rep（会議の削りと条約港の倍率を含む）
#   旗 0 の盤面で門を数える＝旗 0 でも通る門は門にならない（落ちることを確かめる）。
import sys, os, csv, math, collections, statistics as st
sys.stdout.reconfigure(encoding='utf-8')
d = sys.argv[1]; s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
def F(x):
    try: return float(x)
    except: return float('nan')
UK = 'イギリス'
ev = []
for s in range(s0, s0 + n):
    p = os.path.join(d, f'r788_s{s}.tsv')
    rows = [r for r in csv.DictReader(open(p, encoding='utf-8'), delimiter='\t') if r['hit'] == '1']
    for r in rows:
        sc = F(r['score']); aw = sc > 0
        r['loser'] = r['B'] if aw else r['A']; r['winner'] = r['A'] if aw else r['B']
        r['lcol'] = (r['bCol'] if aw else r['aCol']) == '1'
        r['winPush'] = max(0.0, F(r['pushA'] if aw else r['pushB']))
        r['sa'] = min(1.0, abs(sc)); r['abs'] = abs(sc)
        r['unit'] = 0.2 * F(r['loserGdpPerMonth']) * F(r['loserR'])
        pre = 'b' if aw else 'a'
        r['homeLost'] = (int(r[pre + 'Core0']) + int(r[pre + 'Int0'])) - (int(r[pre + 'Core1']) + int(r[pre + 'Int1']))
        r['colLost'] = int(r[pre + 'Col0']) - int(r[pre + 'Col1'])
        r['seed'] = s
    g = collections.OrderedDict()
    for r in rows:
        g.setdefault((r['month'], r['loser']), []).append(r)
    for (m, lo), rs in g.items():
        col = [r for r in rs if r['lcol']]
        if not col: continue
        k = len(col)
        wsum = sum(r['winPush'] for r in col)
        saMax = max(r['sa'] for r in col)
        fsh = [(r['winPush'] / wsum if wsum > 0 else 1.0 / k) for r in col]
        repOld = sum(r['unit'] * r['sa'] for r in col)
        repNew = sum(r['unit'] * saMax * f for r, f in zip(col, fsh))
        repReal = sum(F(r['rep']) for r in col)
        if not all(math.isfinite(x) for x in (repOld, repNew, repReal)): continue   # 敗者か勝者が国を持たない（賠償の枝の外）
        budOld = sum(r['sa'] * 100 for r in col); budNew = saMax * 100
        elig = sum(1 for r in col if r['abs'] >= 1.2)
        cede = sum(1 for r in col if r['homeLost'] > 0)
        ev.append(dict(seed=s, loser=lo, year=int(col[0]['year']), k=k, other=len(rs) - k, saMax=saMax, repOld=repOld, repNew=repNew, repReal=repReal,
                       budOld=budOld, budNew=budNew, elig=elig, cede=cede, u3=sum(1 for r, f in zip(col, fsh) if r['winPush'] > 0 and not f > 0),
                       roots=len(set(r['rootIdx'] for r in col))))
E = [e for e in ev if e['k'] >= 2]
print(f'# {d} 種 {s0}〜{s0 + n - 1}・崩壊の事象 {len(ev)}（N=1: {sum(1 for e in ev if e["k"] == 1)}・N>=2: {len(E)}）・N の分布 {sorted(collections.Counter(e["k"] for e in ev).items())}')
print(f'崩壊の事象と同じ月・同じ敗者の崩壊でない講和（押しで決まる）の混在：{sum(1 for e in ev if e["other"] > 0)} 事象（旧の盤面。新の集合は崩壊だけ＝U2 の対象）')
rr = [e['repReal'] / e['repNew'] for e in E if e['repNew'] > 0]
print(f'[賠償の元] 旗0の実際 ÷ 1回分の式（N>=2・{len(rr)} 事象）：中央 {st.median(rr):.3f}・最大 {max(rr):.3f}・>1.5 の事象 {sum(x > 1.5 for x in rr)}（{sum(x > 1.5 for x in rr) / len(rr):.3f}）・<0.999 の事象 {sum(x < 0.999 for x in rr)}')
print(f'  合計比 Σ実際 ÷ Σ1回分 = {sum(e["repReal"] for e in E) / sum(e["repNew"] for e in E):.3f}（旗 0＝N 倍に近い）')
ro = [e['repNew'] / e['repOld'] for e in E if e['repOld'] > 0]
print(f'[働いた門 L1] 1回分の式 ÷ 旧の式（Σ unit×sa）：事象の中央 {st.median(ro):.3f}・合計比 {sum(e["repNew"] for e in E) / sum(e["repOld"] for e in E):.3f}（旗 1 の合計が旧の式の何割か＝旗 0 は 1.0）')
bb = [e['budOld'] / e['budNew'] for e in E]
print(f'[植民地の予算] 旧 Σ sa×100 ÷ 1回分 saMax×100（N>=2）：中央 {st.median(bb):.3f}・最大 {max(bb):.3f}・>1.5 の事象 {sum(x > 1.5 for x in bb)}（{sum(x > 1.5 for x in bb) / len(bb):.3f}）')
E_c = [e for e in E if e['elig'] >= 1]
print(f'[本国の割譲] N>=2 かつ門を通る講和が 1 本以上の事象 {len(E_c)}：割譲が 2 回以上の事象 {sum(e["cede"] >= 2 for e in E_c)}・1 回以上の事象 {sum(e["cede"] >= 1 for e in E_c)}（p0＝{sum(e["cede"] >= 1 for e in E_c) / max(1, len(E_c)):.3f}）・割譲の旧の合計 {sum(e["cede"] for e in E_c)} に対し 1 回分の上限 {len(E_c)}')
print(f'[形の誤り] U3（押しが正で取り分 0 の勝者）= {sum(e["u3"] for e in ev)}')
print(f'連鎖の根が 1 つだけの事象 {sum(e["roots"] == 1 for e in E)}／根が 2 つ以上 {sum(e["roots"] >= 2 for e in E)}（N>=2・参考）')
# 英の 1784〜1815
U = [e for e in ev if e['loser'] == UK and 1784 <= e['year'] <= 1815]
print(f'[英 1784〜1815] 英が敗者の崩壊の事象 {len(U)}（種の中央 {st.median([sum(1 for e in U if e["seed"] == s) for s in range(s0, s0 + n)]):.1f}）・Σ賠償の旧の式 {sum(e["repOld"] for e in U):.0f} → 1 回分 {sum(e["repNew"] for e in U):.0f}（{sum(e["repNew"] for e in U) / max(1e-9, sum(e["repOld"] for e in U)):.3f}）・Σ講和の数 {sum(e["k"] for e in U)}')
tot = sum(e['repOld'] for e in ev); new = sum(e['repNew'] for e in ev)
print(f'[全体] Σ賠償の旧の式 {tot:.0f} → 1回分 {new:.0f}（{new / tot:.3f}）')
