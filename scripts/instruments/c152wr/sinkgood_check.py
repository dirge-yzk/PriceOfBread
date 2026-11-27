# (1421) sinkgood_s の検算の台本（反証役 v の中 4 の 4）：旧の生（m1418 の木＝keep_1418/base_ins）と腕の表を行ごとに突き合わせる。
# 呼び方：python sinkgood_check.py <旧の表> <腕の表>。0＝宣言どおり（宣言＝行の絞りと割合の式だけが違う）。
# 検算（TOL は結果の前に固定・印字の有効 10 桁より広い 1e-9）：
#  (a) 見出しが同じ。
#  (b) 腕の行は全て旧の行と同じ鍵（quarter・nation・region・good・recipe）で見つかり、laborWeightScaled・fraction 以外の列が文字列で同じ。
#  (c) laborWeightScaled は旧と同じか 0（倒産した装置の重み 0）。0 に変わった行は倒産・眠りの外の理由で起きない＝件数を書く。
#  (d) fraction は [0,1]（TOL の幅）。重みが 0 の行は割合も 0 か、分母 0 の群の 1/件数（0 でない時は 1/整数）。
#  (e) 旧に有って腕に無い行（絞りで落ちた行）の数を書く（記録）。
import sys
TOL = 1e-9
def rows(p):
    with open(p, encoding='utf-8') as f: return [l.rstrip('\n').split('\t') for l in f]
a, b = rows(sys.argv[1]), rows(sys.argv[2])
if a[0] != b[0]: print('見出しが違う'); sys.exit(1)
h = a[0]; ix = {n: i for i, n in enumerate(h)}
key = lambda r: (r[ix['quarter']], r[ix['nation']], r[ix['region']], r[ix['good']], r[ix['recipe']])
skip = {ix['laborWeightScaled'], ix['fraction']}
old = {}
for r in a[1:]: old.setdefault(key(r), []).append(r)
bad = 0; zeroed = 0; same = 0
used = {}
for r in b[1:]:
    cands = [o for o in old.get(key(r), []) if all(o[i] == r[i] for i in range(len(h)) if i not in skip)]
    if not cands: bad += 1; print('旧に無い行', key(r)); continue
    o = cands[0]; used[id(o)] = 1
    wo, wn = float(o[ix['laborWeightScaled']]), float(r[ix['laborWeightScaled']])
    fn = float(r[ix['fraction']])
    if wn == wo: same += 1
    elif wn == 0.0: zeroed += 1
    else: bad += 1; print('重みが旧とも 0 とも違う', key(r), wo, wn)
    if not (-TOL <= fn <= 1 + TOL): bad += 1; print('割合が [0,1] の外', key(r), fn)
    if wn == 0.0 and fn > TOL:
        k = 1.0 / fn
        if abs(k - round(k)) > 1e-6: bad += 1; print('重み 0 の行の割合が 1/件数でない', key(r), fn)
dropped = sum(1 for k, v in old.items() for o in v if id(o) not in used)
print(f'sinkgood_s 検算 {"OK" if bad == 0 else "NG"} 腕の行 {len(b)-1}・重み同じ {same}・重み 0 へ {zeroed}・旧から落ちた行 {dropped}・不一致 {bad}')
sys.exit(1 if bad else 0)
