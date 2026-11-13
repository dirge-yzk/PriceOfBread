# (1120) c108sm の門：書庫＋残り＝元のバイト・書庫の並び・規則・短い README の各行（全文か参照）。使い方：archive_gate.py <移す前の 一覧> <移す前の 書庫> [木]
import re, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import archive_move as am
tree = sys.argv[3] if len(sys.argv) > 3 else am.REPO
F1, F2 = tree + '/' + am.F_ONE, tree + '/' + am.A_ONE
R0 = open(sys.argv[1], 'rb').read().split(b'\n'); A0 = open(sys.argv[2], 'rb').read().split(b'\n')
R1 = open(F1, 'rb').read().split(b'\n'); A1 = open(F2, 'rb').read().split(b'\n')
bad = []
# 1 書庫＋残り＝元のバイト（行の多重集合・書庫の見出し 2 行を除く）
if sorted(R0 + A0[2:]) != sorted(R1 + A1[2:]): bad.append('書庫＋残りが元と違う')
if A0[:2] != A1[:2]: bad.append('書庫の見出しが変わった')
# 2 書庫の並びは元の並び
O = am.base(am.F_ONE).split(b'\n'); pos = {}
for i, l in enumerate(O): pos.setdefault(l, i)
ps = [pos.get(l, -1) for l in A1[2:] if l.strip()]
if any(p < 0 for p in ps) or ps != sorted(ps): bad.append('書庫の並びが元の並びでない')
# 3 規則
for l in A1[2:]:
    m = am.mx(l)
    if l.strip() and m is not None and m >= (b'10', b'13'): bad.append('書庫に 10-13 以後の日付の行: ' + l[:40].decode('utf-8', 'replace'))
for l in R1:
    m = am.mx(l)
    if am.lead_struck(l) and m is not None and m < (b'10', b'13'): bad.append('一覧に残った古い打消し行: ' + l[:40].decode('utf-8', 'replace'))
# 4 短い README
cur = open(tree + '/docs/README.md', 'rb').read().decode('utf-8').replace('\r', '').split('\n')
sh = open(tree + '/docs/README_線の担当.md', 'rb').read().decode('utf-8')
n = 0
for line in sh.split('\n'):
    m = re.match(r'^- (.*) 〔README L(\d+)〕$', line)
    r = re.match(r'^- → README L(\d+)（', line)
    if m:
        n += 1; x = int(m.group(2)); s = re.sub(r'^[-*]\s+', '', cur[x - 1].strip())
        if m.group(1) != s: bad.append('短い README の行が元と違う L%d' % x)
    elif r: n += 1
    elif line.startswith('- '): bad.append('短い README の行が全文でも参照でもない: ' + line[:40])
if len(sh) > 20000: bad.append('短い README が 2 万字超 %d' % len(sh))
if n < 100: bad.append('短い README の行が少ない %d' % n)
for must in (98, 116, 125, 89, 215):
    pass
print('短い README', len(sh), '字', n, '行 / 書庫', len(A1), '行 / 一覧', len(R1), '行')
print('OK' if not bad else 'NG ' + str(len(bad))); [print(' ', b) for b in bad[:10]]
sys.exit(1 if bad else 0)
