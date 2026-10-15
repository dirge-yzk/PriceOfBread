"""消費の入口 段A の計器 ce_s の読み手（(345)・読むだけ）。
  python an_ce.py <計器の出力ディレクトリ> [-n 国名の部分文字列 ...] [--set mass|rich|all] [--goods 名,名,...] [-v]
  入力＝標準の計器（`INS=1 bash batch.sh run <腕>` の <出力>/ins）の ce_s<seed>.tsv と class_s<seed>.tsv。
  ce_s の形（SimCore/Instruments.cs の CeFold／CeTable・列の説明は CeColNames の注）：scope 列で C（国×四半期×階級の月の平均・挙げた国は毎四半期・他の国は年末の7階級）と
  N（国×窓〈40四半期〉×財の必需の割合の影）。設計 doc docs/202610/20261015/20261015_消費の入口_設計.md の §1（予算の3層）・§4（大衆化）の材料。
  出すもの（国ごと・窓ごと・種の中央値〔と最小〜最大〕）：階級の集合（既定＝大衆〔農民・工場労働者・職人〕と富裕層〔貴族・資本家・商人〕）の
    Paid÷Earned（class_s の定義＝建設なども含む全 Paid ÷ その月の Earned）／消費の支払い÷前の月の所得（ce_s の定義＝消費の財のループの中の支払いだけ ÷ Y）／
    食料の籠・日用品の籠〔供給後＝欲しい量×配給率×買い手の値段〕÷Y（供給前の欲しい量は参考で別に出す＝供給の無い主食の欲しい量×品薄の値段で何倍にも膨らむ）／余り X÷Y（Y−層1・2 の支払い）と余り÷Y（Y−籠）／
    取り崩し÷Y（全体・食料だけ・食料と日用品）／層2 で削られる額の影÷日用品の籠／籠を所得で賄える人口の割合／食料の籠が予算を超える人口の割合／貯蓄÷月の所得（月数）／
    層ごとの支払い÷供給後の籠／奢侈品の支払い÷Y／負の需要（残予算が負の POP の後の財）の額÷消費の支払い・残予算が負で終わる人口の割合
  N 行：--goods（既定は設計 doc §5 の財＋主食）の必需の割合の影 n の窓ごとの推移と、買った量÷籠の量。
  ⚠限界：①窓の四半期は 0 始まりの q を 1 始まりの Q に直して名乗る（Q5–Q12＝q4–q11）②非挙げ国は年末の四半期の行だけ（q%4==3）③class_s の Paid÷Earned は同じ四半期の行を使う
  ④Y は前の月の確定所得（資本家は負になりうる→派生量は max(0,Y)・Y÷の比は earnedLastPositive で割る）⑤n の影は書くだけで力学は読まない。"""
import sys, os, collections, statistics, math
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import seeds, num

args = sys.argv[1:]
subs = []
SET = 'both'
GOODS = None
V = '-v' in args
i = 0
dirs = []
while i < len(args):
    a = args[i]
    if a == '-n':
        subs.append(args[i + 1]); i += 2
    elif a == '--set':
        SET = args[i + 1]; i += 2
    elif a == '--goods':
        GOODS = args[i + 1].split(','); i += 2
    elif a.startswith('-'):
        i += 1
    else:
        dirs.append(a); i += 1
D = dirs[0]
sds = seeds(D, 'ce')
if not sds:
    print('ce_s が無い（INS=1 で回した出力の ins/ を渡す）'); sys.exit(1)

SETS = {'mass': ('大衆（農民・工場労働者・職人）', {1, 2, 11}), 'rich': ('富裕層（貴族・資本家・商人）', {0, 3, 10})}
if SET in ('mass', 'rich'): use = [SET]
else: use = ['mass', 'rich']
# 窓（q は 0 始まり・Q は 1 始まり）
WINS = [('Q5-Q12', 4, 11), ('Q13-Q40', 12, 39), ('Q5-Q40', 4, 39), ('Q41-Q160', 40, 159), ('Q161-Q400', 160, 399), ('Q401-Q668', 400, 667)]
GOODS_DEFAULT = ['パン', '米', '野菜', 'ジャガイモ', 'トウモロコシ', '砂糖', '茶', 'コーヒー', '缶詰', 'ビール', '煙草', '蒸留酒', '灯油', 'ガス', '石鹸', '衣料', '家具', '自転車', 'ラジオ', 'ワイン', '時計', '自動車', '冷蔵庫', '電化製品']
GOODS = GOODS or GOODS_DEFAULT


def fnum(x):
    if x == '' or x is None: return float('nan')
    return num(x)


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def fmt(xs, p=3):
    xs = [x for x in xs if x == x]
    if not xs: return '-'
    return f'{statistics.median(xs):.{p}f} [{min(xs):.{p}f}–{max(xs):.{p}f}]'


# ---- 読む
ce = {}        # (sd, nation) -> {q: {classId: row dict}}
nrows = collections.defaultdict(list)   # (nation) -> list of (sd, firstQ, goodName, tier, n, basket, bought, popK)
for sd in sds:
    with open(os.path.join(D, f'ce_s{sd}.tsv'), encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        ix = {h: j for j, h in enumerate(head)}
        for ln in fh:
            r = ln.rstrip('\n').split('\t')
            if r[ix['scope']] == 'C':
                nat = r[ix['nation']]; q = int(r[ix['quarter']]); c = int(r[ix['classId']])
                ce.setdefault((sd, nat), {}).setdefault(q, {})[c] = r
            else:
                nrows[r[ix['nation']]].append((sd, int(r[ix['quarter']]), r[ix['goodName']], r[ix['tier']], fnum(r[ix['necessityShadowShare']]), fnum(r[ix['basketQtyPerMonth']]), fnum(r[ix['boughtQtyPerMonth']]), fnum(r[ix['popK']])))
CE_IX = ix
cls_tab = {}   # (sd, nation) -> {q: {classId: (earned, paid)}}
for sd in seeds(D, 'class'):
    with open(os.path.join(D, f'class_s{sd}.tsv'), encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t'); jx = {h: j for j, h in enumerate(head)}
        for ln in fh:
            r = ln.rstrip('\n').split('\t')
            t = r[jx['time']]
            if not t.startswith('q'): continue
            e = fnum(r[jx['earnedPerMonth']]); p = fnum(r[jx['paidPerMonth']])
            if e != e: continue
            cls_tab.setdefault((sd, r[jx['nation']]), {}).setdefault(int(t[1:]), {})[int(r[jx['classId']])] = (e, p)


def col(r, name):
    return fnum(r[CE_IX[name]])


def ratio(a, b):
    return a / b if (b == b and b > 1e-12 and a == a) else float('nan')


METRICS = [
    ('Paid÷Earned（class_s・全 Paid）', None),
    ('消費の支払い÷Y（ce_s）', lambda s: ratio(s['consumptionPaidPerMonth'], s['earnedLastPositivePerMonth'])),
    ('食料の籠〔供給後〕÷Y', lambda s: ratio(s['foodSuppliedWantPerMonth'], s['earnedLastPositivePerMonth'])),
    ('日用品の籠〔供給後〕÷Y', lambda s: ratio(s['dailySuppliedWantPerMonth'], s['earnedLastPositivePerMonth'])),
    ('（食料+日用品）の籠〔供給後〕÷Y', lambda s: ratio(s['foodSuppliedWantPerMonth'] + s['dailySuppliedWantPerMonth'], s['earnedLastPositivePerMonth'])),
    ('食料の欲しい量〔供給前〕÷Y（参考）', lambda s: ratio(s['foodWantPerMonth'], s['earnedLastPositivePerMonth'])),
    ('日用品の欲しい量〔供給前〕÷Y（参考）', lambda s: ratio(s['dailyWantPerMonth'], s['earnedLastPositivePerMonth'])),
    ('供給後の食料の籠÷欲しい量', lambda s: ratio(s['foodSuppliedWantPerMonth'], s['foodWantPerMonth'])),
    ('供給後の日用品の籠÷欲しい量', lambda s: ratio(s['dailySuppliedWantPerMonth'], s['dailyWantPerMonth'])),
    ('余り X÷Y（Y−層1・2 の支払い）', lambda s: ratio(s['remainderPerMonth'], s['earnedLastPositivePerMonth'])),
    ('余り÷Y（Y−供給後の籠）', lambda s: ratio(s['remainderByBasketPerMonth'], s['earnedLastPositivePerMonth'])),
    ('取り崩し÷Y（消費の支払い−Y）', lambda s: ratio(s['savingsDrawPerMonth'], s['earnedLastPositivePerMonth'])),
    ('取り崩し÷Y（食料だけ）', lambda s: ratio(s['foodSavingsDrawPerMonth'], s['earnedLastPositivePerMonth'])),
    ('取り崩し÷Y（食料+日用品）', lambda s: ratio(s['basketSavingsDrawPerMonth'], s['earnedLastPositivePerMonth'])),
    ('層2 で削られる額の影÷供給後の日用品の籠', lambda s: ratio(s['shadowDailyCutPerMonth'], s['dailySuppliedWantPerMonth'])),
    ('層2 の予算の影÷Y', lambda s: ratio(s['shadowDailyBudgetPerMonth'], s['earnedLastPositivePerMonth'])),
    ('籠を所得で賄える人口の割合', lambda s: ratio(s['basketCoveredPopK'], s['popK'])),
    ('食料の籠が予算を超える人口の割合', lambda s: ratio(s['foodUnaffordablePopK'], s['popK'])),
    ('貯蓄÷月の所得（月数）', lambda s: ratio(s['savingsStartStock'], s['earnedLastPositivePerMonth'])),
    ('予算 S′（予算−Y）÷Y（月数）', lambda s: ratio(s['restStockStock'], s['earnedLastPositivePerMonth'])),
    ('食料の支払い÷供給後の食料の籠', lambda s: ratio(s['foodPaidPerMonth'], s['foodSuppliedWantPerMonth'])),
    ('日用品の支払い÷供給後の日用品の籠', lambda s: ratio(s['dailyPaidPerMonth'], s['dailySuppliedWantPerMonth'])),
    ('奢侈品の支払い÷供給後の奢侈品の欲しい量', lambda s: ratio(s['luxuryPaidPerMonth'], s['luxurySuppliedWantPerMonth'])),
    ('奢侈品の支払い÷Y', lambda s: ratio(s['luxuryPaidPerMonth'], s['earnedLastPositivePerMonth'])),
    ('役務その他の支払い÷Y', lambda s: ratio(s['otherPaidPerMonth'], s['earnedLastPositivePerMonth'])),
    ('負の需要の額÷消費の支払い', lambda s: ratio(-s['negativeDemandPerMonth'], s['consumptionPaidPerMonth'])),
    ('残予算が負で終わる人口の割合', lambda s: ratio(s['budgetEndNegativePopK'], s['popK'])),
]
COLS = ['popK', 'earnedLastPerMonth', 'earnedLastPositivePerMonth', 'savingsStartStock', 'budgetStartStock', 'foodWantPerMonth', 'dailyWantPerMonth', 'luxuryWantPerMonth',
        'foodSuppliedWantPerMonth', 'dailySuppliedWantPerMonth', 'luxurySuppliedWantPerMonth',
        'foodPaidPerMonth', 'dailyPaidPerMonth', 'luxuryPaidPerMonth', 'otherPaidPerMonth', 'consumptionPaidPerMonth', 'remainderPerMonth', 'remainderByBasketPerMonth', 'savingsDrawPerMonth',
        'foodSavingsDrawPerMonth', 'basketSavingsDrawPerMonth', 'shadowDailyCutPerMonth', 'shadowDailyBudgetPerMonth', 'basketCoveredPopK', 'foodUnaffordablePopK', 'restStockStock',
        'negativeDemandPerMonth', 'budgetEndNegativePopK']


def window_stats(sd, nat, classes, q0, q1):
    """窓の中の四半期ごとに階級の和を取り、列ごとに四半期の平均を返す。Paid÷Earned は class_s の和の比。"""
    qs = ce.get((sd, nat), {})
    acc = collections.defaultdict(float); n = 0
    for q, byc in qs.items():
        if q < q0 or q > q1: continue
        got = [byc[c] for c in classes if c in byc]
        if not got: continue
        n += 1
        for name in COLS: acc[name] += sum(col(r, name) for r in got)
    if n == 0: return None
    st = {k: v / n for k, v in acc.items()}
    ct = cls_tab.get((sd, nat), {}); e = p = 0.0; m = 0
    for q, byc in ct.items():
        if q < q0 or q > q1: continue
        g = [byc[c] for c in classes if c in byc]
        if not g: continue
        e += sum(x[0] for x in g); p += sum(x[1] for x in g); m += 1
    st['_pe'] = ratio(p, e) if m else float('nan')
    st['_n'] = n
    return st


nations = sorted({nat for (_, nat) in ce})
shown = 0
print(f'種 {len(sds)} 本（{sds[0]}–{sds[-1]}）  ce_s の国 {len(nations)}  中央値 [最小–最大]')
for nat in nations:
    if subs and not any(s in nat for s in subs): continue
    if not subs and not any(s == nat for s in ('イギリス', 'フランス', 'ネーデルラント', 'ポルトガル', '清')): continue
    for key in use:
        title, classes = SETS[key]
        res = {}
        for wn, q0, q1 in WINS:
            res[wn] = [window_stats(sd, nat, classes, q0, q1) for sd in sds]
        if all(all(x is None for x in v) for v in res.values()): continue
        shown += 1
        print(f'\n== {nat} {title} ==')
        print('指標\t' + '\t'.join(w[0] for w in WINS))
        for name, f in METRICS:
            row = []
            for wn, _, _ in WINS:
                vals = []
                for st in res[wn]:
                    if st is None: continue
                    vals.append(st['_pe'] if f is None else f(st))
                row.append(fmt(vals))
            print(name + '\t' + '\t'.join(row))
        if V:
            for name in ('popK', 'earnedLastPositivePerMonth', 'consumptionPaidPerMonth'):
                print(name + '\t' + '\t'.join(fmt([st[name] for st in res[w[0]] if st is not None], 1) for w in WINS))
        print('四半期の行の数（種の中央値）\t' + '\t'.join(f"{med([st['_n'] for st in res[w[0]] if st is not None]):.0f}" for w in WINS))

# ---- 必需の割合の影
print('\n\n== 必需の割合の影 n（国×窓〈40四半期〉×財・人口で重みづけた州×月の平均・種の中央値）。買った量÷籠＝その国の全人口の平均 ==')
for nat in nations:
    if subs and not any(s in nat for s in subs): continue
    if not subs and not any(s == nat for s in ('イギリス', 'フランス', 'ネーデルラント', 'ポルトガル', '清')): continue
    rows = nrows.get(nat, [])
    if not rows: continue
    wins = sorted({r[1] for r in rows})
    print(f'\n-- {nat}  窓の先頭の Q：' + ' '.join(f'Q{w + 1}' for w in wins))
    for g in GOODS:
        cells = []
        for w in wins:
            ns = [r[4] for r in rows if r[2] == g and r[1] == w]
            bs = [ratio(r[6], r[5]) for r in rows if r[2] == g and r[1] == w]
            cells.append(f'{med(ns):.2f}/{med(bs):.2f}' if ns and any(x == x for x in ns + bs) else '.')
        if any(c != '.' for c in cells):
            print(g + '\t' + '\t'.join(cells))
print('\n（表記 n/買った量÷籠。n は比例配給の財だけ・役務の優先配分の財は nan＝.）')
