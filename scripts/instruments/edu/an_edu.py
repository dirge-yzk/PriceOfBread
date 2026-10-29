#!/usr/bin/env python
"""(658) 線 edu の読み手：教育法の目標と施行を計器つき10種の出力から読む（読むだけ・判定ではない）。
使い方：python -I scripts/instruments/edu/an_edu.py <出力>/ins [種の数=10]
事前登録 measurements/edu658_20261030_prereg.txt の O1（入口）・O2（施行）・O5（往復）と記録[4]。
読む表：nation_s（全ての国x毎四半期・openFrontStates の L.EducationLaw）・reform_s（9国は毎四半期・他は年末・educationLawCode と literacyShare）・
        modsig_s（Apply が選んだ姿勢）・fiscal_s（9国・毎月・ledgerEducationPerMonth と収入）。
限界：reform_s の他の国は年末の断面だけ（年内の往復は9国の毎四半期の列でだけ数える）／fiscal_s は9国だけ。"""
import csv, collections, sys, statistics

D = sys.argv[1]
N = int(sys.argv[2]) if len(sys.argv) > 2 else 10
sys.stdout.reconfigure(encoding='utf-8')
SEEDS = range(1, N + 1)
LAW = {0: 'なし', 1: '宗教', 2: '公教育', 3: '義務'}


def rd(name, s):
    try:
        return csv.DictReader(open(f'{D}/{name}_s{s}.tsv', encoding='utf-8'), delimiter='\t')
    except FileNotFoundError:
        return iter(())


print(f'EDUHEAD\t入力={D}\t種={N}')

# O1 入口：四半期 8 超に L.EducationLaw の戦線が開く国x種
o1 = {}
codes = collections.Counter()
rows_all = 0
for s in SEEDS:
    nat = collections.Counter()
    for x in rd('nation', s):
        rows_all += 1
        q = int(x['quarter'])
        for tok in x['openFrontStates'].split(';'):
            if tok.startswith('L.EducationLaw:'):
                c = tok.split(':')[1]
                if q > 8:
                    nat[x['nation']] += 1
                    codes[c] += 1
    o1[s] = nat
n_pairs = sum(len(v) for v in o1.values())
print(f'EDUO1\t入口：四半期8超に戦線が開いた国x種の数={n_pairs}\t戦線の行={sum(codes.values())}\t符号別={dict(sorted(codes.items()))}\t国x四半期の行={rows_all}')
for s in SEEDS:
    if o1[s]:
        print(f'EDUO1S\t種{s}\t' + '; '.join(f'{k}:{v}' for k, v in sorted(o1[s].items())))

# O2 施行・O5 往復・O6 向き・O7 開幕への集中（事前登録の改訂 [9]）
#  年末の断面（quarter%4==3・全ての国）で読む。O2＝段3に到達した国x種の数（開幕の8四半期も含める）。
#  O5＝年末の断面の「段3→段2以下」の件数÷「段2以下→段3」の件数、および9国の毎四半期の列の同じ比。どちらかが 0.5 以上なら採らない。
first3 = {}         # (seed, nation) -> (四半期, 年)  最初に年末の断面で段3だった所
ups3 = downs3 = 0   # 年末の断面の 段2以下→3 / 3→段2以下
ups_any = 0         # 参考：段が上がる全ての件数
ups3q = downs3q = 0 # 9国の毎四半期の列の 段2以下→3 / 3→段2以下
quarterly_flips = collections.Counter()   # 9国：同じ暦年に上げて戻す・戻して上げる
final_code = collections.Counter()
for s in SEEDS:
    hist = collections.defaultdict(list)    # nation -> [(q, year, code)]（全ての行）
    for x in rd('reform', s):
        hist[x['nation']].append((int(x['quarter']), int(x['year']), int(x['educationLawCode'])))
    for n, h in hist.items():
        h.sort()
        ye = [(q, y, c) for (q, y, c) in h if q % 4 == 3]
        for (q0, y0, c0), (q1, y1, c1) in zip(ye, ye[1:]):
            if c1 > c0:
                ups_any += 1
            if c0 <= 2 and c1 == 3:
                ups3 += 1
            elif c0 == 3 and c1 <= 2:
                downs3 += 1
        for (q, y, c) in ye:
            if c == 3 and (s, n) not in first3:
                first3[(s, n)] = (q, y)
        byyear = collections.defaultdict(list)
        for (q, y, c) in h:
            byyear[y].append(c)
        if len(h) >= 4 * (len(byyear) - 1):   # 毎四半期の列（9国）だけ
            for (q0, y0, c0), (q1, y1, c1) in zip(h, h[1:]):
                if c0 <= 2 and c1 == 3:
                    ups3q += 1
                elif c0 == 3 and c1 <= 2:
                    downs3q += 1
            for y, cs in byyear.items():
                if len(cs) >= 4:
                    seq = [c for i, c in enumerate(cs) if i == 0 or c != cs[i - 1]]
                    if len(seq) >= 3:
                        quarterly_flips[(s, n)] += 1
        final_code[h[-1][2]] += 1
nat3 = len(first3)
print(f'EDUO2	施行：段3に到達した国x種={nat3}	年末の断面の段2以下→3={ups3}	3→段2以下={downs3}	（参考）上げ全体={ups_any}	最終の段別(国x種)={dict(sorted(final_code.items()))}')
r_year = (downs3 / ups3) if ups3 else float('nan')
r_q = (downs3q / ups3q) if ups3q else float('nan')
print(f'EDUO5	往復：年末の断面 撤回/制定={r_year:.3f}（{downs3}/{ups3}）	9国の毎四半期 撤回/制定={r_q:.3f}（{downs3q}/{ups3q}）	採らない条件=どちらかが0.5以上	9国の年内往復の国x種x年の件数={sum(quarterly_flips.values())}')

# O7 開幕への集中：最初の段3が四半期8以内（年末の断面では q=3 か 7）の割合
early = sum(1 for (q, y) in first3.values() if q <= 8)
print(f'EDUO7	開幕8四半期以内の制定の割合={(early / nat3) if nat3 else float("nan"):.3f}（{early}/{nat3}）	採らない条件=0.5以上')
print('EDUREC	最初の段3の四半期の度数（年末の断面・q=3,7,11,…）	' + ' '.join(f'{k}:{v}' for k, v in sorted(collections.Counter(q for (q, y) in first3.values()).items())[:12]))

# O6 向き：制定した国x種の、制定の四半期の一人当たり名目GDP（nation_s の nominalGdpPerYear÷populationK）の同じ種・同じ四半期の順位（低い国の割合）。識字も同じ形（記録）
need_q = collections.defaultdict(set)
for (s, n), (q, y) in first3.items():
    need_q[s].add(q)
gdp = {}   # (s, q) -> {nation: gdp/cap}
for s in SEEDS:
    for x in rd('nation', s):
        q = int(x['quarter'])
        if q in need_q[s]:
            try:
                pop = float(x['populationK']); g = float(x['nominalGdpPerYear'])
            except ValueError:
                continue
            if pop > 0 and g == g:
                gdp.setdefault((s, q), {})[x['nation']] = g / pop
lit = {}
for s in SEEDS:
    for x in rd('reform', s):
        q = int(x['quarter'])
        if q in need_q[s] and q % 4 == 3:
            try:
                lit.setdefault((s, q), {})[x['nation']] = float(x['literacyShare'])
            except ValueError:
                pass


def pct(table, s, q, n):
    d = table.get((s, q))
    if not d or n not in d:
        return None
    v = d[n]
    return sum(1 for m, u in d.items() if m != n and u < v) / max(1, len(d) - 1)


pi = [p for (s, n), (q, y) in first3.items() if (p := pct(gdp, s, q, n)) is not None]
pl = [p for (s, n), (q, y) in first3.items() if (p := pct(lit, s, q, n)) is not None]
mi = statistics.median(pi) if pi else float('nan')
ml = statistics.median(pl) if pl else float('nan')
print(f'EDUO6	向き：制定時の一人当たり名目GDPの順位（低い国の割合）の中央値={mi:.3f}（件数={len(pi)}）	採らない条件=0.5未満	（記録）識字の順位の中央値={ml:.3f}（件数={len(pl)}）')

# 記録：段3に上がった年（国ごとの中央値）
by_nat = collections.defaultdict(list)
for (s, n), (q, y) in first3.items():
    by_nat[n].append(y)
for n, ys in sorted(by_nat.items(), key=lambda kv: statistics.median(kv[1])):
    print(f'EDUREC	義務教育の最初の年	{n}	種の数={len(ys)}	中央値={statistics.median(ys):.0f}	最小={min(ys)}	最大={max(ys)}')

# 姿勢の度数（根の確認）
agg = collections.Counter()
for s in SEEDS:
    for x in rd('modsig', s):
        agg[x['stance']] += 1
print('EDUSTANCE\t0=Modernize 1=Industrialize 2=Welfare 3=Fiscal 4=Militarize -1=Apply なし\t' + ' '.join(f'{k}={agg[k]}' for k in sorted(agg)))

# 記録：識字の年の伸び（制定の前後5年）と教育費÷収入（9国）
LIT = []
for (s, n), (q_, y) in first3.items():
    lit = {}
    for x in rd('reform', s):
        if x['nation'] == n and int(x['quarter']) % 4 == 3:
            lit[int(x['year'])] = float(x['literacyShare'])
    if (y - 5) in lit and (y + 5) in lit and y in lit:
        LIT.append(((lit[y] - lit[y - 5]) / 5, (lit[y + 5] - lit[y]) / 5))
if LIT:
    print(f'EDUREC\t識字の年の伸び（制定の前5年→後5年・中央値）\t{statistics.median(a for a, b in LIT):.4f}\t{statistics.median(b for a, b in LIT):.4f}\t件数={len(LIT)}')
else:
    print('EDUREC\t識字の年の伸び\t該当なし（段3に上がった国x種が無い）')
