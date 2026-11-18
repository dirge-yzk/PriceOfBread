# (1212) 合流：腕の生の表を基準の生と表ごとに突き合わせる（読むだけ・予言 P3 P5 P6）。
#   python -X utf8 -I cmpraw1212.py <腕の ins> <基準の ins>
#   共通の表（同じファイル名）だけを読む。規則：
#   - バイト一致＝一致。
#   - 既知の揺れ（ident.cfg の KNOWN と同じ 3 つ：burscan_s:seconds・clock_s:worldTotalPerMonth の kind=C の行・dep_s:callerLine）は、その列を除いて一致なら〈揺れ〉。
#   - 列が末尾に増えてよい表（soldiervac_s・nation_s＝ca (1206)）：行の数が同じで、基準の各行が腕の同じ行の頭（基準の行＋タブ）なら〈列の増え〉。
#   - 行が増えてよい表（dep_s＝ca の kind 3 の行）：基準の行（callerLine を除く）が腕の行の順序つき部分列なら〈行の増え〉。
#   - それ以外は〈違い〉（宣言の無い違い）。
import os, sys
A, B = sys.argv[1], sys.argv[2]
GROWN = ('soldiervac_s', 'nation_s'); ROWS = ('dep_s',)
def tab(f): return f.rsplit('_s', 1)[0] + '_s'
def rd(p): return open(p, 'rb').read().split(b'\n')
def drop(lines, col, cond=None):
    h = lines[0].split(b'\t')
    if col not in h: return lines
    i = h.index(col); out = [lines[0]]
    ci = h.index(b'kind') if cond and b'kind' in h else None
    for ln in lines[1:]:
        c = ln.split(b'\t')
        if len(c) > i and (ci is None or (len(c) > ci and c[ci] == b'C')): c[i] = b'*'
        out.append(b'\t'.join(c))
    return out
cnt = {}; bad = []
fa = set(os.listdir(A)); fb = set(os.listdir(B))
for f in sorted(fa & fb):
    t = tab(f); a = open(f'{A}/{f}', 'rb').read(); b = open(f'{B}/{f}', 'rb').read()
    if a == b: k = '一致'
    else:
        la, lb = a.split(b'\n'), b.split(b'\n')
        if t == 'burscan_s': la, lb = drop(la, b'seconds'), drop(lb, b'seconds')
        if t == 'clock_s': la, lb = drop(la, b'worldTotalPerMonth', True), drop(lb, b'worldTotalPerMonth', True)
        if t == 'dep_s': la, lb = drop(la, b'callerLine'), drop(lb, b'callerLine')
        if la == lb: k = '揺れ'
        elif t in GROWN and len(la) == len(lb) and all(x == y or (x.startswith(y + b'\t')) or (x.startswith(y.rstrip(b'\r') + b'\t')) for x, y in zip(la, lb)): k = '列の増え'
        elif t in ROWS:
            it = iter(la); k = '行の増え' if all(any(x == y for x in it) for y in lb) else '違い'
        else: k = '違い'
    cnt[k] = cnt.get(k, 0) + 1
    if k == '違い': bad.append(f)
print(f'腕 {A}')
print(f'基準 {B}')
print(f'共通の表 {len(fa & fb)}・腕だけ {len(fa - fb)}・基準だけ {len(fb - fa)}')
print('内訳 ' + '・'.join(f'{k} {v}' for k, v in sorted(cnt.items())))
print('腕だけの表 ' + ','.join(sorted({tab(f) for f in fa - fb})))
print('基準だけの表（頭） ' + ','.join(sorted({tab(f) for f in fb - fa}))[:400])
print('違い ' + (','.join(bad) if bad else '0'))
print('判定: ' + ('共通の表は一致（宣言の範囲）' if not bad else '違いあり'))
