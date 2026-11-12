"""(1067) A：同じ国の州どうしの値段の散らばり（今ある生で読める範囲だけ・読むだけ）。
入力＝<ins>/pxgoods_s<種>.tsv（Instruments.cs:1703〜:1721＝国×財ごと・財の名簿 PxGoodPicked だけ・助走〔time=w*〕と試合の最初の 12 年〔time=q0..q47〕だけ）。
州ごとの値段そのものは無い＝読めるのは rel＝値段÷(参照価格×州の物価水準) の州の中央と最大だけ（最小・変動係数は無い）。
指標＝最大÷中央（州が 2 以上の行）。財ごと・時期（助走／1783〜89／1790〜95）ごとに、国×種の行の中央と 90 百分位。
python -X utf8 a_disp.py <ins> <出力>"""
import sys, os, csv, glob, collections, statistics
ins, out = sys.argv[1], sys.argv[2]
acc = collections.defaultdict(list); accT = collections.defaultdict(list); nat = collections.defaultdict(list)
nrows = 0
for p in sorted(glob.glob(os.path.join(ins, 'pxgoods_s*.tsv'))):
    with open(p, encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            try: n = int(r['activeRegions']); med = float(r['priceOverRefTimesLevelMedianRatio']); mx = float(r['priceOverRefTimesLevelMaxRatio'])
            except Exception: continue
            if n < 2 or not (med > 0) or mx != mx: continue
            per = '助走' if r['time'].startswith('w') else ('1783-89' if int(r['year']) < 1790 else '1790-95')
            v = mx / med; nrows += 1
            acc[(r['goodName'], r['tier'], per)].append(v); accT[(r['tier'], per)].append(v); nat[(r['nation'], per)].append(v)


def q(xs, p):
    xs = sorted(xs); return xs[min(len(xs) - 1, int(p * (len(xs) - 1) + 0.5))]


with open(out, 'w', encoding='utf-8') as fo:
    fo.write(f'# 行 {nrows}（州が 2 以上の国×財×時点・10 種）。指標＝州の rel の最大÷中央（rel＝値段÷(参照価格×州の物価水準)）\n')
    fo.write('## 階層×時期：行数・中央・90 百分位・最大\n')
    for k in sorted(accT): xs = accT[k]; fo.write(f'{k[0]}\t{k[1]}\t{len(xs)}\t{statistics.median(xs):.3f}\t{q(xs, .9):.3f}\t{max(xs):.3f}\n')
    fo.write('## 国×時期：行数・中央・90 百分位\n')
    for k in sorted(nat): xs = nat[k]; fo.write(f'{k[0]}\t{k[1]}\t{len(xs)}\t{statistics.median(xs):.3f}\t{q(xs, .9):.3f}\n')
    fo.write('## 財×階層×時期：行数・中央・90 百分位・最大\n')
    for k in sorted(acc): xs = acc[k]; fo.write(f'{k[0]}\t{k[1]}\t{k[2]}\t{len(xs)}\t{statistics.median(xs):.3f}\t{q(xs, .9):.3f}\t{max(xs):.3f}\n')
print('rows', nrows)
