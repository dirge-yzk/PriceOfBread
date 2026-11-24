# 診断（結果の後・記録だけ）：国×年の partShare の最小の分布（旗 1 で構造 0.4 の 1 本なら 0.69 以上は困窮の項だけで届く稀な場合）
import sys, os, csv
d = sys.argv[1]; b = [0]*6; lawc = [0, 0]
for s in range(1, 11):
    code = {}
    with open(os.path.join(d, f'scalars_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); iy, inn, ic = h.index('year'), h.index('nation'), h.index('womenRightsCode')
        for x in r: code[(x[inn], int(x[iy]))] = int(float(x[ic]))
    mn = {}
    with open(os.path.join(d, f'lwk_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); iy, inn, ip = h.index('year'), h.index('nation'), h.index('partShare')
        for x in r:
            k = (x[inn], int(x[iy])); v = float(x[ip]); mn[k] = min(mn.get(k, 9), v)
    for k, v in mn.items():
        if v <= 0.4 + 1e-6: b[0] += 1
        elif v < 0.5: b[1] += 1
        elif v < 0.6: b[2] += 1
        elif v < 0.69: b[3] += 1
        elif v < 0.7 + 1e-9: b[4] += 1
        else: b[5] += 1
        if v > 0.4 + 1e-6: lawc[code.get(k, 0) >= 1] += 1
print(f'{d}\t≦0.4 {b[0]}\t(0.4,0.5) {b[1]}\t[0.5,0.6) {b[2]}\t[0.6,0.69) {b[3]}\t[0.69,0.7] {b[4]}\t>0.7 {b[5]}\t＞0.4 のうち法の段 0 {lawc[0]}・≧1 {lawc[1]}')
