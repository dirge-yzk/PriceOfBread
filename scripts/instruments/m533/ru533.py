# (533) ロシアの農民（classId=1）の年代：1 人あたりの所得・人数・充足・農奴解放の段（記録・読むだけ）
import csv, glob, os, sys, statistics as st
def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
NAT = sys.argv[1]
DEC = [(1783,1800),(1800,1825),(1825,1850),(1850,1875),(1875,1900),(1900,1925),(1925,1951)]
def read(d):
    inc = {}; sat = {}; serf = {}; lord = {}
    for p in glob.glob(os.path.join(d, 'class_s*.tsv')):
        s = int(os.path.basename(p)[7:-4])
        for x in rows(p):
            if x['nation'] != NAT: continue
            try: y = int(x['year']); c = int(x['classId']); k = float(x['sizeK']); e = float(x['earnedPerMonth'])
            except ValueError: continue
            if c == 1 and k > 0: inc.setdefault(y, []).append(e / k)
            if c == 0 and k > 0: lord.setdefault(y, []).append(e)
    for p in glob.glob(os.path.join(d, 'nation_s*.tsv')):
        for x in rows(p):
            if x['nation'] != NAT: continue
            try: serf.setdefault(int(x['year']), []).append(float(x['serfEmancipation']))
            except ValueError: pass
    for p in glob.glob(os.path.join(d, 'svcbuy_s*.tsv')):
        for x in rows(p):
            if x['nation'] != NAT or x['classId'] != '1': continue
            try: sat.setdefault(int(x['year']), []).append((float(x['popK']), float(x['satisfactionMean'])))
            except ValueError: pass
    return inc, sat, serf, lord
arms = [(a.split('=',1)[0], read(a.split('=',1)[1])) for a in sys.argv[2:]]
print(f'# {NAT} の農民：年代ごとの中央値（1 人あたりの所得 earnedPerMonth÷sizeK・農民の人数 popK・充足の平均・<0.02 の割合）と農奴解放の段の平均・貴族 classId=0 の所得')
for a, b in DEC:
    out = []
    for nm, (inc, sat, serf, lord) in arms:
        iv = [v for y, l in inc.items() if a <= y < b for v in l]
        sv = [t for y, l in sat.items() if a <= y < b for t in l]
        fv = [v for y, l in serf.items() if a <= y < b for v in l]
        lv = [v for y, l in lord.items() if a <= y < b for v in l]
        pk = st.median([p for p, _ in sv]) if sv else float('nan')
        ms = sum(p * q for p, q in sv) / max(1e-9, sum(p for p, _ in sv)) if sv else float('nan')
        fl = sum(p for p, q in sv if q < 0.02) / max(1e-9, sum(p for p, _ in sv)) if sv else float('nan')
        out.append(f'{nm}: 所得/人 {st.median(iv) if iv else float("nan"):.4g} 人数 {pk:.4g} 充足 {ms:.3f} 床 {fl:.3f} 解放 {st.mean(fv) if fv else float("nan"):.2f} 貴族の所得 {st.median(lv) if lv else float("nan"):.4g}')
    print(f'{a}-{b}  ' + '  |  '.join(out))
