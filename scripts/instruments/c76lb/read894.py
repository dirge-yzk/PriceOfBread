# (894) c76lb：ident の生（計器つき 10 種）の landbase_s・landbasereg_s を読む（記録・採否でない）。
#   python -X utf8 read894.py <ins のフォルダ> <出力.txt>
#   J-a＝landClaimExtShareUpperPerMonth÷landClaimPerMonth・J-b＝landClaimExtGrossUpperPerMonth÷landClaimPerMonth（請求が正の国×四半期）。
#   窓＝1783-85・1786-99・1800-49・1850-99・1900-50（year の列）。中央は国×四半期の値の中央（国ごとの中央は 10 種の全ての四半期をまとめた中央）。
import csv, glob, math, os, re, sys
from collections import defaultdict

WIN = [(1783, 1785), (1786, 1799), (1800, 1849), (1850, 1899), (1900, 1950)]


def win(y):
    for a, b in WIN:
        if a <= y <= b:
            return '%d-%02d' % (a, b % 100)
    return None


def med(xs):
    xs = sorted(xs)
    if not xs:
        return float('nan')
    n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])


def q(xs, p):
    xs = sorted(xs)
    if not xs:
        return float('nan')
    return xs[min(len(xs) - 1, int(p * (len(xs) - 1) + 0.5))]


def f(s):
    return float('nan') if s == 'nan' else float(s)


def main():
    ins, out = sys.argv[1], sys.argv[2]
    L = []
    paid = defaultdict(list); ja = defaultdict(list); jb = defaultdict(list); first = defaultdict(list)
    jaw = defaultdict(list); jbw = defaultdict(list); firstw = defaultdict(list); lagw = defaultdict(list)
    shortw = defaultdict(int); carw = defaultdict(int); shortSizeW = defaultdict(float); noPrice = 0; rows = 0; flag = 0
    nanDeg = 0
    for p in sorted(glob.glob(os.path.join(ins, 'landbase_s*.tsv'))):
        with open(p, encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                rows += 1
                y = int(r['year']); w = win(y); n = r['nation']
                flag += int(r['baseRCheckBreakFlag'])
                if r['privExemptDegreeShare'] == 'nan':
                    nanDeg += 1
                c = f(r['landClaimPerMonth']); po = f(r['landPaidOverClaim'])
                if po == po:
                    paid[n].append(po)
                if c > 0:
                    a = f(r['landClaimExtShareUpperPerMonth']) / c; b = f(r['landClaimExtGrossUpperPerMonth']) / c
                    ja[n].append(a); jb[n].append(b); jaw[w].append(a); jbw[w].append(b)
                fs = f(r['baseOverFirstSeenRatio'])
                if fs == fs:
                    first[n].append(fs); firstw[w].append(fs)
                lg = f(r['baseLag4Ratio'])
                if lg == lg:
                    lagw[w].append(lg)
                shortw[w] += int(r['fpShortPops']); carw[w] += int(r['fpCarrierPops']); shortSizeW[w] += f(r['fpShortSizeK'])
                noPrice += int(r['fpShortNoPricePops'])
    L.append('[0] landbase_s の行 %d・baseRCheckBreakFlag の和 %d・免税の度合いが nan の行 %d' % (rows, flag, nanDeg))
    L.append('[1] 時期ごと（国×四半期の値・10 種）')
    L.append('窓\tJ-a 中央\tJ-a p90\tJ-b 中央\tJ-b p90\t最初の比 中央（p10・p90）\t遅れ 4 の比 中央（p10・p90）\t不足の POP×四半期\t担い手の POP×四半期\t不足の人数（千人・四半期の和）')
    for a, b in WIN:
        w = '%d-%02d' % (a, b % 100)
        L.append('%s\t%.3f\t%.3f\t%.3f\t%.3f\t%.3f（%.3f・%.3f）\t%.3f（%.3f・%.3f）\t%d\t%d\t%.1f' % (
            w, med(jaw[w]), q(jaw[w], .9), med(jbw[w]), q(jbw[w], .9), med(firstw[w]), q(firstw[w], .1), q(firstw[w], .9),
            med(lagw[w]), q(lagw[w], .1), q(lagw[w], .9), shortw[w], carw[w], shortSizeW[w]))
    L.append('不足の POP のうち州の主食の値段が無い数（全ての行の和）：%d' % noPrice)
    L.append('[2] 国ごと（10 種の全ての四半期の中央）')
    L.append('国\t取れた÷請求 中央\tJ-a 中央\tJ-b 中央\t最初の比 中央（p10・p90）\t行')
    for n in sorted(set(paid) | set(ja) | set(first)):
        L.append('%s\t%.3f\t%.3f\t%.3f\t%.3f（%.3f・%.3f）\t%d' % (n, med(paid[n]), med(ja[n]), med(jb[n]), med(first[n]), q(first[n], .1), q(first[n], .9), len(first[n])))
    allpaid = [med(v) for v in paid.values() if v]
    L.append('取れた÷請求の国ごとの中央の、全ての国の中央：%.3f（国 %d）' % (med(allpaid), len(allpaid)))
    # 州の表
    zero = 0; regrows = 0; absr = 0; absn = 0; emptyr = 0; zeroBy = defaultdict(int); tierZero = defaultdict(int)
    for p in sorted(glob.glob(os.path.join(ins, 'landbasereg_s*.tsv'))):
        with open(p, encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                regrows += 1
                if r['regionZeroBaseFlag'] == '1':
                    zero += 1; zeroBy[r['nation']] += 1; tierZero[r['tier']] += 1
                a = int(r['regionAbsenteeRgoCount'])
                if a > 0:
                    absr += 1; absn += a
                if int(r['regionRgoOwnerEmpty']) > 0:
                    emptyr += 1
    L.append('[3] landbasereg_s の行 %d・regionZeroBaseFlag＝1 の州×四半期 %d（格ごと %s・国ごと上位 %s）' % (
        regrows, zero, dict(tierZero), sorted(zeroBy.items(), key=lambda x: -x[1])[:8]))
    L.append('    不在地主の RGO がある州×四半期 %d（RGO の数の和 %d）・持ち主の空の RGO がある州×四半期 %d' % (absr, absn, emptyr))
    with open(out, 'wb') as fh:
        fh.write(('\n'.join(L) + '\n').encode('utf-8'))
    print('\n'.join(L[:12]))


main()
