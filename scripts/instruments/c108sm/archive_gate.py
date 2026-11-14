# (1120) c108sm の門：書庫＋残り＝元のバイト・書庫の並び・規則・短い README の各行（全文か参照）。使い方：archive_gate.py <移す前の 一覧> <移す前の 書庫> [木]
import re, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import archive_move as am
import rowkey as RK
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
# 4 短い README（(1140) 札＝★(NNN) か表の第 1 欄の名だけ・行の番号は置かない）
cur = open(tree + '/docs/README.md', 'rb').read().decode('utf-8').replace(chr(13), '').split(chr(10))
curs = set(re.sub(r'^[-*]\s+', '', x.strip()) for x in cur)
sh = open(tree + '/docs/README_線の担当.md', 'rb').read().decode('utf-8')
def col1(x):
    return re.sub(r'[*★⚠`~]', '', [y.strip() for y in x.strip().strip('|').split('|')][0]).strip() if x.lstrip().startswith('|') else ''
def label_exists(lab):
    m = re.match(r'^★\((\d+)\)$', lab)
    if m: return any('★(' + m.group(1) + ')' in x for x in cur)
    m = re.match(r'^表「(.*)」$', lab)
    if m: return RK.count(cur, m.group(1)) == 1   # (1146) ちょうど 1 行（旧は startswith の any＝空の札でも、別の表の行にも通った）
    return bool(re.match(r'^§', lab))
n = 0; sec = ''; refs_in = {}; fulls = []
for line in sh.split(chr(10)):
    if line.startswith('## '): sec = line
    m = re.match(r'^- (.*) 〔([^〕]*)〕$', line)
    r = re.match(r'^- → README (.*)$', line)
    if m:
        n += 1; fulls.append(m.group(1))
        if m.group(1) not in curs: bad.append('短い README の行が元と違う: ' + m.group(1)[:40])
        if re.match(r'^README L\d+$', m.group(2)): bad.append('札が行の番号: ' + m.group(2))
        elif not label_exists(m.group(2)): bad.append('札が README に無い: ' + m.group(2))
    elif r:
        n += 1; refs_in[sec] = refs_in.get(sec, 0) + 1
        if re.match(r'^L\d+', r.group(1)) or not label_exists(r.group(1)): bad.append('参照の札が README に無い／行の番号: ' + r.group(1)[:30])
    elif line.startswith('- '): bad.append('短い README の行が全文でも参照でもない: ' + line[:40])
if len(sh) > 20000: bad.append('短い README が 2 万字超 %d' % len(sh))
if n < 100: bad.append('短い README の行が少ない %d' % n)
# 全文で要る物（(1140) 旧は何もしない検査だった）＝止める線の改めの ★(711)・★(743)・腕の節約・計算量の予算の節は参照だけの行を持たない
for must in ('★(711)：', '★(743)：'):
    if not any(f.startswith(must) for f in fulls): bad.append('全文で要る行が無い: ' + must)
for s_, c_ in refs_in.items():
    if '腕の節約' in s_ or '計算量の予算' in s_: bad.append('全文で要る節に参照の行: ' + s_)
print('短い README', len(sh), '字', n, '行 / 書庫', len(A1), '行 / 一覧', len(R1), '行')
print('OK' if not bad else 'NG ' + str(len(bad))); [print(' ', b) for b in bad[:10]]
sys.exit(1 if bad else 0)
