# (1153) c113qn 門の台本（事前登録 measurements/m1153_20261115_prereg_qn.txt の G1・G2・G4 と記録）。読むだけ。
# 使い方: python -X utf8 -I gates.py <リポジトリ> <腕の ins> [--flag0 <旗 0 の ins>] [--g4ref <旗 0 の fuelClaim＞0 の行の数>] [--nofloor]
#   --nofloor＝1 種の突然変異の腕（床は 10 種の和なので除いて読む）。
import sys, os, json
sys.stdout.reconfigure(newline='\n')
a = sys.argv[1:]
REPO, ARM = a[0], a[1]
opt = {a[i]: (a[i + 1] if i + 1 < len(a) and not a[i + 1].startswith('--') else '1') for i in range(2, len(a)) if a[i].startswith('--')}
NOFLOOR = '--nofloor' in opt
W = json.load(open(os.path.join(REPO, 'PriceOfBread.SimCore', 'world.json'), encoding='utf-8'))
QT = {t['name'] for t in W['abstractTargets'] if t.get('quinine')}


def fl(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def read(d):
    seeds = [s for s in range(1, 21) if os.path.exists(os.path.join(d, f'colqn_s{s}.tsv'))]
    S = dict(seeds=seeds, rows=0, malRows=0, g1Got=0.0, g1Dem=0.0, g1Claim=0.0, buyRows=0, sBuy=0.0, sGot=0.0, fuelRows=0,
             oldWant=0.0, oldReq=0.0, oldClaim=0.0, obsCost=0.0, obsClaim=0.0, maxGot=0.0, maxReq=0.0, bad=0, inv=0, invQ=0, short=0)
    for s in seeds:
        f = open(os.path.join(d, f'colqn_s{s}.tsv'), encoding='utf-8')
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            x = l.rstrip('\n').split('\t')
            if len(x) < len(h):
                S['short'] += 1; continue
            v = {k: fl(x[ix[k]]) for k in ('oldWant', 'oldReq', 'oldClaim', 'oldBuyable', 'obsDemand', 'obsGot', 'obsClaim', 'obsCost', 'fuelClaim', 'maxAbsDiffGot', 'maxRelDiffReq')}
            if any(z != z for z in v.values()):
                S['bad'] += 1
            S['rows'] += 1
            if v['oldWant'] > 0:
                S['malRows'] += 1; S['g1Got'] += v['obsGot']; S['g1Dem'] += v['obsDemand']; S['g1Claim'] += v['obsClaim']
            if v['oldBuyable'] > 0:
                S['buyRows'] += 1; S['sBuy'] += v['oldBuyable']; S['sGot'] += v['obsGot']
            if v['fuelClaim'] > 0:
                S['fuelRows'] += 1
            for k in ('oldWant', 'oldReq', 'oldClaim', 'obsCost', 'obsClaim'):
                S[k] += v[k]
            S['maxGot'] = max(S['maxGot'], v['maxAbsDiffGot']); S['maxReq'] = max(S['maxReq'], v['maxRelDiffReq'])
        f.close()
        ip = os.path.join(d, f'invade_s{s}.tsv')
        if os.path.exists(ip):
            f = open(ip, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); it = h.index('target')
            for l in f:
                x = l.rstrip('\n').split('\t'); S['inv'] += 1; S['invQ'] += len(x) > it and x[it] in QT
            f.close()
    return S


def show(lab, S):
    R = (S['sBuy'] - S['sGot']) / S['sBuy'] if S['sBuy'] > 0 else float('nan')
    print(f"{lab}\tseeds={','.join(map(str, S['seeds']))}\trows={S['rows']}\tshort={S['short']}\tnanRows={S['bad']}\tmalRows={S['malRows']}\tg1_sumObsGot={S['g1Got']:.6g}\tg1_sumObsDemand={S['g1Dem']:.6g}\tg1_sumObsClaim={S['g1Claim']:.6g}"
          f"\tbuyRows={S['buyRows']}\tsumOldBuyable={S['sBuy']:.6g}\tsumObsGot={S['sGot']:.6g}\tR={R:.6f}\tfuelRows={S['fuelRows']}"
          f"\tsumOldWant={S['oldWant']:.6g}\tsumOldReq={S['oldReq']:.6g}\tsumOldClaim={S['oldClaim']:.6g}\tsumObsCost={S['obsCost']:.6g}\tsumObsClaim={S['obsClaim']:.6g}"
          f"\tmaxAbsDiffGot={S['maxGot']:.6g}\tmaxRelDiffReq={S['maxReq']:.3g}\tinvade={S['inv']}\tinvadeQuinineTargets={S['invQ']}")
    return R


A = read(ARM)
print(f'# 腕 {ARM}（種 {len(A["seeds"])}・短い行 {A["short"]}）' + ('・床を除いて読む（1 種）' if NOFLOOR else ''))
R = show('arm', A)
g4ref = None
if '--flag0' in opt:
    F = read(opt['--flag0']); show('flag0', F); g4ref = F['fuelRows']
if '--g4ref' in opt:
    g4ref = int(opt['--g4ref'])
fl1 = NOFLOOR or A['malRows'] >= 10000
fl2 = NOFLOOR or A['buyRows'] >= 1000
g1 = A['g1Got'] == 0 and A['g1Dem'] == 0 and A['g1Claim'] == 0 and A['malRows'] > 0 and fl1
g2 = 0.9 <= R <= 1.1 and fl2
g4 = (A['fuelRows'] >= 0.5 * g4ref) if g4ref is not None else None
print(f"G1 狙い（旧の規則＞0 の行で実の調達・需要・請求の和＝0・床 ≧10000）：{'緑' if g1 else '赤'}（行 {A['malRows']}・調達 {A['g1Got']:.6g}・需要 {A['g1Dem']:.6g}・請求 {A['g1Claim']:.6g}）")
print(f"G2 大きさ（R＝Σ(旧で買えた量−実の調達)÷Σ旧で買えた量 が 0.9〜1.1・床 ≧1000）：{'緑' if g2 else '赤'}（R {R:.6f}・行 {A['buyRows']}）")
print(f"G4 壊さない・燃料（fuelClaim＞0 の行 ≧ 旗 0 の 0.5 倍）：{'―（基準なし）' if g4 is None else ('緑' if g4 else '赤')}（腕 {A['fuelRows']}・旗 0 {g4ref}）")
print(f"記録：計器の自己一致 maxAbsDiffGot {A['maxGot']:.6g}・maxRelDiffReq {A['maxReq']:.3g}・NaN の行 {A['bad']}・キニーネの標的への侵略 {A['invQ']}/{A['inv']}")
