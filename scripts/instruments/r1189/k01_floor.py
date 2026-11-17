# (1189) K01 扶養力の床の紙の予言：anchor_s で capacity（床の後）が 0.3×anchorUsed（＝DiagAnchorBaseline＝床の錨）に等しいか下の行を数える＝床が効いた行。
#  母集合：anchor_s の全ての行（計器の国×四半期）。capacity が非有限か 1e-9 以下の行は別に数える（エンジンの否定形の門が人口の段を飛ばす＝床の外）。
import csv, os, sys, math
sys.stdout.reconfigure(newline='\n')
for B in sys.argv[1:]:
    n = bind = bad = 0; mn = float('inf')
    for s in range(1, 11):
        p = os.path.join(B, f'anchor_s{s}.tsv')
        if not os.path.exists(p): continue
        for r in csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'):
            try: cap, base = float(r['capacity']), float(r['anchorUsed'])
            except ValueError: continue
            n += 1
            if not (cap > 1e-9) or not math.isfinite(base): bad += 1; continue
            if base > 0:
                x = cap / (0.3 * base); mn = min(mn, x)
                if x <= 1 + 1e-12: bind += 1
    print(f'{B}\t行 {n}\t床が効いた行 {bind}\t扶養力が 0 か非有限 {bad}\t扶養力÷(0.3×錨) の最小 {mn:.4g}')
