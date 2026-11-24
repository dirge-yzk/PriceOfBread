# 州の人口の最大と、最大の後の最小（読むだけ）：python regmin.py <ins> <種> <州の名>
import sys, os
d, s, name = sys.argv[1], int(sys.argv[2]), sys.argv[3]
p = os.path.join(d, f'region_s{s}.tsv')
with open(p, encoding='utf-8') as f:
    h = f.readline().rstrip('\r\n').split('\t')
    ir = [i for i, c in enumerate(h) if c in ('region', 'regionName', 'name')][0]
    ip = [i for i, c in enumerate(h) if c in ('populationK', 'popK')][0]
    iy = h.index('year'); io = h.index('nation')
    ser = []
    for line in f:
        c = line.rstrip('\r\n').split('\t')
        if c[ir] == name: ser.append((int(c[iy]), float(c[ip]), c[io] if io is not None else ''))
pk = max(ser, key=lambda t: t[1]); after = [t for t in ser if t[0] >= pk[0]]; mn = min(after, key=lambda t: t[1])
last = ser[-1]
print(f'{name} 種 {s}: ピーク {pk[1]:.1f}({pk[0]}) 最小 {mn[1]:.1f}({mn[0]}) 比 {mn[1]/pk[1]:.3f} 最後 {last[1]:.1f}({last[0]}) 持ち主 {last[2]}')
