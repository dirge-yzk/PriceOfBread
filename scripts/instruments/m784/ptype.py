# (784) 港が替わった行（portUsed≠portA）の型の内訳（読むだけ）：跳びの行（lmA≠lmRegion）と〈旧が同じ札で陸路∞〉（lmA==lmRegion かつ landKmA が inf）とその他
#   python -X utf8 -I ptype.py <portlm_s のあるフォルダ>
import sys, glob, os
d = sys.argv[1]; n = jump = same_inf = other = tot = 0
for p in sorted(glob.glob(os.path.join(d, 'portlm_s*.tsv'))):
    with open(p, 'rb') as f: L = f.read().decode('utf-8').split('\n')
    h = L[0].rstrip('\r').split('\t'); ix = {c: i for i, c in enumerate(h)}
    for ln in L[1:]:
        c = ln.rstrip('\r').split('\t')
        if len(c) < len(h) or c[ix['portUsed']] == '': continue
        tot += 1
        if c[ix['portUsed']] == c[ix['portA']]: continue
        n += 1
        if c[ix['lmA']] != c[ix['lmRegion']]: jump += 1
        elif c[ix['landKmA']] == 'inf': same_inf += 1
        else: other += 1
print(f'{d}：行 {tot}・portUsed≠portA {n}（{n/tot:.2%}）＝跳びの行 {jump}（{jump/tot:.2%}）＋旧が同じ札で陸路∞ {same_inf}（{same_inf/tot:.2%}）＋その他 {other}')
