# (1437) 型 K の貯蓄の縛り：φ＜0.05 の束の国の中の買いの呼びのうち、買い手の投資家の貯蓄が 0 以下だった呼びの割合（trrail の natInvestorSavingsZeroCallsPhiLow÷natCallsPhiLow・計器の定義どおり）
# 1870〜1950・q1c の止まった州×種だけ。出力：標準出力
import sys, csv
raw, out = sys.argv[1], sys.argv[2]
st = {}
for r in csv.DictReader(open(f"{out}/q1c_rows.tsv", encoding='utf-8'), delimiter='\t'): st[(int(r['seed']), r['region'])] = 'K' if r['tier'] in ('Core', 'Integrated') else 'C'
A = {'K': [0.0, 0.0], 'C': [0.0, 0.0]}
for s in range(1, 11):
    with open(f"{raw}/trrail_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for row in rd:
            k = st.get((s, row[ix['region']]))
            if k is None or not (1870 <= int(row[ix['year']]) <= 1950): continue
            A[k][0] += float(row[ix['natCallsPhiLow']] or 0); A[k][1] += float(row[ix['natInvestorSavingsZeroCallsPhiLow']] or 0)
for k, (c, z) in A.items(): print(f"型 {k}：φ<0.05 の呼び {c:.0f}・貯蓄 0 以下 {z:.0f}（{z / c if c else float('nan'):.2f}）")
