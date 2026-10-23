# (504) 線 fd の読み手：計器の表 fd_s<seed>.tsv（州×四半期・挙げた国だけ）から、事前登録 measurements/fd504_20261023_prereg.txt の M1〜M6 と記録の量を出す。読むだけ。
# 使い方：PYTHONUTF8=1 python scripts/instruments/fd504/an_fd.py <計器のフォルダ（ins）> [出力]
# 判定は事前登録の字面のまま（この読み手は結果を見て線を動かさない）。M3 の 0.5・M4 の 0.9／0.98・M5 の 0.2・M6 の 1.0 は事前登録の数。
# ⚠限界：fd_s は挙げた国の州だけ・四半期の末の断面（直近の月の 1 回）・旗①②③が全て 0 の腕では表が無い。
import csv, glob, os, sys, statistics, collections, math

folder = sys.argv[1]
out = open(sys.argv[2], 'w', encoding='utf-8', newline='\n') if len(sys.argv) > 2 else sys.stdout


def f(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


rows = []
for path in sorted(glob.glob(os.path.join(folder, 'fd_s*.tsv'))):
    seed = int(os.path.basename(path)[4:-4])
    for r in csv.DictReader(open(path, encoding='utf-8'), delimiter='\t'):
        d = {k: (v if k in ('nation', 'region', 'tier') else f(v)) for k, v in r.items()}
        d['seed'] = seed
        rows.append(d)
print('(504) fd_s の読み（%s）・行 %d・種 %d' % (folder, len(rows), len(set(r['seed'] for r in rows))), file=out)
if not rows:
    print('行が無い（旗①②③が全て 0 の腕か、計器が無い）', file=out)
    sys.exit(0)


def med(v):
    v = [x for x in v if x == x]
    return statistics.median(v) if v else float('nan')


# M1 向き
bad1 = [r for r in rows if r['fillNew'] < r['fillOld'] - 1e-12 or r['fillNew'] > 1 + 1e-9 or r['carryPerMonth'] < -1e-12]
print('M1 ①②の向き：違反の行 %d／%d（要 0）' % (len(bad1), len(rows)), file=out)
# M2 保存
m2 = [r for r in rows if r['desiredOldPerMonth'] > 0 and r['marketNeedPerMonth'] > 0]
bad2 = [r for r in m2 if abs(r['desiredPerMonth'] / r['marketNeedPerMonth'] - 1) > 1e-9]
print('M2 ②の保存（予算の前の欲しい量の和＝市場に出す必要量の和）：違反の行 %d／%d（要 0）・最大の相対差 %.3g' % (len(bad2), len(m2), max([abs(r['desiredPerMonth'] / r['marketNeedPerMonth'] - 1) for r in m2] or [float('nan')])), file=out)
# M3 開幕の向き
m3 = [r for r in rows if r['year'] <= 1790 and r['tier'] == 'Core' and r['fillOld'] < 0.98]
share = sum(1 for r in m3 if r['carryPerMonth'] > 0) / len(m3) if m3 else float('nan')
gain = med([r['fillNew'] - r['fillOld'] for r in m3])
print('M3 開幕（1783〜1790・本国格・fillOld<0.98）：行 %d・carry>0 の割合 %.3f（要 ≧0.5）・fillNew−fillOld の中央値 %.4f（要 >0）→ %s' % (len(m3), share, gain, 'PASS' if (share >= 0.5 and gain > 0) else 'FAIL/判定不能'), file=out)
# M4 凶作の向き
lo = [r['marketNeedPerMonth'] / r['stapleNeedPerMonth'] for r in rows if r['harvestMean'] < 0.9 and r['stapleNeedPerMonth'] > 0]
hi = [r['marketNeedPerMonth'] / r['stapleNeedPerMonth'] for r in rows if r['harvestMean'] >= 0.98 and r['stapleNeedPerMonth'] > 0]
# 州ごとの対（同じ州の最悪の収穫の四半期と最良の四半期の差）
byreg = collections.defaultdict(list)
for r in rows:
    if r['stapleNeedPerMonth'] > 0:
        byreg[(r['seed'], r['nation'], r['region'])].append((r['harvestMean'], r['marketNeedPerMonth'] / r['stapleNeedPerMonth']))
pairs = []
for k, v in byreg.items():
    v = [x for x in v if x[0] == x[0]]
    if len(v) >= 8:
        v.sort()
        pairs.append(med([m for _, m in v[:max(1, len(v) // 10)]]) - med([m for _, m in v[-max(1, len(v) // 10):]]))
pos = sum(1 for p in pairs if p > 0) / len(pairs) if pairs else float('nan')
print('M4 凶作の向き：harvestMean<0.9 の行 %d（市場に出す割合の中央値 %.4f）・≧0.98 の行 %d（%.4f）→ %s／州ごとの対（最悪の 1 割−最良の 1 割）が正の州の割合 %.3f（%d 州）' % (len(lo), med(lo), len(hi), med(hi), 'PASS' if (lo and hi and med(lo) > med(hi)) else 'FAIL/判定不能', pos, len(pairs)), file=out)
# M5 挟みが働いていたか
m5 = sum(1 for r in rows if r['clampBindShare'] > 0) / len(rows)
print('M5 ②の挟み：旧の挟みが縛っていた重みが正の行の割合 %.3f（要 ≧0.2）→ %s' % (m5, 'PASS' if m5 >= 0.2 else 'FAIL'), file=out)
# M6 総需要の倍率
ratios = [r['desiredPerMonth'] / r['desiredOldPerMonth'] for r in rows if r['desiredOldPerMonth'] > 0]
print('M6 総需要の倍率 desired／desiredOld：中央値 %.4f（要 ≧1.0）・25%% %.4f・75%% %.4f' % (med(ratios), sorted(ratios)[len(ratios) // 4] if ratios else float('nan'), sorted(ratios)[3 * len(ratios) // 4] if ratios else float('nan')), file=out)

# 記録
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
bynat = collections.defaultdict(list)
for r in rows:
    if r['year'] <= 1799:
        bynat[r['nation']].append(r)
print('  国別（1783〜1799・本国と植民地の合算）：国／行／充足の人口加重 旧→新／0.8 未満の人口の割合 旧→新', file=out)
for nat in sorted(bynat):
    s = bynat[nat]; pk = sum(r['popK'] for r in s)
    print('   %s\t%d\t%.3f→%.3f\t%.3f→%.3f' % (nat, len(s), sum(r['fillOld'] * r['popK'] for r in s if r['fillOld'] == r['fillOld']) / pk, sum(r['fillNew'] * r['popK'] for r in s if r['fillNew'] == r['fillNew']) / pk,
                                              sum(r['belowOldK'] for r in s) / pk, sum(r['belowNewK'] for r in s) / pk), file=out)
