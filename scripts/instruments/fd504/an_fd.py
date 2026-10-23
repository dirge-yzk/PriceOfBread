# (504) 線 fd の読み手：計器の表 fd_s<seed>.tsv（州×四半期・挙げた国だけ）と region_s<seed>.tsv から、事前登録 measurements/fd504_20261023_prereg.txt（追記 2 を含む）の M1〜M6 と記録の量を出す。読むだけ。
# 使い方：PYTHONUTF8=1 python scripts/instruments/fd504/an_fd.py <腕 B の計器のフォルダ（ins）> [基線の計器のフォルダ=D:/PoB_work/keep_500/m500_ins] [出力]
# 判定は事前登録（追記 2）の字面のまま（この読み手は結果を見て線を動かさない）。M3 の 0.5・M4 の 0.9／0.98／0.5・M5 の 0.2・M6 の 1.0 は事前登録の数。
# M1・M2・M6 は配線の確かめ（式から決まる）＝機構が働いた証拠に数えない。働いた証拠は M3（旗 0 の世界との比較）・M4・M5。
# ⚠限界：fd_s は挙げた国の州だけ・四半期の末の断面（直近の月の 1 回）・旗①②③が全て 0 の腕では表が無い。M3 は軌道が割れた 2 つの世界の比較（①②③を合わせた効果）。
import csv, glob, os, sys, statistics, collections, math

folder = sys.argv[1]
base = sys.argv[2] if len(sys.argv) > 2 and os.path.isdir(sys.argv[2]) else 'D:/PoB_work/keep_500/m500_ins'
outp = [a for a in sys.argv[2:] if not os.path.isdir(a)]
out = open(outp[0], 'w', encoding='utf-8', newline='\n') if outp else sys.stdout


def f(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def load(path_glob, prefix, strcols):
    rows = []
    for path in sorted(glob.glob(path_glob)):
        seed = int(os.path.basename(path)[len(prefix):-4])
        for r in csv.DictReader(open(path, encoding='utf-8'), delimiter='\t'):
            d = {k: (v if k in strcols else f(v)) for k, v in r.items()}
            d['seed'] = seed
            rows.append(d)
    return rows


def med(v):
    v = [x for x in v if x == x]
    return statistics.median(v) if v else float('nan')


def pct(v, q):
    v = sorted(x for x in v if x == x)
    return v[min(len(v) - 1, int(q * len(v)))] if v else float('nan')


rows = load(os.path.join(folder, 'fd_s*.tsv'), 'fd_s', ('nation', 'region', 'tier'))
print('(504) fd_s の読み（%s）・行 %d・種 %d・基線 %s' % (folder, len(rows), len(set(r['seed'] for r in rows)), base), file=out)
if not rows:
    print('行が無い（旗①②③が全て 0 の腕か、計器が無い）', file=out)
    sys.exit(0)
flags = sorted(set(int(r['flags']) for r in rows if r['flags'] == r['flags']))
has2 = any(fl & 2 for fl in flags)
print('旗の組（bit1＝①・bit2＝②・bit4＝③の和）：%s' % flags, file=out)

print('\n--- 配線の確かめ（機構が働いた証拠に数えない）---', file=out)
bad1 = [r for r in rows if r['fillNew'] < r['fillOld'] - 1e-12 or r['fillNew'] > 1 + 1e-9 or r['carryPerMonth'] < -1e-12 or r['overNeedPopK'] > 0]
print('M1 ①②の向きと台帳の不変条件 S1：違反の行 %d／%d（要 0）・overNeedPopK>0 の行 %d' % (len(bad1), len(rows), sum(1 for r in rows if r['overNeedPopK'] > 0)), file=out)
m2 = [r for r in rows if r['desiredOldPerMonth'] > 0 and r['marketNeedPerMonth'] > 0]
if has2:
    bad2 = [r for r in m2 if abs(r['desiredPerMonth'] / r['marketNeedPerMonth'] - 1) > 1e-9]
    print('M2 ②の保存（予算の前の欲しい量の和＝市場に出す必要量の和）：違反の行 %d／%d（要 0）・最大の相対差 %.3g' % (len(bad2), len(m2), max([abs(r['desiredPerMonth'] / r['marketNeedPerMonth'] - 1) for r in m2] or [float('nan')])), file=out)
else:
    print('M2 ②の保存：旗②が立っていない腕＝読まない', file=out)
ok6 = [r for r in rows if r['desiredOldPerMonth'] > 0 and r['stapleNeedPerMonth'] > 0 and r['subsistenceMean'] < 1 and r['subsistenceMean'] == r['subsistenceMean']]
ratios = [r['desiredPerMonth'] / r['desiredOldPerMonth'] for r in ok6]
r3 = [r['marketNeedPerMonth'] / (r['stapleNeedPerMonth'] * (1 - r['subsistenceMean'])) for r in ok6]
lvl2 = [a / b for a, b in zip(ratios, r3) if b > 0]
m6 = med(ratios)
print('M6 総需要の倍率 desired／desiredOld：中央値 %.4f（要 ≧1.0）→ %s・25%% %.4f・75%% %.4f／分解：③の増分 r の中央値 %.4f・②の水準（倍率÷r）の中央値 %.4f' % (
    m6, 'PASS' if m6 >= 1.0 else 'FAIL', pct(ratios, .25), pct(ratios, .75), med(r3), med(lvl2)), file=out)

print('\n--- 機構が働いた証拠 ---', file=out)
rb = load(os.path.join(folder, 'region_s*.tsv'), 'region_s', ('nation', 'region', 'tier'))
r0 = load(os.path.join(base, 'region_s*.tsv'), 'region_s', ('nation', 'region', 'tier'))
key = lambda r: (r['seed'], int(r['quarter']), r['nation'], r['region'])
b0 = {key(r): r for r in r0 if r['year'] <= 1790}
m3 = []
for r in rb:
    if r['year'] > 1790 or r['tier'] not in ('Core', 'Integrated'):
        continue
    o = b0.get(key(r))
    if o is None or not (o['foodFillShare'] < 0.98):
        continue
    m3.append((r['foodFillShare'] - o['foodFillShare'], o['foodFillShare'], r['foodFillShare']))
share = sum(1 for d in m3 if d[0] > 0) / len(m3) if m3 else float('nan')
gain = med([d[0] for d in m3])
m3ok = bool(m3) and share >= 0.5 and gain > 0
print('M3 開幕（1783〜1790・Core と Integrated・基線 foodFillShare<0.98）：行 %d・B−基線 の中央値 %.4f（要 >0）・正の行の割合 %.3f（要 ≧0.5）→ %s／基線の中央値 %.4f→B %.4f' % (
    len(m3), gain, share, 'PASS' if m3ok else 'FAIL/判定不能', med([d[1] for d in m3]), med([d[2] for d in m3])), file=out)
core = [r for r in rows if r['year'] <= 1790 and r['tier'] in ('Core', 'Integrated') and r['fillOld'] < 0.98]
print('   参考（採否に使わない）：B の中の fd_s で fillOld<0.98 の行 %d・carry>0 の割合 %.3f・fillNew−fillOld の中央値 %.4f（②が作った過不足を含む）' % (
    len(core), (sum(1 for r in core if r['carryPerMonth'] > 0) / len(core)) if core else float('nan'), med([r['fillNew'] - r['fillOld'] for r in core])), file=out)
byreg = collections.defaultdict(list)
for r in rows:
    if r['stapleNeedPerMonth'] > 0 and r['subsistenceMean'] < 1 and r['weatherHarvestMean'] == r['weatherHarvestMean']:
        byreg[(r['seed'], r['nation'], r['region'])].append((r['weatherHarvestMean'], r['marketNeedPerMonth'] / (r['stapleNeedPerMonth'] * (1 - r['subsistenceMean']))))
diffs = []
for k, v in byreg.items():
    lo = [x for h, x in v if h < 0.9]; hi = [x for h, x in v if h >= 0.98]
    if len(lo) >= 3 and len(hi) >= 3:
        diffs.append(med(lo) - med(hi))
pos = sum(1 for d in diffs if d > 0) / len(diffs) if diffs else float('nan')
dm = med(diffs)
m4ok = bool(diffs) and pos >= 0.5 and dm > 0
nlo = sum(1 for v in byreg.values() for h, _ in v if h < 0.9)
print('M4 凶作の向き（同じ州の対・weatherHarvestMean<0.9 と ≧0.98 の r の中央値の差）：対象の州 %d（天候の凶作の行 %d）・差が正の州の割合 %.3f（要 ≧0.5）・差の中央値 %.4f（要 >0）→ %s' % (
    len(diffs), nlo, pos, dm, 'PASS' if m4ok else 'FAIL/判定不能'), file=out)
lo = [r['marketNeedPerMonth'] / r['stapleNeedPerMonth'] for r in rows if r['harvestMean'] < 0.9 and r['stapleNeedPerMonth'] > 0]
hi = [r['marketNeedPerMonth'] / r['stapleNeedPerMonth'] for r in rows if r['harvestMean'] >= 0.98 and r['stapleNeedPerMonth'] > 0]
print('   参考（旧の M4・プールした中央値）：harvestMean<0.9 の行 %d（市場に出す割合の中央値 %.4f）・≧0.98 の行 %d（%.4f）' % (len(lo), med(lo), len(hi), med(hi)), file=out)
m5 = sum(1 for r in rows if r['clampBindShare'] > 0) / len(rows)
print('M5 ②の挟み：旧の挟みが縛っていた重みが正の行の割合 %.3f（要 ≧0.2）→ %s' % (m5, 'PASS' if m5 >= 0.2 else 'FAIL'), file=out)

allok = (len(bad1) == 0) and (m6 >= 1.0) and m3ok and m4ok and (m5 >= 0.2 or not has2)   # 追記 3：M5 は旗②の腕だけ判定に使う
print('\n機構の判定（M1〜M6・M2 は旗②の腕だけ）：%s' % ('全て通った' if allok else '外れがある（採らない＝事前登録 [5]・追記 2）'), file=out)

print('\n記録（採否に使わない）', file=out)
for lab, sel in (('開幕 1783〜1784 全て', lambda r: r['year'] <= 1784), ('開幕 1783〜1784 本国格', lambda r: r['year'] <= 1784 and r['tier'] == 'Core'), ('植民地格 全期', lambda r: r['tier'] == 'Colonial'), ('植民地格 1783〜1799', lambda r: r['tier'] == 'Colonial' and r['year'] <= 1799)):
    s = [r for r in rows if sel(r)]
    if not s:
        continue
    pk = sum(r['popK'] for r in s)
    print('  %s：行 %d・充足 0.8 未満の人口の割合 旧 %.3f→新 %.3f・充足の人口加重 旧 %.3f→新 %.3f・carry の割合（足した POP の人口÷人口）%.3f' % (
        lab, len(s), sum(r['belowOldK'] for r in s) / pk, sum(r['belowNewK'] for r in s) / pk,
        sum(r['fillOld'] * r['popK'] for r in s if r['fillOld'] == r['fillOld']) / pk, sum(r['fillNew'] * r['popK'] for r in s if r['fillNew'] == r['fillNew']) / pk,
        sum(r['carryPopK'] for r in s) / pk), file=out)
for lab, yr in (('1783〜1784', 1784), ('1783〜1799', 1799)):
    for nm, rr in (('基線 m500', r0), ('腕 B', rb)):
        s = [r for r in rr if r['year'] <= yr and r['activeFlag'] == 1 and r['foodFillShare'] == r['foodFillShare']]
        pk = sum(r['populationK'] for r in s)
        print('  州の充足が 0.8 未満の州の人口の割合 %s %s：%.3f（州単位の近似）' % (lab, nm, sum(r['populationK'] for r in s if r['foodFillShare'] < 0.8) / pk if pk else float('nan')), file=out)
bynat = collections.defaultdict(list)
for r in rows:
    if r['year'] <= 1799:
        bynat[r['nation']].append(r)
print('  国別（1783〜1799・本国と植民地の合算）：国／行／充足の人口加重 旧→新／0.8 未満の人口の割合 旧→新', file=out)
for nat in sorted(bynat):
    s = bynat[nat]; pk = sum(r['popK'] for r in s)
    print('   %s\t%d\t%.3f→%.3f\t%.3f→%.3f' % (nat, len(s), sum(r['fillOld'] * r['popK'] for r in s if r['fillOld'] == r['fillOld']) / pk, sum(r['fillNew'] * r['popK'] for r in s if r['fillNew'] == r['fillNew']) / pk,
                                              sum(r['belowOldK'] for r in s) / pk, sum(r['belowNewK'] for r in s) / pk), file=out)


def natpop(rr, q):
    d = collections.defaultdict(float)
    for r in rr:
        if int(r['quarter']) == q and r['activeFlag'] == 1:
            d[(r['seed'], r['nation'])] += r['populationK']
    return d


qmax = max(int(r['quarter']) for r in rb) if rb else 0
for lab, q in (('1850 年', (1850 - 1783) * 4), ('1900 年', (1900 - 1783) * 4), ('最終四半期 %d' % qmax, qmax)):
    pb, p0 = natpop(rb, q), natpop(r0, q)
    rat = collections.defaultdict(list)
    for k in pb:
        if k in p0 and p0[k] > 0:
            rat[k[1]].append(pb[k] / p0[k])
    mx = max([x for v in rat.values() for x in v] or [float('nan')])
    print('  人口の倍率 B／基線（%s・国別の種の中央値）：%s・全体の最大 %.3f（S2 の線 10）' % (lab, '、'.join('%s %.3f' % (n, med(v)) for n, v in sorted(rat.items())), mx), file=out)
