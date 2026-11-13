# (1095) 段 R の〈運賃を線路の持ち主へ〉の大きさ：trnation（(380) 輸送の段0 の計器）から、世界の運賃のうち陸の脚の分の割合と、
# 鉄道の門（機関車の発見）が開いた国の数を年ごとに数える。使い方: python -X utf8 -I q_landfreight.py <表のフォルダ> > out
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
d = sys.argv[1]
YS = [1790, 1820, 1830, 1840, 1850, 1860, 1870, 1880, 1900, 1913, 1930, 1949]
share = collections.defaultdict(list); gates = collections.defaultdict(list); landdom = collections.defaultdict(list); tot = collections.defaultdict(list)
for s in range(1, 11):
    p = os.path.join(d, f'trnation_s{s}.tsv')
    acc = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0])
    with open(p, encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for line in fi:
            c = line.rstrip('\n').split('\t')
            try: y = int(float(c[ix['year']]))
            except ValueError: continue
            if y not in YS: continue
            try:
                fp = float(c[ix['freightPaidGoldPerYear']]); fl = float(c[ix['freightPaidLandPartGoldPerYear']])
                fld = float(c[ix['freightPaidLandPartDomesticGoldPerYear']]); g = float(c[ix['railGateFlag']])
            except ValueError: continue
            a = acc[y]; a[0] += fp; a[1] += fl; a[2] += fld; a[3] += 1 if g > 0 else 0
    for y, a in acc.items():
        if a[0] > 0: share[y].append(a[1] / a[0]); landdom[y].append(a[2] / a[0])
        gates[y].append(a[3]); tot[y].append(a[0])
print('年\t陸の脚÷運賃（10 種の中央）\tうち国内の成約の陸の脚÷運賃\t鉄道の門が開いた国×行（年の行の延べ）\t運賃の合計（金建て・年率の行の和）')
for y in YS:
    if not share[y]: print(f'{y}\t-'); continue
    print(f'{y}\t{st.median(share[y]):.3f}\t{st.median(landdom[y]):.3f}\t{st.median(gates[y]):.0f}\t{st.median(tot[y]):.4g}')
