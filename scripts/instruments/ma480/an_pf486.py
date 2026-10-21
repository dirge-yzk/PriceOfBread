#!/usr/bin/env python
# (486) 線 pf の読み手＝pay_s（PaySales の保存則の穴の表・累計）から H0（直しの効いた回数）・H2（直した後の穴）を読む（読むだけ）。
# 使い方：python an_pf486.py <ins のフォルダ> [種の数=10]
# 読み方：各国の最後の行（累計）の和を種ごとに出す。年の途中で消えた国は最後の年末の行までしか無い（その後の分は数えられない＝fixHitN を小さく数える向き）。
#   最終の四半期より 4 つ以上前に最後の行がある国を〈消えた国〉として数える（H0 で〈違う種なのに fixHitN＝0〉が出たときの確かめ用）。
import csv, sys, io, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
csv.field_size_limit(1 << 30)
D = sys.argv[1].rstrip('/') + '/'
N = int(sys.argv[2]) if len(sys.argv) > 2 else 10
COLS = ['fixHit', 'mintEmpty', 'popEmpty', 'treasEmpty', 'clearUnmoved', 'mintUnmoved']
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
tot = collections.Counter()
print('seed ' + ' '.join(f'{c}N/Amt' for c in COLS) + '  消えた国の最終行(国数/うち fixHit>0)')
for s in range(1, N + 1):
    last = {}
    try:
        for r in csv.DictReader(open(D + f'pay_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
            q = int(r['quarter'])
            if r['nation'] not in last or q >= last[r['nation']][0]: last[r['nation']] = (q, r)
    except FileNotFoundError:
        print(f'seed{s}: pay_s が無い'); continue
    qmax = max((q for q, _ in last.values()), default=0)
    c = collections.Counter(); gone = goneHit = 0
    for n, (q, r) in last.items():
        for k in COLS:
            c[k + 'N'] += fl(r[k + 'N']); c[k + 'Amt'] += fl(r[k + 'Amt'])
        if q < qmax - 3:
            gone += 1
            if fl(r['fixHitN']) > 0: goneHit += 1
    print(f'seed{s}: ' + ' '.join(f"{c[k+'N']:.0f}/{c[k+'Amt']:.2f}" for k in COLS) + f'  {gone}/{goneHit}')
    for k, v in c.items(): tot[k] += v
print('合計: ' + ' '.join(f"{tot[k+'N']:.0f}/{tot[k+'Amt']:.2f}" for k in COLS))
fh = tot['fixHitN']; cu = tot['clearUnmovedAmt']
print(f"H2 clearUnmovedAmt の累計 {cu:.4f}＝" + ('当たり（0）' if cu == 0 else ('部分（0 超だが fixHitN より 2 桁小さい）' if cu > 0 and fh > 0 and tot['clearUnmovedN'] * 100 <= fh else '外れ')) + f"／fixHitN {fh:.0f}・額 {tot['fixHitAmt']:.2f}")
print(f"記録：商人 0 人の枝に入った回数の合計（fixHit＋mintEmpty＋popEmpty＋treasEmpty）{fh + tot['mintEmptyN'] + tot['popEmptyN'] + tot['treasEmptyN']:.0f}（財の側の穴 popEmpty {tot['popEmptyN']:.0f}・額 {tot['popEmptyAmt']:.2f}＝触らない）")
