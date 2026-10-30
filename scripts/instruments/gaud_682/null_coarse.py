# (682) 門の棚卸し：要約の残る基線ごとのヌル腕の三つ組（基線・N1・N2）に、粗い門を当てて〈何も変えていないのに落ちる率〉を出す（読むだけ・シミュは回さない）。
# 使い方：python -X utf8 -I scripts/instruments/gaud_682/null_coarse.py
# 数の出所＝measurements/m584n_20261027_null.txt・m592n_20261027_null.txt・m609n_20261028_null.txt・m617n_20261028_null.txt・
#   m627n_20261028_null.txt・m645n_20261029_null.txt・m655n_20261030_null.txt・m675n_20261030_null.txt の表の行（手で写した・下の表）。
# 順序つきの組＝（腕に見立てる側, 比べる側）＝三つ組から 6 組×8 基線＝48 組。〈落ちる〉＝その門の向きで腕に見立てた側が門を割る。
import itertools
# 基線: [(腕, exact, near, NG, 空洞化1/4未満, 補給崩壊, 推定秒, 価格水準の最大)]
T = {
 'm584': [('m584',44.8,82.6,89,8,11,210.3,None),('N1',41.1,82.8,78,15,5,210.8,None),('N2',42.5,84.4,75,26,35,208.5,None)],
 'm592': [('m592',45.1,82.3,92,8,8,209.8,None),('N1',41.8,82.3,80,16,7,210.1,None),('N2',43.4,84.8,79,22,32,208.3,None)],
 'm609': [('m609',43.9,81.5,78,10,98,211.9,1.63e4),('N1',43.4,81.8,80,7,1,213.7,11.1),('N2',44.3,81.5,85,9,5,212.9,6.36)],
 'm617': [('m617',45.9,81.8,93,9,29,216.1,11.88),('N1',48.4,84.6,95,11,12,216.0,47.98),('N2',43.4,82.5,90,10,3,216.9,10.23)],
 'm627': [('m627',44.6,85.4,84,12,3,220.1,10.18),('N1',45.6,82.5,94,6,4,217.5,5.93),('N2',44.4,83.4,93,6,8,215.1,55.71)],
 'm645': [('m645',43.6,83.6,82,3,2,220.0,6.862),('N1',43.9,84.6,93,3,4,218.7,3.642),('N2',44.8,84.8,89,13,9,215.8,12.36)],
 'm655': [('m655',43.4,82.8,85,4,4,216.6,6.558),('N1',45.7,83.9,97,5,2,218.7,3.642),('N2',44.9,83.3,103,6,3,217.5,3.746)],
 'm675': [('m675',42.8,81.6,100,8,2,216.2,6.873),('N1',41.8,83.8,87,3,27,215.2,14.52),('N2',44.9,84.9,100,5,1,215.7,3.943)],
}
pairs = []
for b, rows in T.items():
    for a, c in itertools.permutations(rows, 2):
        pairs.append((b, a, c))
def rate(name, pred, usable=lambda a, c: True):
    P = [(b, a, c) for b, a, c in pairs if usable(a, c)]
    hit = [(b, a[0], c[0]) for b, a, c in P if pred(a, c)]
    print(f'{name}\t{len(hit)}/{len(P)}\t{100*len(hit)/len(P):.0f}%\t例: ' + ', '.join(f'{b}:{x}対{y}' for b, x, y in hit[:6]))
print('門\t落ちた組/組\t率\t例（基線:腕に見立てた側 対 比べる側）')
for X in (0.5, 1.0, 2.0, 3.4, 4.7, 6.0, 7.5, 12.0, 14.0):
    rate(f'計算量の増分 > +{X} 秒（割り当て・許しの型）', lambda a, c, X=X: a[6] - c[6] > X)
rate('補給崩壊が 2 倍以上（★(397) で外した物差し・navf3 の G4）', lambda a, c: a[5] >= 2 * c[5], lambda a, c: c[5] > 0)
rate('補給崩壊が 2 倍以上 かつ 差が 10 件以上', lambda a, c: a[5] >= 2 * c[5] and a[5] - c[5] >= 10, lambda a, c: c[5] > 0)
rate('空洞化 1/4 未満が 2 倍以上（igi4 の〈判定不能〉の型）', lambda a, c: a[4] >= 2 * c[4], lambda a, c: c[4] > 0)
rate('空洞化 1/4 未満が +5 件以上', lambda a, c: a[4] - c[4] >= 5)
rate('exact が −3pt 超（近似の累積の線の型）', lambda a, c: c[1] - a[1] > 3)
rate('near が −3pt 超', lambda a, c: c[2] - a[2] > 3)
rate('exact か near が −10pt 以上（★(397) で外した物差し）', lambda a, c: c[1] - a[1] >= 10 or c[2] - a[2] >= 10)
rate('NG の件数が +10 以上（外した物差し・記録）', lambda a, c: a[3] - c[3] >= 10)
rate('価格水準の最大が比べる側の最大を超える（fsc ⑦ の型〈≦基線の最大〉）', lambda a, c: a[7] > c[7], lambda a, c: a[7] is not None and c[7] is not None)
rate('価格水準の最大が比べる側の 2 倍以上', lambda a, c: a[7] >= 2 * c[7], lambda a, c: a[7] is not None and c[7] is not None)
# 幅：基線ごとの三つ組の最大−最小
print()
print('基線\t計算量の幅(秒)\t補給崩壊の最大÷最小\t1/4未満の最大÷最小\texactの幅\tnearの幅\tNGの幅')
for b, rows in T.items():
    s = [r[6] for r in rows]; sc = [r[5] for r in rows]; h = [r[4] for r in rows]
    print(f'{b}\t{max(s)-min(s):.1f}\t{max(sc)/max(min(sc),1):.1f}\t{max(h)/max(min(h),1):.1f}\t{max(r[1] for r in rows)-min(r[1] for r in rows):.1f}\t{max(r[2] for r in rows)-min(r[2] for r in rows):.1f}\t{max(r[3] for r in rows)-min(r[3] for r in rows)}')
