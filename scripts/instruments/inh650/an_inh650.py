#!/usr/bin/env python
# (650) 相続法の読み手：<出力>/ins の inh_s<seed>.tsv と nation_s<seed>.tsv を読み、事前登録（measurements/inh_650_20261030_prereg.txt・_prereg2.txt）の狙いの数字 ③a ③b ③c ③d ④ ⑥ を出す。
# 使い方：python an_inh650.py <insフォルダ> [種の数=10]
# 読むだけ。力学の値は持たない。崩れ＝nation_s の civilWarMonths>0 の四半期、または polityCode が前の四半期と違う四半期（敗戦は列が無いので入れない）。
import csv, sys, statistics, os, collections

d = sys.argv[1]
N = int(sys.argv[2]) if len(sys.argv) > 2 else 10
BASE = 0.1148  # 基線 m645 の基礎率（base650.py）


def rd(fn):
    with open(fn, encoding='utf-8') as f:
        return list(csv.DictReader(f, delimiter='\t'))


stage_first = collections.defaultdict(list)   # 段 -> 最初の制定の年のリスト
ever = collections.Counter()                   # 段 -> (国・種)の数（一度でもその段以上）
all_set = []                                   # 『全て』を制定した (種, 国, 四半期, 年)
collapse_before = []                           # 『全て』ごとの 直前20四半期に崩れがあるか
ratio = collections.defaultdict(list)          # 段 -> 相続の歳入÷歳入
long_all = []                                  # ④ 普通選挙の国で『全て』が20四半期以上続いた
total_pairs = 0
rows_total = 0
changes = 0
for s in range(1, N + 1):
    fi = os.path.join(d, f'inh_s{s}.tsv')
    fn = os.path.join(d, f'nation_s{s}.tsv')
    if not os.path.exists(fi):
        print(f'!! inh_s{s}.tsv が無い（旗 0 の腕か、計器が出ていない）')
        continue
    inh = rd(fi)
    nat = collections.defaultdict(dict)
    for r in rd(fn):
        nat[r['nation']][int(r['quarter'])] = (float(r['civilWarMonths']), r['polityCode'])
    by = collections.defaultdict(list)
    for r in inh:
        by[r['nation']].append(r)
    for n, rs in by.items():
        rs.sort(key=lambda r: int(r['quarter']))
        total_pairs += 1
        mx = 0
        prev = 0
        run = 0
        run_start = None
        seen = set()
        for r in rs:
            q = int(r['quarter']); y = int(r['year'])
            lv = int(r['inheritanceLawCode'])
            rows_total += 1
            mx = max(mx, lv)
            if lv != prev:
                changes += 1
            for k in range(1, lv + 1):
                if k not in seen:
                    seen.add(k); stage_first[k].append(y)
            rev = float(r['revenuePerMonth'])
            est = float(r['estatePerMonth'])
            if lv > 0 and rev > 0:
                ratio[lv].append(est / rev)
            if lv == 4:
                if run == 0:
                    run_start = (q, y)
                run += 1
                if run == 20 and int(r['suffrageCode']) >= 2:
                    long_all.append((s, n, run_start))
            else:
                run = 0
            prev = lv
        for k in range(1, mx + 1):
            ever[k] += 1
        if mx == 4:
            q4 = next(int(r['quarter']) for r in rs if int(r['inheritanceLawCode']) == 4)
            y4 = next(int(r['year']) for r in rs if int(r['inheritanceLawCode']) == 4)
            all_set.append((s, n, q4, y4))
            # 直前20四半期の崩れ
            tab = nat.get(n, {})
            col = False
            for qq in range(max(1, q4 - 20), q4 + 1):
                cur = tab.get(qq); pr = tab.get(qq - 1)
                if cur is None:
                    continue
                if cur[0] > 0 or (pr is not None and pr[1] != cur[1]):
                    col = True; break
            collapse_before.append(col)

print(f'種={N} 国×種の組={total_pairs} 行={rows_total} 段の変化={changes}')
print('--- ③c 段ごとに「その段以上を一度でも制定した」(国・種)の数と、最初の制定の年の中央値')
for k, name in [(1, '低率'), (2, '中率'), (3, '高率'), (4, '全額')]:
    ys = stage_first[k]
    print(f'  {name}: {ever[k]} 件  最初の年の中央値={statistics.median(ys) if ys else "-"}')
a = ever[4]
b = sum(collapse_before) / len(collapse_before) if collapse_before else float('nan')
print('--- 狙いの数字')
print(f'  ③a 『全額』を制定した(国・種)={a}  (線 ≦30) -> {"通る" if a <= 30 else "破れる"}')
print(f'  ③b 制定の前20四半期に崩れがある割合={b:.3f} (件数 {len(collapse_before)}; 線 ≧0.5 かつ ≧3×基礎率={3*BASE:.3f}) -> '
      + ('件数0 (働いているとは読まない)' if not collapse_before else ('通る' if b >= 0.5 and b >= 3 * BASE else '破れる')))
print(f'  ③d 点火の床: 『低率』以上を制定した(国・種)={ever[1]} (線 ≧20) -> {"通る" if ever[1] >= 20 else "破れる(不点火)"}')
print(f'  ④ 男性普通選挙以上の国で『全額』が20四半期以上続いた(国・種)={len(long_all)}')
for t in long_all[:20]:
    print('    ', t)
print('--- ⑥ 相続の歳入÷歳入(月)の中央値 (段ごと・国×四半期)')
for k, name in [(1, '低率'), (2, '中率'), (3, '高率'), (4, '全額')]:
    v = ratio[k]
    print(f'  {name}: n={len(v)} 中央値={statistics.median(v):.4f}' if v else f'  {name}: n=0')
print('--- 『全額』を制定した(国・種)の一覧')
for t in all_set[:40]:
    print('   ', t)
