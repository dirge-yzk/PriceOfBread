# (1075) (1068) の選び方 P＋R を〈今の木 b0edc8963〉へ写し、本数でなく trx の秒で数える。読むだけ。
# 使い方: python -X utf8 -I q3_secs.py <リポジトリの根> <今の木の展開> <class_secs.tsv> <suite1069.sh> <出力>
# 方法（AI の案・確証なし）：各件の〈触ったファイル〉は (1068) と同じ差分の組から取り、〈触った型の名〉はその件の木の字面から取る（replay_select.py と同じ正規表現）。
#   それを今の木の試験のファイルに当てる（D＝型の名を書く・T＝触った試験のファイル〔今の木にある物〕・B＝今日の束・S＝走査の番人・R＝データなら全部）。
# 壁の見積り（名目）：並列の段＝max(並列のクラスの秒の和÷16, 選んだ並列の試験 1 本の最大)・直列の段＝直列の集まりの秒の和（実測の 1 本の糸）。
#   検算：suite1069.sh の guards の選び（実測 13 分 9 秒＝789 秒）と全体（実測 1083.5 秒）に同じ見積りを当てて並べる。
# 限界：その時点の木と今の木はクラスが違う＝〈今その型を触ったら〉の写し。秒は 1 回の trx（16 並列の混み合いを含む）。
import sys, os, re, subprocess, collections
REPO, TREE, CS, SUITE, OUT = sys.argv[1:6]
sys.argv = ['x', REPO, OUT]
src = open(os.path.join(REPO, 'scripts/instruments/r1068/replay_select.py'), 'rb').read().decode('utf-8')
src = src[:src.index("os.makedirs(RW, exist_ok=True)")]   # 定義だけを使う（本体の走りは使わない）
ns = {}
exec(compile(src, 'replay_select.py', 'exec'), ns)
git, CASES, TOP, COMMENT, WORD, READS, SRCPAT, DECL, TODAY, nmethods = (ns[k] for k in
    ('git', 'CASES', 'TOP', 'COMMENT', 'WORD', 'READS', 'SRCPAT', 'DECL', 'TODAY', 'nmethods'))
touched_pairs = ns['touched_pairs']

# trx の秒
cls = {}
for l in list(open(CS, encoding='utf-8'))[1:]:
    c, f, coll, world, n, secs, a, b = l.rstrip('\n').split('\t')
    cls[c] = dict(file=f or None, coll=coll, n=int(n), s=float(secs))
for c, v in cls.items():   # 入れ子のクラス（X+Y）は外のクラスのファイルへ
    if v['file'] is None and '+' in c:
        v['file'] = cls.get(c.split('+')[0], {}).get('file')
TOT_S = sum(v['s'] for v in cls.values()); TOT_N = sum(v['n'] for v in cls.values())
# 試験 1 本の最大（並列の段の下限に使う）＝trx から取り直さず、クラスの秒の上限で近似しない＝別に読む
import xml.etree.ElementTree as ET
NSX = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
r = ET.parse(os.path.join(REPO, 'measurements/m1069_20261112/trx/nonfull.trx')).getroot()
cid = {u.get('id'): u.find(NSX + 'TestMethod').get('className').split('.')[-1] for u in r.iter(NSX + 'UnitTest')}
def dur(s):
    h, m, x = s.split(':'); return int(h) * 3600 + int(m) * 60 + float(x)
maxone = collections.defaultdict(float)
for u in r.iter(NSX + 'UnitTestResult'):
    c = cid[u.get('testId')]; maxone[c] = max(maxone[c], dur(u.get('duration', '0:0:0')))

def wall(sel):
    par = [c for c in sel if cls[c]['coll'] != 'Instruments']
    ser = [c for c in sel if cls[c]['coll'] == 'Instruments']
    ps = sum(cls[c]['s'] for c in par); ss = sum(cls[c]['s'] for c in ser)
    pw = max(ps / 16, max([maxone[c] for c in par], default=0))
    return pw + ss, pw, ss, ps / 16 + ss   # 4 番目＝下限（混み合いの無い場合の並列の段＝和÷16・1 本の最大を見ない）

# 今の木の試験のファイル
tests = {}
TD = os.path.join(TREE, 'PriceOfBread.Tests')
for f in os.listdir(TD):
    if f.endswith('.cs'):
        raw = open(os.path.join(TD, f), 'rb').read().decode('utf-8-sig', 'replace')
        tests[f] = (raw, COMMENT.sub('', raw))
file_cls = collections.defaultdict(list)
for c, v in cls.items():
    if v['file']: file_cls[v['file']].append(c)
S_today = {f for f, (raw, t) in tests.items() if READS.search(t) and SRCPAT.search(raw)}
B_today = {f for f, (raw, t) in tests.items() if any(any(k in c for k in TODAY) for c in DECL.findall(t))}
def secs_of(files):
    cs = [c for f in files for c in file_cls.get(f, [])]
    return cs, sum(cls[c]['n'] for c in cs), sum(cls[c]['s'] for c in cs)
# クラスの粒度の変種（dotnet test --filter はクラスの名で選ぶ＝ファイルに同居する別のクラスを連れてこない）：
#   ファイルの字面をトップのクラス宣言ごとの区間に切り、D と S を区間の字面で、B をクラスの名で当てる（クラスの外の補助の字面は落とす）。
CSPAN = {}
for f, (raw, t) in tests.items():
    ms = list(TOP.finditer(t))
    for i, m in enumerate(ms):
        CSPAN[m.group(1)] = (f, t[m.start(): ms[i + 1].start() if i + 1 < len(ms) else len(t)])
def cls_secs(sel):
    return sel, sum(cls[c]['n'] for c in sel), sum(cls[c]['s'] for c in sel)
def pick_cls(tn, touched, data):
    out = []
    for c in cls:
        base = c.split('+')[0]
        f, span = CSPAN.get(base, (cls[c]['file'], ''))
        d = bool(set(WORD.findall(span)) & tn)
        tt = f is not None and ('PriceOfBread.Tests/' + f) in touched
        s = bool(READS.search(span) and SRCPAT.search(span))
        b = any(k in base for k in TODAY)
        if data or d or tt or s or b:
            out.append(c)
    return out

L = []
W_ALL = wall(list(cls))
L.append(f'全体：件 {TOT_N}・秒の和 {TOT_S:.1f}・壁の見積り {W_ALL[0]:.1f}（並列 {W_ALL[1]:.1f}＋直列 {W_ALL[2]:.1f}）・実測 1083.5')
# 検算：guards の選び
sh = open(SUITE, 'rb').read().decode('utf-8')
keys = re.findall(r'FullyQualifiedName~([A-Za-z0-9]+)', sh[sh.index('MODE" = guards'):sh.index('GUARDSDONE')])
gsel = [c for c in cls if any(k in 'PriceOfBread.Tests.' + c.replace('+', '.') for k in keys)]
gw = wall(gsel)
L.append(f'検算 guards（suite1069.sh の束・クラス名の部分一致で近似）：クラス {len(gsel)}・件 {sum(cls[c]["n"] for c in gsel)}（実測 1142）・秒の和 {sum(cls[c]["s"] for c in gsel):.1f}・壁の見積り {gw[0]:.1f}（並列 {gw[1]:.1f}＋直列 {gw[2]:.1f}）・実測 789（13 分 9 秒・別の日の別の木 2a6a6ea1d）')
for nm, fs in (('今日の束 B だけ', B_today), ('走査の番人 S だけ', S_today), ('B∪S', B_today | S_today)):
    cs, n, s = secs_of(fs); w = wall(cs)
    L.append(f'{nm}：ファイル {len(fs)}・件 {n}（{n/TOT_N:.2f}）・秒 {s:.1f}（{s/TOT_S:.2f}）・壁の見積り {w[0]:.1f}（{w[0]/W_ALL[0]:.2f}・並列 {w[1]:.1f}＋直列 {w[2]:.1f}）')
L.append('\n件\t名\t型\tデータ\t(1068) 本数の割合\t今の木の件の割合\t秒の割合\t壁の見積り（秒）\t壁の割合\t壁の下限の割合\t並列+直列\t赤のクラスの秒（今の木）')
shares = []
red_secs = {}
CL = ['\n== クラスの粒度の変種：件\t名\t件の割合\t秒の割合\t壁の見積り\t壁の割合\t壁の下限の割合\t並列+直列\t取りこぼした赤のクラス']
shares2 = []
for case in CASES:
    n, name, spec, tree, fails, caught, logline, kind = case
    touched = set()
    for a, b in touched_pairs(spec):
        out = git('log', '--all', '--no-merges', '--format=', '--name-only', '--grep=' + a[2:]) if a.startswith('G:') else git('diff', '--name-only', a, b)
        touched |= set(x for x in out.split('\n') if x and not x.startswith(('docs/', 'measurements/')))
    tn = set()
    for f in touched:
        if f.endswith('.cs') and not f.startswith('PriceOfBread.Tests/'):
            try:
                t = git('show', f'{tree}:{f}')
            except subprocess.CalledProcessError:
                continue
            tn |= set(TOP.findall(COMMENT.sub('', t)))
    data = any(x.startswith('PriceOfBread.SimCore/') and x.endswith(('.json', '.bin')) for x in touched)
    D = {f for f, (raw, t) in tests.items() if set(WORD.findall(t)) & tn}
    T = {f for f in tests if 'PriceOfBread.Tests/' + f in touched}
    P = set(tests) if data else (D | T | B_today | S_today)
    cs, cn, s = secs_of(P); w = wall(cs)
    rs = sum(cls[c]['s'] for c in fails if c in cls)
    for c in fails:
        if c in cls: red_secs[c] = cls[c]['s']
    old = {l.split('\t')[0]: l.split('\t')[15] for l in open(os.path.join(REPO, 'measurements/m1068_20261112/b_replay.txt'), encoding='utf-8') if l[:1].isdigit()}
    shares.append((n, name, cn / TOT_N, s / TOT_S, w[0] / W_ALL[0], w[3] / W_ALL[3]))
    L.append(f'{n}\t{name}\t{kind}\t{data}\t{old.get(str(n), "?")}\t{cn/TOT_N:.2f}\t{s/TOT_S:.2f}\t{w[0]:.0f}\t{w[0]/W_ALL[0]:.2f}\t{w[3]/W_ALL[3]:.2f}\t{w[1]:.0f}+{w[2]:.0f}\t{rs:.1f}')
    pc = pick_cls(tn, touched, data); _, cn2, s2 = cls_secs(pc); w2 = wall(pc)
    redin = [c for c in fails if c in cls and c not in pc]
    CL.append(f'{n}\t{name}\t{cn2/TOT_N:.2f}\t{s2/TOT_S:.2f}\t{w2[0]:.0f}\t{w2[0]/W_ALL[0]:.2f}\t{w2[3]/W_ALL[3]:.2f}\t{w2[1]:.0f}+{w2[2]:.0f}\t{",".join(redin) or "-"}')
    shares2.append((cn2 / TOT_N, s2 / TOT_S, w2[0] / W_ALL[0], w2[3] / W_ALL[3]))
def med(xs):
    xs = sorted(xs); return xs[len(xs) // 2]
L.append(f'\n中央：件の割合 {med([x[2] for x in shares]):.2f}・秒の割合 {med([x[3] for x in shares]):.2f}・壁の割合 {med([x[4] for x in shares]):.2f}')
L.append(f'壁の下限の割合の中央 {med([x[5] for x in shares]):.2f}・最小 {min(x[5] for x in shares):.2f}（全体の下限 {W_ALL[3]:.1f} 秒）')
L.append(f'壁の割合 0.5 未満の件 {sum(1 for x in shares if x[4] < 0.5)} / {len(shares)}・秒の割合 0.5 未満 {sum(1 for x in shares if x[3] < 0.5)}')
L.append(f'赤になったクラス（延べ 43 の重複を除く・今の木にある物）{len(red_secs)}・秒の和 {sum(red_secs.values()):.1f}（全体の {sum(red_secs.values())/TOT_S:.3f}）')
L.append('  ' + '・'.join(f'{c} {s:.1f}' for c, s in sorted(red_secs.items(), key=lambda kv: -kv[1])))
L += CL
L.append(f'クラスの粒度の中央：件 {med([x[0] for x in shares2]):.2f}・秒 {med([x[1] for x in shares2]):.2f}・壁 {med([x[2] for x in shares2]):.2f}・壁の下限 {med([x[3] for x in shares2]):.2f}・壁の割合 0.8 未満の件 {sum(1 for x in shares2 if x[2] < 0.8)}')
with open(os.path.join(OUT, 'q3_select_secs.txt'), 'w', encoding='utf-8', newline='\n') as fh:
    fh.write('\n'.join(L) + '\n')
print('ok')
