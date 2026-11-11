# (1017) 合流：c94pe の反証役の中①の読み（読むだけ・記録）＝pay_s（InternationalMarket.PaySales の〈商人 0 人〉の枝の累計）を基線と腕で並べる。
#   国ごとの最後の行（累計）を種で足し、枝ごとの回数と額の和と 新÷旧 を出す。商人 0 人の枝が減れば、腕で空の商人の州が払いの受け手を持つようになった分。
#   python -X utf8 -I paydiff1017.py <名>=<ins> ... [--seeds a-b]
import sys, os, csv
csv.field_size_limit(1 << 30)
rng = next((a.split('=', 1)[1] for a in sys.argv[1:] if a.startswith('--seeds=')), None)
args = [a for a in sys.argv[1:] if not a.startswith('--')]
COLS = ['fixHitN', 'fixHitAmt', 'mintEmptyN', 'mintEmptyAmt', 'popEmptyN', 'popEmptyAmt', 'treasEmptyN', 'treasEmptyAmt', 'clearUnmovedN', 'mintUnmovedN']
res = {}
for spec in args:
    name, d = spec.split('=', 1)
    seeds = sorted(int(f[5:-4]) for f in os.listdir(d) if f.startswith('pay_s') and f.endswith('.tsv') and f[5:-4].isdigit())
    if rng: lo, hi = map(int, rng.split('-')); seeds = [s for s in seeds if lo <= s <= hi]
    tot = {c: 0.0 for c in COLS}
    for s in seeds:
        last = {}
        with open(os.path.join(d, f'pay_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: h.index(c) for c in COLS}; inn = h.index('nation')
            for t in r: last[t[inn]] = t
        for t in last.values():
            for c in COLS:
                try: tot[c] += float(t[ix[c]])
                except ValueError: pass
    res[name] = tot
    print(name, f'種 {len(seeds)}', '\t'.join(f'{c} {tot[c]:.6g}' for c in COLS))
names = list(res)
if len(names) >= 2:
    a = res[names[0]]
    for n in names[1:]:
        print(f'新÷旧（{n}÷{names[0]}）', '\t'.join(f'{c} {(res[n][c] / a[c]) if a[c] else float("nan"):.4g}' for c in COLS))
