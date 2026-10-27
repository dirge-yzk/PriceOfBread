# (603) 線 pfix の読み手（読むだけ）：計器つきの生（money_s<種>.tsv・pfix_s<種>.tsv・pjump_s<種>.tsv・nation_s<種>.tsv）から、価格水準の発散の観測量を種ごとに出す。
#   使い方：python g_pfix.py <ins_dir> [<ins_dir> ...] [--report <成績表のファイル>]   （例：D:/PoB_work/keep_600/m600_ins）
#   出す量（事前登録 pfix603_20261028_prereg.txt §7 と追記 1・2 の G1〜G6）：
#     money_s（NATIONS の 9 国・四半期ごと）
#       rows        四半期の行数（time が q で始まる行）
#       bound_viol  G3＝priceLevelAfterIndex > max(max(Before, cyclicalRatio) × (1 + W×その行の月数), 床) の行数（W＝FiatInflationWeight 0.6・月の圧は各 1 以下の割合・月数は列 monthsInQuarterCount から読む）
#       jump100     G1（9 国の側）＝ln(After/Before) ≥ ln 100 の行数
#       nonfinite   P が有限でない行（NaN を含む＝NaN の行も数える）
#       g6_viol     G6＝gdpRealPerMonth ≤ 1e-9 の行のうち、趨勢が更新の前後で違う行（旗 PriceTrendHoldAtZeroGdp が働いていない）
#       broad_le0   broadMoneyStock ≤ 0 の行（G5 の記録）
#       gdp_le0     gdpRealPerMonth ≤ 1e-9 の行（旗が入口に入る行）
#       gdp_small   1e-9 < gdpRealPerMonth ≤ 1 の行（G5 の記録：旗の門が硬い段差なので、0 に近い正の GDP の行を別に数える）
#       maxdlnp / maxP   ln(After/Before) の最大・priceLevelAfterIndex の最大（国・四半期つき）
#     pjump_s（全ての国）  G1 の全国の側：行数（国・四半期の一覧を 5 行まで）
#     pfix_s（全ての国）   G4・追記 1：kind=1（圧の分母）／kind=2（実質 GDP の保持）の行数・最初の kind=1 の行（国・四半期）
#     残り（追記 1）  種×国ごとに ln P(最終) − ln P(最初の pfix_s の kind=1 の行の After) ≥ ln 1e10（＝23.03）の (種, 国)＝〈単一の指数関数の残りが開いている〉。最終の P は nation_s の最後の四半期の priceLevel（全ての国）
#   係数は環境変数で変えられる：PFIX_W=0.6 PFIX_FLOOR=0.4 は事前登録の既定（Coefficients.FiatInflationWeight・PriceLevelFloor）。
#   --report：成績表の〈貿易/GDP 平均〉の行を出し、有限で 1e3 % 以下かを判定する（G2）。
import csv, sys, math, os, glob, re
sys.stdout.reconfigure(encoding='utf-8')
W = float(os.environ.get('PFIX_W', '0.6')); FLOOR = float(os.environ.get('PFIX_FLOOR', '0.4'))
LN1E10 = math.log(1e10)
def f(x):
    try: return float(x)
    except: return float('nan')
def seed_of(p): return int(''.join(c for c in os.path.basename(p) if c.isdigit()) or 0)
def rows(path):
    return csv.DictReader(open(path, encoding='utf-8'), delimiter='\t')
def scan_money(path):
    o = dict(rows=0, bound_viol=0, jump100=0, nonfinite=0, g6_viol=0, broad_le0=0, gdp_le0=0, gdp_small=0, maxd=(-1e99, '', ''), maxp=(0.0, '', ''))
    for r in rows(path):
        if not r['time'].startswith('q'): continue
        pb, pa, cy = f(r['priceLevelBeforeIndex']), f(r['priceLevelAfterIndex']), f(r['cyclicalRatio'])
        o['rows'] += 1
        if not math.isfinite(pa):   # NaN も無限大もここに入る（isfinite は NaN で False）
            o['nonfinite'] += 1; continue
        mon = f(r['monthsInQuarterCount'])
        if not (mon > 0): mon = 3.0   # 月数の列が読めないときだけ 3（記録）
        cyv = cy if cy == cy else 0.0
        pbv = pb if pb == pb else 0.0
        if not (pa <= max(max(pbv, cyv) * (1 + W * mon), FLOOR) * (1 + 1e-9)): o['bound_viol'] += 1
        if pb > 0 and pa > 0:
            d = math.log(pa / pb)
            if d >= math.log(100): o['jump100'] += 1
            if d > o['maxd'][0]: o['maxd'] = (d, r['nation'], r['time'])
        if pa > o['maxp'][0]: o['maxp'] = (pa, r['nation'], r['time'])
        if f(r['broadMoneyStock']) <= 0: o['broad_le0'] += 1
        g = f(r['gdpRealPerMonth'])
        if g <= 1e-9:
            o['gdp_le0'] += 1
            tb, ta = f(r['moTrendBeforeMonths']), f(r['moTrendAfterMonths'])
            if not (tb == ta): o['g6_viol'] += 1   # NaN どうしも違う側へ倒す（保持できていない）
        elif g <= 1.0: o['gdp_small'] += 1
    return o
def pfix_firsts(d, seed):
    """pfix_s の kind=1 の行：国ごとの最初の (四半期, After)・kind=1 と kind=2 の行数・全体の最初の行"""
    p = os.path.join(d, 'pfix_s%d.tsv' % seed); firstby = {}; c1 = c2 = 0; best = None
    if os.path.exists(p):
        for r in rows(p):
            if r['kind'] == '2': c2 += 1; continue
            if r['kind'] != '1' or not r['time'].startswith('q'): continue
            c1 += 1; q = int(r['time'][1:]); n = r['nation']; a = f(r['priceLevelAfter'])
            if n not in firstby or q < firstby[n][0]: firstby[n] = (q, a)
            if best is None or q < best[0]: best = (q, n, a)
    return firstby, c1, c2, best
def pjump_list(d, seed):
    p = os.path.join(d, 'pjump_s%d.tsv' % seed); out = []
    if os.path.exists(p):
        for r in rows(p): out.append((r['nation'], r['time'], f(r['priceLevelBefore']), f(r['priceLevelAfter'])))
    return out
def finals(d, seed):
    """nation_s の最後の四半期の priceLevel（国名ごと）"""
    p = os.path.join(d, 'nation_s%d.tsv' % seed); last = {}
    if os.path.exists(p):
        for r in rows(p):
            q = int(r['quarter']); n = r['nation']
            if n not in last or q >= last[n][0]: last[n] = (q, f(r['priceLevel']))
    return last
args = sys.argv[1:]; report = None
if '--report' in args:
    i = args.index('--report'); report = args[i + 1]; del args[i:i + 2]
for d in args:
    print('##', d)
    tot = dict(rows=0, bound_viol=0, jump100=0, nonfinite=0, g6_viol=0, broad_le0=0, gdp_le0=0, gdp_small=0)
    tj = 0; t1 = t2 = 0; opened = []
    for p in sorted(glob.glob(os.path.join(d, 'money_s*.tsv')), key=seed_of):
        s = seed_of(p); o = scan_money(p)
        for k in tot: tot[k] += o[k]
        jl = pjump_list(d, s); firstby, c1, c2, best = pfix_firsts(d, s); tj += len(jl); t1 += c1; t2 += c2
        print(os.path.basename(p), 'rows', o['rows'], 'bound_viol', o['bound_viol'], 'jump100', o['jump100'], 'nonfinite', o['nonfinite'], 'g6_viol', o['g6_viol'],
              'maxdlnp %.3f(%s %s)' % o['maxd'], 'maxP %.4g(%s %s)' % o['maxp'], 'broad<=0', o['broad_le0'], 'gdp<=1e-9', o['gdp_le0'], 'gdp_small', o['gdp_small'])
        print('   pjump_s', len(jl), jl[:5], '| pfix_s kind1', c1, 'kind2', c2, '| first kind1', best)
        if firstby:
            fin = finals(d, s)
            for n, (q, pa) in sorted(firstby.items()):
                if n in fin and fin[n][1] > 0 and pa > 0:
                    gap = math.log(fin[n][1]) - math.log(pa)
                    if gap >= LN1E10: opened.append((s, n, q, round(gap, 2), fin[n][1]))
    print('TOTAL', tot, 'pjump_s_rows', tj, 'pfix_kind1', t1, 'pfix_kind2', t2)
    print('RESIDUAL_OPEN (種, 国, 最初の kind=1 の四半期, ln P(最終)-ln P(最初), P(最終)) ≥ ln1e10:', opened if opened else 'なし')
if report:
    txt = open(report, encoding='utf-8', errors='replace').read()
    m = re.search(r'貿易/GDP 平均\s*([0-9.eE+\-]+|nan|inf)\s*%', txt)
    v = f(m.group(1)) if m else float('nan')
    print('REPORT', report, '貿易/GDP 平均', m.group(1) if m else '(行なし)', '→', 'G2 OK' if (math.isfinite(v) and v <= 1e3) else 'G2 NG')
