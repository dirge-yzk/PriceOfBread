# (1087) 合流：アブレーションの腕の生を線の腕の生と表ごとに突き合わせる（読むだけ・予言 [3]）。
#   python -X utf8 -I abl1087.py <腕の ins> <線の ins> [揺れの表の頭,...]
#   共通の表だけを比べる。バイト一致でなければ：flow_s は sourceFile・sourceLine を除いた行の多重集合（ident_check.py の FLOW_LOC と同じ規則・tr が Simulation.cs の行を動かす＝★(899) の既知の揺れ）、
#   揺れの表（burscan_s・clock_s）は記録だけ。それ以外の違いは〈違い〉と数える。
import sys, os, hashlib, collections, csv
csv.field_size_limit(1 << 30)
A, B = sys.argv[1], sys.argv[2]
noise = [x for x in (sys.argv[3].split(',') if len(sys.argv) > 3 else 'burscan_s,clock_s'.split(',')) if x]
fa = {f for f in os.listdir(A) if f.endswith('.tsv')}; fb = {f for f in os.listdir(B) if f.endswith('.tsv')}
common = sorted(fa & fb)
def sha(p):
    h = hashlib.sha1()
    with open(p, 'rb') as f:
        for ch in iter(lambda: f.read(1 << 22), b''): h.update(ch)
    return h.hexdigest()
def flowms(p):
    c = collections.Counter()
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        drop = {h.index(x) for x in ('sourceFile', 'sourceLine') if x in h}
        for row in r: c[tuple(v for i, v in enumerate(row) if i not in drop)] += 1
    return c
same = loc = nz = diff = 0; bad = []
for f in common:
    if sha(os.path.join(A, f)) == sha(os.path.join(B, f)): same += 1; continue
    if any(f.startswith(n) for n in noise): nz += 1; continue
    if f.startswith('flow_s') and flowms(os.path.join(A, f)) == flowms(os.path.join(B, f)): loc += 1; continue
    diff += 1; bad.append(f)
print(f'共通の表 {len(common)}（腕だけ {len(fa - fb)}・線だけ {len(fb - fa)}）＝バイト一致 {same}・flow_s の呼び出し位置だけ {loc}・揺れの表 {nz}・違い {diff}')
for f in bad[:20]: print('  違い', f)
print('判定: ' + ('一致（共通の表）' if diff == 0 else '不一致'))
