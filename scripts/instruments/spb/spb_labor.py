"""(spb) 案 B（労働の閉じた式）の止める線の読み手（読むだけ）。使い方：python spb_labor.py <計器の生のフォルダ> [...] > 出力（種ごとの行・タブ区切り）
読む：labor_s（国×四半期・挙げた国は毎四半期・他の国は年末）。年 Y の行（その年の全ての四半期の行）を人力の生産者の能力で重み付けて平均する。
  unempPlain<Y>＝plainUnemploymentShare（工場の労働の担い手の失業率）を plainCapPerMonth で重み付け
  unempFarm<Y> ＝farmUnemploymentShare を farmCapPerMonth で重み付け
  floor<Y>     ＝最低賃金の床に座る州の能力の割合：enactedNearFloorShare×[lawCode>0] を plainCapPerMonth で重み付け（法が無い国は 0 として分母に入る＝世界の人力の能力のうち床に座る割合）
⚠限界：四半期の末の断面（州×月の数ではない）・挙げた 9 国は四半期ごと、他の国は年末の 1 行だけ＝挙げた国の重みが 4 倍。enactedNearFloorShare は工場の労働（LaborId）の床だけ。"""
import csv, glob, os, re, sys, collections
sys.stdout.reconfigure(encoding='utf-8')
YEARS = (1850, 1875, 1900, 1914, 1930)


def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except Exception:
        return 0.0


res = {}
for D in sys.argv[1:]:
    for f in glob.glob(os.path.join(D, 'labor_s[0-9]*.tsv')):
        s = int(re.search(r'_s(\d+)\.tsv$', f).group(1))
        acc = collections.defaultdict(lambda: [0.0, 0.0])
        with open(f, encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                y = int(r['year'])
                if y not in YEARS: continue
                cp, cf = fl(r['plainCapPerMonth']), fl(r['farmCapPerMonth'])
                a = acc[('unempPlain', y)]; a[0] += fl(r['plainUnemploymentShare']) * cp; a[1] += cp
                a = acc[('unempFarm', y)]; a[0] += fl(r['farmUnemploymentShare']) * cf; a[1] += cf
                a = acc[('floor', y)]; a[0] += (fl(r['enactedNearFloorShare']) if fl(r['lawCode']) > 0 else 0.0) * cp; a[1] += cp
        res[s] = {f'{k}{y}': (v[0] / v[1] if v[1] > 0 else float('nan')) for (k, y), v in acc.items()}
keys = [f'{k}{y}' for k in ('unempPlain', 'unempFarm', 'floor') for y in YEARS]
print('seed\t' + '\t'.join(keys))
for s in sorted(res):
    print(f'{s}\t' + '\t'.join(f'{res[s].get(k, float("nan")):.6g}' for k in keys))
