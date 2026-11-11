# (875) c73gt pxmonth の年の名乗り（記録だけ・事前登録 [3] (b)）：pxmonth_s*.tsv の year の値の集合と行数を種ごとでなく全体で名乗る。
#   使い方: python -X utf8 -I pxyears875.py <ins フォルダ>
import csv, glob, os, sys
d = sys.argv[1]
ys = set(); n = 0; files = sorted(glob.glob(os.path.join(d, 'pxmonth_s*.tsv')))
for p in files:
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); iy = h.index('year')
        for x in r: ys.add(x[iy]); n += 1
print('pxmonth は %s だけ（ファイル %d・行 %d）＝その後の年の価格の 3 か月の跳びは pxjump も plvl_seeds も数えない（下向きの跳びも含めて誰も数えない）' % ('・'.join(sorted(ys)), len(files), n))
