# (906) c78gz 自己試験：門 (a)(b)(c)(d)。使い方: python -X utf8 -I selftest906.py [--dir <c78gz の写し>] [--cache <旧の出力の置き場>] [--gates abcd]
#   期待は事前登録 measurements/m906_20261106_prereg_gz.txt と盤面の意図から書く（台本の出力から写していない）。
import os, subprocess, sys, re, concurrent.futures as cf
A = sys.argv[1:]
def opt(n, d):
    return A[A.index(n) + 1] if n in A else d
HERE = os.path.dirname(os.path.abspath(__file__))
D = opt('--dir', HERE)
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
S = os.path.join(REPO, 'scripts', 'instruments')
CACHE = opt('--cache', 'D:/PoB_work/c78gz_work/old')
GATES = opt('--gates', 'abcd')
os.makedirs(CACHE, exist_ok=True)
W = 'D:/PoB_work/'
ARMS = [('M905', 'keep_905/base_ins'), ('S11_905', 'keep_905/a11'), ('N1_905', 'keep_905/n1'), ('N2_905', 'keep_905/n2'),
        ('M873', 'keep_873/base_ins'), ('S11_873', 'keep_873/a11'), ('N1_873', 'keep_873/n1'), ('N2_873', 'keep_873/n2'),
        ('c77ceB', 'keep_905_rej/c77ceB_10'), ('c77ceS', 'keep_905_rej/c77ceS_10_s11'), ('c77cwB', 'keep_905_rej/c77cwB_10'), ('c77cwS', 'keep_905_rej/c77cwS_10_s11')]
def run(script, *args):
    p = subprocess.run([sys.executable, '-X', 'utf8', '-I', script, *args], capture_output=True, encoding='utf-8')
    return p.stdout
def cached(key, script, *args):
    f = os.path.join(CACHE, key + '.txt')
    if os.path.exists(f): return open(f, encoding='utf-8', newline='').read()
    o = run(script, *args); open(f, 'w', encoding='utf-8', newline='').write(o); return o
OLDJ = os.path.join(S, 'r833', 'gdp_jump833.py'); OLDZ = os.path.join(S, 'm808', 'gdp_zero10.py')
NEWJ = os.path.join(D, 'gdp_jump906.py'); NEWZ = os.path.join(D, 'gdp_zero906.py')
res = []
def ok(name, cond, note=''):
    res.append((name, bool(cond))); print(('緑' if cond else '赤'), name, note)
# stop_table_905.tsv の旧の件数（列：arm・gdp100_833 の 建国/痩せ/それ以外・gdpZero4q）
TAB = {}
for l in open(os.path.join(REPO, 'measurements', 'm905_20261106', 'stop_table_905.tsv'), encoding='utf-8').read().split('\n')[1:]:
    c = l.split('\t')
    if len(c) > 9: TAB[c[0]] = (c[3], c[5])
if 'a' in GATES:
    def job(a):
        n, d = a
        oj = cached(f'jump_{n}', OLDJ, '--rows', f'{n}={W}{d}'); oz = cached(f'zero_{n}', OLDZ, f'{n}={W}{d}')
        return n, oj, run(NEWJ, '--rows', f'{n}={W}{d}'), oz, run(NEWZ, f'{n}={W}{d}')
    with cf.ThreadPoolExecutor(6) as ex: R = list(ex.map(job, ARMS))
    for n, oj, nj, oz, nz in R:
        ok(f'(a) {n} gdp_jump 既定がバイト一致', oj == nj and oj != '')
        ok(f'(a) {n} gdp_zero 既定がバイト一致', oz == nz and oz != '')
        m = re.search(r'建国の直後〔≦4 四半期〕(\d+)・分母の痩せ〔[^〕]*〕(\d+)・それ以外 (\d+)）', nj)
        trip = f'{m.group(1)}/{m.group(2)}/{m.group(3)}' if m else '?'
        zr = re.search(r'区間 (\d+)・', nz)
        if n in TAB: ok(f'(a) {n} stop_table_905 の再現（833 の 3 区分 {TAB[n][0]}・gdpZero4q {TAB[n][1]}）', trip == TAB[n][0] and zr and zr.group(1) == TAB[n][1], f'実 {trip}/{zr.group(1) if zr else "?"}')
FIXD = os.path.join(HERE, 'fixture')
if 'b' in GATES:
    fz_old = run(OLDZ, f'FIX={FIXD}'); fz_new = run(NEWZ, '--founding', f'FIX={FIXD}')
    oldn = int(re.search(r'区間 (\d+)・', fz_old).group(1))
    ok('(b) 旧 gdp_zero10 は a建国0が7行 を数えない（盤面で旧の区間は j旧でも見える0 の 1 件だけ）', oldn == 1 and 'a建国0が7行' not in fz_old)
    ok('(b) --founding は a建国0が7行 を 7 四半期で数える', re.search(r'a建国0が7行 \d+〜\d+ 7四半期', fz_new))
    ok('(b) --founding は j旧でも見える0 を 12 四半期で数える', 'j旧でも見える0 1783〜1785 12四半期' in fz_new)
    fj = run(NEWJ, '--founding', '--rows', f'FIX={FIXD}')
    EXP = {'b建国後0からの跳び': '建国の後の0の続き', 'c建国の直後': '建国の直後', 'd窓の外': 'それ以外', 'e三行だけ': 'それ以外', 'f痩せ': '分母の痩せ',
           'g0の続きと痩せ': '建国の後の0の続き', 'i建国2から跳び': '建国の直後', 'k窓の端': '建国の後の0の続き', 'l窓の外の端': 'それ以外', 'm窓7で落ちる': '建国の後の0の続き', 'n窓9で入る': 'それ以外'}
    got = {}
    for l in fj.split('\n'):
        m = re.match(r"\s+(\S+)\s+\(\d+, '([^']*)'", l)
        if m: got[m.group(2)] = m.group(1)
    bad = [f'{k}:{v}→{got.get(k, "無し")}' for k, v in EXP.items() if got.get(k) != v]
    ok('(b) 盤面の区分（優先と窓の端・初出の四半期）', not bad, ' '.join(bad))
    ok('(b) 旧の既定の盤面の出力が 833 と一致', run(OLDJ, '--rows', f'FIX={FIXD}') == run(NEWJ, '--rows', f'FIX={FIXD}'))
if 'c' in GATES or 'd' in GATES:
    AR = dict(ARMS)
    def jj(n): return n, run(NEWJ, '--founding', '--rows', f'{n}={W}{AR[n]}')
    with cf.ThreadPoolExecutor(6) as ex: JR = dict(ex.map(jj, [n for n, _ in ARMS]))
    def inzr(n, nm, yr): return any(l.startswith('   建国の後の0の続き') and f"'{nm}', {yr}," in l for l in JR[n].split('\n'))
    def indirect(n, nm, yr): return any(l.startswith('   建国の直後') and f"'{nm}', {yr}," in l for l in JR[n].split('\n'))
    def nzr(n): return sum(1 for l in JR[n].split('\n') if l.startswith('   建国の後の0の続き'))
    if 'c' in GATES:
        ok('(c) P1 N1 種 3 ポンディシェリ王国 1806（7 四半期・212.4 倍）が入る', inzr('N1_905', 'ポンディシェリ王国', 1806) and "'ポンディシェリ王国', 1806, 93, 7, 212.4" in JR['N1_905'])
        ok('(c) P2 N2 種 5 共和制ネーデルラント 1878 が入る', inzr('N2_905', '共和制ネーデルラント', 1878))
        ok('(c) P3 N2 種 7 共和制ポルトガル 1853〜1854 が入る【事前登録の予言】', inzr('N2_905', '共和制ポルトガル', 1853) or inzr('N2_905', '共和制ポルトガル', 1854))
        ok('(c) P4 c77ce の S11 の共和制モロッコが入る【事前登録の予言】', any(l.startswith('   建国の後の0の続き') and 'モロッコ' in l for l in JR['c77ceS'].split('\n')))
        ok('(c) P5 c77cw の S11 の共和制両シチリア 1902（351.1 倍）が入る', inzr('c77cwS', '共和制両シチリア', 1902) and '351.1' in JR['c77cwS'])
        ok('(c) P6 M の共和制ポルトガル 1879 は建国の直後に残る', indirect('M905', '共和制ポルトガル', 1879) and nzr('M905') == 0)
        others = sum(nzr(n) for n, _ in ARMS) - 3
        ok('(c) P7 P1・P2・P5 の 3 件以外で新しい区分に入る件が 12 本のどれにも無い', others == 0, f'その他 {others}')
    if 'd' in GATES:
        six = ['M905', 'N1_905', 'N2_905', 'M873', 'N1_873', 'N2_873']
        cnt = {n: nzr(n) for n in six}; print('   (d) 6 本の新しい区分の件数', cnt)
        hit = sum(1 for n in six if cnt[n] > 0)
        miss = sum(1 for n in six if cnt[n] > 0 and all(cnt[o] == 0 for o in six if o != n))   # 腕に入る件があり、残りの 5 本のどれにも無い
        print(f'   (d) 一つ抜き：件が出る本数 {hit}/6・残りの 5 本のどれにも無い（空振り）{miss}/6 率 {miss/6:.2f}')
        ok('(d) 一つ抜きの空振りの率を出した（1/3 以上なら記録へ下げる根拠・採否の門にはしない）', True, f'率 {miss/6:.2f}')
nbad = [n for n, c in res if not c]
print('合計', len(res), '赤', len(nbad)); print('赤の一覧', nbad)
