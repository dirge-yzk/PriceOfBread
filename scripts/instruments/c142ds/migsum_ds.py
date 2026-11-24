# (1349) c142ds 記録：移民の総量（region_s の国内の流出の和・越境の流出の和・種ごと・千人）。python migsum_ds.py <ins> <種の起点> <種の数>
import sys, os
ins, s0, ns = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
print('種\t国内流出の和K\t越境流出の和K')
T = [0.0, 0.0]
for s in range(s0, s0 + ns):
    p = os.path.join(ins, 'region_s%d.tsv' % s)
    if not os.path.exists(p): print(s, 'MISSING'); continue
    a = b = 0.0
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); i1 = h.index('migrationOutKPerQuarter'); i2 = h.index('crossBorderOutKPerQuarter')
        for line in f:
            v = line.split('\t')
            try: a += float(v[i1]); b += float(v[i2])
            except ValueError: pass
    T[0] += a; T[1] += b
    print('%d\t%.1f\t%.1f' % (s, a, b))
print('和\t%.1f\t%.1f' % tuple(T))
