# (504) 線 fd 段0：食料の台帳（主食）の代替の余地を、m500 の計器つき 10 種の pxgoods_s（9 国の財×時点の表）から数える。読むだけ・シミュは回さない。
# 使い方：PYTHONUTF8=1 python scripts/instruments/fd504/step0.py <m500_ins のフォルダ> [出力]
# 数えるもの（国×時点＝pxgoods_s の 1 行群）：
#   主食 5 財（パン・米・野菜・ジャガイモ・トウモロコシ）の 需要 D_g・不足 S_g（lastShortageQty）・余り R_g＝max(0, 産出＋在庫−需要)。在庫は期末の値・産出は直近の月（近似）。
#   上限の見積り X＝min(Σ_g S_g , Σ_g R_g)／Σ_g D_g（国の主食の需要に対する、〈不足の主食を余りの主食で埋められる最大の割合〉＝台帳を和にしたときの充足の上がり幅の上限）。
#   同時の条件＝Σ S_g>0 かつ Σ R_g>0 の国×時点の割合。
# ⚠限界：量は財の単位のまま足す（栄養の換算はしない＝台帳も量を足す）・国の合計＝州の偏りを見ない（州×月の同時はソースの形の話で、ここは上の方の見積り）・在庫は期末・産出は直近の月。
import csv, sys, glob, os, collections, statistics
folder = sys.argv[1]
out = open(sys.argv[2], 'w', encoding='utf-8', newline='\n') if len(sys.argv) > 2 else sys.stdout
STAPLES = ['パン', '米', '野菜', 'ジャガイモ', 'トウモロコシ']
def f(x):
    try: return float(x)
    except: return float('nan')
rows = []  # (seed, nation, time, year, {good:(D,S,P,Stock)})
for path in sorted(glob.glob(os.path.join(folder, 'pxgoods_s*.tsv')), key=lambda p: int(os.path.basename(p)[9:-4])):
    seed = int(os.path.basename(path)[9:-4])
    by = collections.defaultdict(dict)
    for r in csv.DictReader(open(path, encoding='utf-8'), delimiter='\t'):
        if r['goodName'] in STAPLES:
            by[(r['nation'], r['time'], int(r['year']))][r['goodName']] = (f(r['lastDemandQty']), f(r['lastShortageQty']), f(r['lastProducedQty']), f(r['stockQty']))
    for (nat, t, y), d in by.items():
        if len(d) == len(STAPLES): rows.append((seed, nat, t, y, d))
def metrics(d):
    D = sum(d[g][0] for g in STAPLES); S = sum(d[g][1] for g in STAPLES)
    R = sum(max(0.0, d[g][2] + d[g][3] - d[g][0]) for g in STAPLES if d[g][1] <= 1e-9 * max(d[g][0], 1e-12) + 1e-12)   # 不足のない財の余りだけ
    X = min(S, R) / D if D > 0 else float('nan')
    return D, S, R, X
def band(y):
    return '1783-1799' if y < 1800 else '1800-1849' if y < 1850 else '1850-1899' if y < 1900 else '1900-1950'
agg = collections.defaultdict(list)
for seed, nat, t, y, d in rows:
    D, S, R, X = metrics(d)
    agg[(nat, band(y))].append((S / D if D > 0 else float('nan'), X, 1 if (S > 0 and R > 0) else 0))
print('(504) 線 fd 段0：主食の代替の余地（pxgoods_s の主食 5 財・国×時点・10 種・上限の見積り）', file=out)
print('入力フォルダ=%s ・行群=%d（国×時点×種）・⚠ pxgoods_s は 1783〜1799 年だけを出す表（それ以後の断面は無い）' % (folder, len(rows)), file=out)
print('列：国／期間／行群の数／主食の不足の割合 ΣS/ΣD の中央値／上限の見積り X の中央値／X の 25%・75%／同時（不足と余りが両方ある）の割合', file=out)
def q(v, p):
    v = sorted(x for x in v if x == x); return v[min(len(v) - 1, int(p * len(v)))] if v else float('nan')
for key in sorted(agg):
    v = agg[key]
    sh = [a for a, _, _ in v]; xs = [b for _, b, _ in v]; co = [c for _, _, c in v]
    print('%s\t%s\t%d\t%.3f\t%.3f\t%.3f\t%.3f\t%.3f' % (key[0], key[1], len(v), q(sh, .5), q(xs, .5), q(xs, .25), q(xs, .75), sum(co) / len(co)), file=out)
# 開幕（w0＝助走の始め・q0＝暦の最初の四半期）
print('\n開幕の断面（時点 w0 と q0・10 種の中央値）：財ごとの 不足/需要 と 在庫/需要', file=out)
for nat in sorted(set(r[1] for r in rows)):
    for tt in ('w0', 'q0'):
        parts = []
        for g in STAPLES:
            sv = [r[4][g][1] / r[4][g][0] for r in rows if r[1] == nat and r[2] == tt and r[4][g][0] > 0]
            kv = [r[4][g][3] / r[4][g][0] for r in rows if r[1] == nat and r[2] == tt and r[4][g][0] > 0]
            parts.append('%s 不足%.2f 在庫%.2f' % (g, statistics.median(sv) if sv else float('nan'), statistics.median(kv) if kv else float('nan')))
        xs = [metrics(r[4])[3] for r in rows if r[1] == nat and r[2] == tt]
        print('%s %s\t%s\tX中央値%.3f' % (nat, tt, ' / '.join(parts), statistics.median(xs) if xs else float('nan')), file=out)

# ---------------------------------------------------------------- ② 主食の相対価格の挟み（PriceFactorMin 0.4／上は PriceFactorMax 1.4）が縛っていた割合 ----------------------------------------------------------------
# 近似：コードの ownRatio＝州の主食の値段÷参照価格。pxgoods_s の priceOverRefTimesLevelMedianRatio（値段÷（参照価格×物価水準）の州の中央値）に regionPriceLevelMeanIndex を掛けて代える（物価水準の定義の差は残る＝上の方の近似）。
# subst_g＝(主食の幾何平均 ÷ own_g)^CrossElasticity（0.6）・f_g＝clamp(subst_g, 0.4, 1.4)。需要の重み e_g＝BaseNeed（パン 0.55・米 0.2・野菜 0.25・ジャガイモ 0.06・トウモロコシ 0.04）。
# 読む量：縛られた財の需要の重み割合 Σ e_g[f_g が挟みの端]／Σ e_g の国×時点の中央値と、挟みで総需要がずれた割合 Σ e_g f_g／Σ e_g（旧の総需要の倍率）と、挟みを外した新しい総需要の倍率（1.0 に正規化されるので 1／（Σ e_g subst_g／Σ e_g））。
E = {'パン': 0.55, '米': 0.2, '野菜': 0.25, 'ジャガイモ': 0.06, 'トウモロコシ': 0.04}
import math
CE = 0.6
rows2 = []  # (seed,nation,time,year,{g:own})
for path in sorted(glob.glob(os.path.join(folder, 'pxgoods_s*.tsv')), key=lambda p: int(os.path.basename(p)[9:-4])):
    seed = int(os.path.basename(path)[9:-4])
    by = collections.defaultdict(dict)
    for r in csv.DictReader(open(path, encoding='utf-8'), delimiter='\t'):
        if r['goodName'] in E:
            a = f(r['priceOverRefTimesLevelMedianRatio']); lv = f(r['regionPriceLevelMeanIndex'])
            if a == a and lv == lv and a > 0 and lv > 0: by[(r['nation'], r['time'], int(r['year']))][r['goodName']] = a * lv
    for k, d in by.items():
        if len(d) == len(E): rows2.append((seed, k[0], k[1], k[2], d))
agg2 = collections.defaultdict(list)
for seed, nat, t, y, d in rows2:
    mean = math.exp(sum(math.log(d[g]) for g in E) / len(E))
    sub = {g: (mean / d[g]) ** CE for g in E}
    fc = {g: min(1.4, max(0.4, sub[g])) for g in E}
    tot = sum(E.values())
    bound = sum(E[g] for g in E if sub[g] < 0.4 or sub[g] > 1.4) / tot
    old_scale = sum(E[g] * fc[g] for g in E) / tot
    unclamped_scale = sum(E[g] * sub[g] for g in E) / tot
    agg2[(nat, band(y))].append((bound, old_scale, unclamped_scale))
print('\n② 主食の相対価格の挟み（0.4〜1.4）が縛っていた需要の重みの割合と総需要の倍率（pxgoods_s からの近似・国×時点×種の中央値）', file=out)
print('国\t期間\t行群\t縛られた重みの割合\t旧の総需要の倍率 Σe·f／Σe\t挟みなしの倍率 Σe·subst／Σe（新は 1 に正規化）', file=out)
for key in sorted(agg2):
    v = agg2[key]
    print('%s\t%s\t%d\t%.3f\t%.3f\t%.3f' % (key[0], key[1], len(v), q([a for a, _, _ in v], .5), q([b for _, b, _ in v], .5), q([c for _, _, c in v], .5)), file=out)
allv = [a for v in agg2.values() for a, _, _ in v]
print('全体：縛られた重みの割合が 0 より大きい行群の割合 %.3f・中央値 %.3f' % (sum(1 for a in allv if a > 0) / len(allv), q(allv, .5)), file=out)

# ---------------------------------------------------------------- FoodMet／FoodNeed／FoodFill の書き手・読み手の全数（ソースの grep・機械的な分類） ----------------------------------------------------------------
import re
if len(sys.argv) > 3:
    src = sys.argv[3]
    pat = re.compile(r'\b(FoodMet|FoodNeed|FoodFill)\b')
    wr = re.compile(r'(\bFoodMet\s*(\+=|-=|=(?!=))|\bFoodNeed\s*(\+=|-=|=(?!=))|\bFoodFill\s*(\+=|-=|=(?!=))|p\.FoodMet = 0)')
    print('\n書き手・読み手の全数（%s の *.cs・コメントだけの行は読み手に数えない）：' % src, file=out)
    wrs, rds = [], []
    for path in sorted(glob.glob(os.path.join(src, '*.cs'))):
        for i, line in enumerate(open(path, encoding='utf-8-sig'), 1):
            code = line.split('//')[0]
            if not pat.search(code): continue
            tag = '%s:%d' % (os.path.basename(path), i)
            (wrs if wr.search(code) else rds).append((tag, code.strip()[:150]))
    print('書き手 %d 行・読み手 %d 行' % (len(wrs), len(rds)), file=out)
    for t, c in wrs: print('  書き  ' + t + '  ' + c, file=out)
    for t, c in rds: print('  読み  ' + t + '  ' + c, file=out)
