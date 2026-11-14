# (1145) c112hy 門 G1〜G4（事前登録 measurements/m1145_20261115_prereg_hy.txt で腕の前に固定）。
# 使い方：python -I scripts/instruments/c112hy/gates.py <腕の ins> <旗 0 の ins> <基線 M の生> [種の始め 1] [種の終わり 10]
#   旗 0 の ins を '-' にすると G3 を読まない（1 種の突然変異の腕など）。基線 M の生を '-' にすると G4 を読まない。
import sys, os
ARM, OFF, BASE = sys.argv[1], sys.argv[2], sys.argv[3]
S0 = int(sys.argv[4]) if len(sys.argv) > 4 else 1
S1 = int(sys.argv[5]) if len(sys.argv) > 5 else 10
G3_NULL = 1.1072          # 紙の台本のヌル腕の幅（M 0.4946・N1 0.4685・N2 0.4467 の比の最大）
G4_BASE, G4_TOL = 0.983295, 0.0072
FRESH_FLOOR, GRUDGE_FLOOR = 1000, 10000

def dipcool(d):
    s = {'freshRows': 0, 'obsHyenaFreshRows': 0, 'grudgeRows': 0, 'obsHyenaRows': 0, 'oldHyenaRows': 0, 'highRows': 0, 'frozenRows': 0, 'obsOtherRows': 0,
         'sumObsCoolGrudge': 0.0, 'sumOldCoolGrudge': 0.0, 'sumNewCoolGrudge': 0.0, 'sumObsCoolFrozen': 0.0, 'sumFrozenRuleCool': 0.0}
    mx = {'maxAbsDiffNewGrudge': 0.0, 'maxAbsDiffNewHigh': 0.0, 'maxAbsDiffOldHigh': 0.0, 'maxAbsDiffFrozen': 0.0, 'maxAbsDiffLow': 0.0}
    seeds = 0; bad = 0
    for sd in range(S0, S1 + 1):
        p = os.path.join(d, f'dipcool_s{sd}.tsv')
        if not os.path.exists(p): continue
        seeds += 1
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
            for l in f:
                r = l.rstrip('\n').split('\t')
                if len(r) < len(h): bad += 1; continue
                for k in s: s[k] += float(r[ix[k]])
                for k in mx:
                    v = float(r[ix[k]])
                    if not (v == v): v = float('inf')   # 非有限は最大へ
                    if v > mx[k]: mx[k] = v
    return seeds, bad, s, mx

def warband(d):
    st = pe = 0; seeds = 0
    for sd in range(S0, S1 + 1):
        p = os.path.join(d, f'warband_s{sd}.tsv')
        if not os.path.exists(p): continue
        seeds += 1
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
            sc = [i for n, i in ix.items() if n.startswith('start_') and n != 'start_civil']
            for l in f:
                r = l.rstrip('\n').split('\t'); st += sum(int(r[i]) for i in sc); pe += int(r[ix['peaceResets']])
    return seeds, st, pe

na, bad, a, am = dipcool(ARM)
print(f'# 腕 {ARM}（種 {na}・短い行 {bad}）')
print('# 和：' + ' '.join(f'{k}={v:.6g}' for k, v in a.items()))
print('# 最大：' + ' '.join(f'{k}={v:.6g}' for k, v in am.items()))
g1 = a['obsHyenaFreshRows'] / a['freshRows'] if a['freshRows'] > 0 else float('nan')
ok1 = a['freshRows'] >= FRESH_FLOOR and a['obsHyenaFreshRows'] == 0
print(f'G1 狙い ハイエナの枝を取った講和の直後の行の割合={g1:.6f}（{int(a["obsHyenaFreshRows"])}/{int(a["freshRows"])}・床 {FRESH_FLOOR}）→ {"緑" if ok1 else "赤"}')
ok2 = a['grudgeRows'] >= GRUDGE_FLOOR and am['maxAbsDiffNewGrudge'] <= 1e-9
print(f'G2 上側 エンジンと新の式の差の最大={am["maxAbsDiffNewGrudge"]:.6g}（行 {int(a["grudgeRows"])}・床 {GRUDGE_FLOOR}）→ {"緑" if ok2 else "赤"}')
same = a['sumOldCoolGrudge'] / a['sumNewCoolGrudge'] if a['sumNewCoolGrudge'] > 0 else float('nan')
print(f'記録 同じ盤面の旧÷新（腕の盤面・計器の 2 つの規則）={same:.6f}')
if OFF != '-':
    nb, _, b, bm = dipcool(OFF)
    mo = b['sumObsCoolGrudge'] / b['grudgeRows'] if b['grudgeRows'] > 0 else float('nan')
    ma = a['sumObsCoolGrudge'] / a['grudgeRows'] if a['grudgeRows'] > 0 else float('nan')
    r3 = mo / ma if ma > 0 else float('inf')
    ok3 = r3 > G3_NULL
    print(f'G3 大きさ 実の冷めの平均 旗 0 {mo:.6f}（種 {nb}・行 {int(b["grudgeRows"])}）÷ 腕 {ma:.6f} = {r3:.6f}（線 >{G3_NULL}）→ {"緑" if ok3 else "赤"}')
    g1o = b['obsHyenaFreshRows'] / b['freshRows'] if b['freshRows'] > 0 else float('nan')
    print(f'記録 旗 0 の G1 の値={g1o:.6f}（{int(b["obsHyenaFreshRows"])}/{int(b["freshRows"])}）・旗 0 の G2 の値={bm["maxAbsDiffNewGrudge"]:.6g}')
if BASE != '-':
    ns, st, pe = warband(ARM)
    r4 = st / pe if pe > 0 else float('nan')
    lo, hi = G4_BASE * (1 - G4_TOL), G4_BASE * (1 + G4_TOL)
    ok4 = lo <= r4 <= hi
    print(f'G4 壊さない 開戦÷講和={r4:.6f}（{st}/{pe}・種 {ns}・幅 {lo:.6f}〜{hi:.6f}）→ {"緑" if ok4 else "赤"}')
