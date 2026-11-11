# (966) F の区間の表（<頭>_frun.tsv）を盤面ごとにまとめる：終わり方・cyc の最小の分布・E と組か・戻るまでの四半期の中央。
import sys, statistics as st
for p in sys.argv[1:]:
    rows = [l.rstrip('\n').split('\t') for l in open(p, encoding='utf-8')][1:]
    for b in sorted({r[0] for r in rows}):
        R = [r for r in rows if r[0] == b]
        cy = [float(r[7]) for r in R if r[7] != 'なし']
        up = [int(r[10]) for r in R if r[10].isdigit()]
        ends = {}
        for r in R: ends[r[6]] = ends.get(r[6], 0) + 1
        pe = sum(r[9] == '組' for r in R)
        L = [int(r[5]) for r in R]
        print(b, f'F {len(R)}', f'Eと組 {pe}', f'終わり方 {ends}', f'長さの中央 {st.median(L)}・最大 {max(L)}',
              f'cycの最小の最小 {min(cy):.3g}・中央 {st.median(cy):.3g}・cyc<0.1 {sum(c<0.1 for c in cy)}・床の行なし {sum(r[7]=="なし" for r in R)}',
              f'P≧1までの中央 {st.median(up) if up else "-"}・戻らず {sum(r[10]=="戻らず" for r in R)}', sep='\t')
