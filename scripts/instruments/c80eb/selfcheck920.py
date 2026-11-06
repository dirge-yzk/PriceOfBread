# (920) c80eb 列の自己検証。読むだけ。python -X utf8 -I selfcheck920.py <ins フォルダ> [--seeds 1 10]
#   billBlockedClearings ≧ billBlockedDustClearings（全行）・estateDebtMovedPerQuarter の絶対値の最大 ≦1e-9（旗 0 の今の実装で 0）・
#   尾の列の数（cearn 5・cpop 3）と合計の行数・列の和を出す。
import sys, os
a = sys.argv[1:]; s0, s1 = 1, 10
if '--seeds' in a:
    i = a.index('--seeds'); s0, s1 = int(a[i+1]), int(a[i+2]); del a[i:i+3]
d = a[0]
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
bad = 0; rows = 0; mx = 0.0; s = dict(bb=0, bd=0, ed=0, hs=0, dc=0, o1=0); prows = 0; o1set = 0
for sd in range(s0, s1 + 1):
    p = os.path.join(d, f'cearn_s{sd}.tsv')
    if os.path.exists(p):
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                x = l.rstrip('\n').split('\t'); rows += 1
                bb, bd, ed, em, hs = (fl(x[ix[k]]) for k in ['billBlockedClearings', 'billBlockedDustClearings', 'emptyDebtAfterEstate', 'estateDebtMovedPerQuarter', 'heirDebtSettledPerQuarter'])
                if bb < bd: bad += 1
                mx = max(mx, abs(em)); s['bb'] += bb; s['bd'] += bd; s['ed'] += ed; s['hs'] += hs
    p = os.path.join(d, f'cpop_s{sd}.tsv')
    if os.path.exists(p):
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                x = l.rstrip('\n').split('\t'); prows += 1
                s['dc'] += fl(x[ix['debtInCarryPerQuarter']])
                if x[ix['outOtherTop1Route']] != '-': o1set += 1
print(f'cearn 行 {rows}・billBlocked<dust の行 {bad}・|estateDebtMoved| の最大 {mx:.3g}')
print(f'和：billBlockedClearings {s["bb"]:.0f}・dust {s["bd"]:.0f}・emptyDebtAfterEstate {s["ed"]:.0f}・heirDebtSettled {s["hs"]:.6g}')
print(f'cpop 行 {prows}・debtInCarry の和 {s["dc"]:.6g}・outOtherTop1Route が - でない行 {o1set}')
print('自己検証', 'OK' if bad == 0 and mx <= 1e-9 else 'NG')
