# -*- coding: utf-8 -*-
"""(267) 貯蓄の在庫に比例する流れの棚卸し：flow_s（貯蓄の書き手の台帳）から経路ごとの 年額÷年所得・年額÷年GDP を窓ごとに読む（読むだけ）。

入力＝計器つき10種の出力フォルダ（INS=1 batch.sh run が出す <出力>/ins・POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント）。
  class_s（階級の貯蓄の在庫・Earned）・flow_s（貯蓄の書き手の台帳＝階級×書き手の種類×呼び出しの位置×相手の側の月平均の額）・nation_s（nominalGdpPerYear）。
出力＝TSV（標準出力）：国・窓・鍵・中央・最小・最大・種の数。種は全て中央値（と最小・最大）。

分母（2通り）：
  ・年所得＝全階級の Earned（階級の月平均×3か月）の窓の和。(262) の読み手と同じ。⚠限界：MonthClose の後に書かれる所得は Earned に載らない（(264) 未確認）。
  ・年GDP＝nation_s の nominalGdpPerYear の窓の平均×窓の年数。作者の答え（貯蓄は貨幣・目標は貨幣÷年GDP）と比べるときの分母。
窓は四半期の添字（Q1＝q0）。貯蓄の在庫は窓の各四半期末の平均。
投資家＝近代エリート（兵士を除く）＝資本家・技師・教師・官僚・医者・研究者・商人（Simulation.cs の MkPop(…, modern: true) の7階級）。
"""
import csv, collections, statistics, sys, os
sys.stdout.reconfigure(newline='\n')   # Windows の文字モードは LF を CRLF に変える＝LF のまま出す

D = sys.argv[1]
NATIONS = ['イギリス', 'フランス', 'ネーデルラント']
WINS = [('Q5〜Q40', 4, 39), ('Q41〜Q80', 40, 79), ('Q81〜Q160', 80, 159), ('Q161〜Q240', 160, 239), ('Q241〜Q400', 240, 399), ('Q401〜Q667', 400, 667)]
INV = {'技師', '教師', '官僚', '医者', '商人', '資本家', '研究者'}
MONTHS = 3
ROUTES_FILE = sys.argv[2] if len(sys.argv) > 2 else None   # 読む経路の一覧（TSV：鍵・kind・file・line）。省略時は下の既定


def rd(p):
    with open(p, encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            yield r


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


SEEDS = sorted(int(f[7:-4]) for f in os.listdir(D) if f.startswith('class_s') and f.endswith('.tsv'))
CLS = {}; FLOW = {}; GDP = {}
for sd in SEEDS:
    d = collections.defaultdict(lambda: collections.defaultdict(dict))
    for r in rd(os.path.join(D, f'class_s{sd}.tsv')):
        if r['nation'] in NATIONS: d[r['nation']][r['className']][r['time']] = (fl(r['savingsStock']), fl(r['earnedPerMonth']))
    CLS[sd] = d
    f = collections.defaultdict(lambda: collections.defaultdict(float))
    for r in rd(os.path.join(D, f'flow_s{sd}.tsv')):
        if r['nation'] not in NATIONS: continue
        f[r['nation']][(int(r['quarter']), r['kind'], r['sourceFile'], int(r['sourceLine']), r['counterpartySide'])] += fl(r['amountPerMonth'])
    FLOW[sd] = f
    g = collections.defaultdict(dict)
    for r in rd(os.path.join(D, f'nation_s{sd}.tsv')):
        if r['nation'] in NATIONS: g[r['nation']][int(r['quarter'])] = fl(r['nominalGdpPerYear'])
    GDP[sd] = g


def Y(sd, n, q0, q1):
    s = 0.0
    for c, t in CLS[sd][n].items():
        for q in range(q0, q1 + 1):
            e = t.get(f'q{q}')
            if e is None or e[1] != e[1]: return None
            s += e[1] * MONTHS
    return s


def G(sd, n, q0, q1):   # 窓の年名目GDPの和（四半期の年率÷4 の和）
    s = 0.0
    for q in range(q0, q1 + 1):
        v = GDP[sd][n].get(q)
        if v is None or v != v: return None
        s += v / 4.0
    return s


def sav(sd, n, q0, q1, only_inv):
    v = []
    for q in range(q0, q1 + 1):
        s = 0.0
        for c, t in CLS[sd][n].items():
            if only_inv and c not in INV: continue
            e = t.get(f'q{q}')
            if e is None or e[0] != e[0]: return None
            s += max(0.0, e[0])
        v.append(s)
    return sum(v) / len(v)


def routesum(sd, n, q0, q1, kind, file, line, cp=None):
    return sum(a * MONTHS for (q, k, f, l, c), a in FLOW[sd][n].items() if q0 <= q <= q1 and k == kind and f == file and l == line and (cp is None or c == cp))


DEFAULT_ROUTES = []
if ROUTES_FILE:
    for r in rd(ROUTES_FILE): DEFAULT_ROUTES.append((r['key'], r['kind'], r['file'], int(r['line']), r.get('cp') or None))

out = ['nation\twindow\tkey\tmedian_over_income\tmin\tmax\tmedian_over_gdp\tseeds']


def emit(n, wn, key, xs, gs=None):
    xs = [x for x in xs if x == x]
    if not xs: return
    gm = ''
    if gs:
        gs = [x for x in gs if x == x]
        if gs: gm = f'{statistics.median(gs):.6g}'
    out.append('\t'.join([n, wn, key, f'{statistics.median(xs):.6g}', f'{min(xs):.6g}', f'{max(xs):.6g}', gm, str(len(xs))]))


for n in NATIONS:
    for wn, q0, q1 in WINS:
        ys = {sd: Y(sd, n, q0, q1) for sd in SEEDS}
        gs_ = {sd: G(sd, n, q0, q1) for sd in SEEDS}
        for key, kind, f, l, cp in DEFAULT_ROUTES:
            xs = []; xg = []
            for sd in SEEDS:
                y = ys[sd]
                if not y or y <= 0: continue
                a = abs(routesum(sd, n, q0, q1, kind, f, l, cp))
                xs.append(a / y)
                if gs_[sd] and gs_[sd] > 0: xg.append(a / gs_[sd])
            emit(n, wn, key, xs, xg)
        for lab, only in (('投資家の貯蓄÷年所得', True), ('全階級の貯蓄÷年所得', False)):
            xs = []; xg = []
            for sd in SEEDS:
                y = ys[sd]
                if not y or y <= 0: continue
                s = sav(sd, n, q0, q1, only)
                if s is None: continue
                yr = y / ((q1 - q0 + 1) / 4.0)
                xs.append(s / yr)
                if gs_[sd] and gs_[sd] > 0: xg.append(s / (gs_[sd] / ((q1 - q0 + 1) / 4.0)))
            emit(n, wn, lab, xs, xg)
print('\n'.join(out))
