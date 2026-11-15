# 読むだけ (1169)：S22-H027（資本逃避・配当・新規流入の対と保存）・H029（外資の吸収）の頻度を cpwdft_s・cpworld_s で数える。
#  逃避が起きた＝flightLedgerIndex＞0。保存＝旧の数え（flightOldIndex）と台帳（flightLedgerIndex）の差・片側の記帳（oneSided*）≠0 の行。
#  世界＝持ち主の対外資産の減り ownerDropGold と受け手の負債の減り（逃避 hostFlightDropGold＋落とし hostWoDropGold）の差。吸収＝absorbed*＞0。
# 使い方: python -X utf8 -I s22_cp.py <生のフォルダ> <種の数>
import sys, os, csv, collections
B, NS = sys.argv[1], int(sys.argv[2])
def f(x):
    try: return float(x)
    except: return float('nan')
n = fl = mis = os1 = os2 = ab1 = ab2 = cp = 0; miss = 0; nat = collections.Counter(); yrs = []
wn = wgap = wun = wwo = wclip = 0; maxgap = 0.0
for s in range(1, NS + 1):
    p, q = os.path.join(B, f'cpwdft_s{s}.tsv'), os.path.join(B, f'cpworld_s{s}.tsv')
    if not (os.path.exists(p) and os.path.exists(q)): miss += 1; continue
    for r in csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'):
        n += 1; a, b = f(r['flightOldIndex']), f(r['flightLedgerIndex'])
        if b > 0: fl += 1; nat[r['nation']] += 1; yrs.append(int(r['year']))
        if abs(a - b) > 1e-6 * max(1.0, abs(a), abs(b)): mis += 1
        os1 += f(r['oneSidedFlight']) != 0; os2 += f(r['oneSidedCoupon']) != 0
        ab1 += f(r['absorbedFlight']) > 0; ab2 += f(r['absorbedCoupon']) > 0
    for r in csv.DictReader(open(q, encoding='utf-8'), delimiter='\t'):
        wn += 1; o = f(r['ownerDropGold']); h = f(r['hostFlightDropGold']) + f(r['hostWoDropGold'])
        g = abs(o - h) / max(1e-9, abs(o), abs(h)) if (o or h) else 0.0
        if g > 1e-6: wgap += 1
        maxgap = max(maxgap, g); wun += f(r['unpaidGold']) > 0; wwo += f(r['woEvents']) > 0; wclip += f(r['clipNations']) > 0
yrs.sort()
med = yrs[len(yrs) // 2] if yrs else None
print(f'種 {NS - miss}/{NS}・cpwdft の国×呼び出し {n}・逃避あり {fl} = {fl / max(n, 1):.1%}（年の中央 {med}）・旧と台帳の食い違い {mis}・片側の記帳 逃避 {os1}・利札 {os2}・吸収あり 逃避 {ab1} = {ab1 / max(n, 1):.1%}・利札 {ab2} = {ab2 / max(n, 1):.1%}・国 ' + ' '.join(f'{k}:{v}' for k, v in nat.most_common(6)))
print(f'cpworld の呼び出し {wn}・持ち主の減り≠受け手の減り（相対 1e-6 超）{wgap}（最大の相対差 {maxgap:.2e}）・未払いあり {wun}・落としの事象あり {wwo}・切った国あり {wclip}')
