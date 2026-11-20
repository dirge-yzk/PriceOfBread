# (1259) c128dc：観測（disc_s の E 行＝実際に開いた最初の年）の主な行の年と行の数を、基準 m1254 と N1 で並べる（読むだけ）
# 使い方: python -X utf8 -I obs.py <data フォルダ> <出力 .txt>
import sys, json, statistics as st
def load(p):
    with open(p, 'rb') as f:
        return {int(k): v for k, v in json.loads(f.read().decode('utf-8')).items()}
dd = sys.argv[1]
out = open(sys.argv[2], 'w', encoding='utf-8', newline='\n')
def pr(*a): print(*a, file=out)
res = {}
for nm in ('base', 'n1'):
    d = load(f'{dd}/{nm}.json'); L = []
    for s in sorted(d):
        f = {}
        for q, y, nat, row, *_ in d[s]['D']:
            if y < f.get(row, 9999): f[row] = y
        L.append(f)
    res[nm] = L
rows = sorted({r for L in res.values() for f in L for r in f})
def med(L, r): return st.median([f.get(r, 1951) for f in L])
def cnt(L, r): return sum(f.get(r, 1951) < 1951 for f in L)
pr('行の数（種の中央・観測）：基準 ' + str(st.median([len(f) for f in res['base']])) + '・N1 ' + str(st.median([len(f) for f in res['n1']])) + '（思想と制度の 6 行を含む）')
pr('行\t基準の中央（開いた種）\tN1 の中央（開いた種）\t差')
for r in sorted(rows, key=lambda r: med(res['base'], r)):
    b, n = med(res['base'], r), med(res['n1'], r)
    pr(f'{r}\t{b:g}({cnt(res["base"], r)})\t{n:g}({cnt(res["n1"], r)})\t{n-b:+g}')
out.close()
