# (1203) 問い③ S21-14 減債：fsx_s の repayDoneSum・repayFloorSum・repayAboveFloorSum を国ごとに足し、線を超えた国庫の分の割合を出す（計器が書かない行は数えない）
# 使い方: python -X utf8 -I s21_repay.py <生> <種0> <種1> <出力>
import sys, csv, collections
raw, s0, s1, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return None
D = collections.Counter(); A = collections.Counter(); G = collections.Counter()
for s in range(s0, s1 + 1):
    with open(f'{raw}/fsx_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            d, a, g = num(r['repayDoneSum']), num(r['repayAboveFloorSum']), num(r['repayGateMonths'])
            if None in (d, a, g): continue
            D[r['nation']] += d; A[r['nation']] += a; G[r['nation']] += g
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    td, ta = sum(D.values()), sum(A.values())
    o.write(f"種 {s0}〜{s1}・返した額のうち床を超えた分 {ta/td if td else float('nan'):.1%}（返す門が開いた月 {sum(G.values()):.0f}）\n")
    for n in sorted(D, key=lambda k: -D[k]): o.write(f"{n}\t{A[n]/D[n] if D[n] else float('nan'):.1%}\n")
