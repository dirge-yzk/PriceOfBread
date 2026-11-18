# (1203) 問い② 兵の数の四半期の純増÷人口（nation_s）を法の段×戦時・平時で分位に並べる（徴募の流れの上界の目安・純増なので除隊と相殺した後の量）
# 使い方: python -X utf8 -I draftmag.py <生> <種0> <種1> <出力>
import sys, csv, collections
raw, s0, s1, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return None
buck = collections.defaultdict(list)
for s in range(s0, s1 + 1):
    prev = {}
    with open(f'{raw}/nation_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            n = r['nation']; q = int(r['quarter']); sk = num(r['soldierK']); pk = num(r['populationK']); law = r['conscriptionLaw']; war = num(r['atWarMonths'])
            if None in (sk, pk, war) or pk < 100: prev[n] = None; continue
            p = prev.get(n)
            if p and p[0] == q - 1 and p[2] == law:
                buck[(law, 'war' if war > 0 else 'peace')].append((sk - p[1]) / pk)
            prev[n] = (q, sk, law)
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    o.write('law\tstate\tn\tshare_pos\tp50\tp90\tp99\n')
    for k in sorted(buck):
        v = sorted(buck[k]); n = len(v); P = lambda x: v[min(n - 1, int(x * n))]
        o.write(f"{k[0]}\t{k[1]}\t{n}\t{sum(1 for x in v if x > 0)/n:.3f}\t{P(.5):.2e}\t{P(.9):.2e}\t{P(.99):.2e}\n")
