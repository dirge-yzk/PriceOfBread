# (835) c67id：ident_check.py の cearn_s の経路の列の規則（cearn_same_but_location）の道具の門（両側）。使い捨ての木と実データの両方で、新しい規則と旧の規則（バイト比べ）を並べる。
#   使い方：python -I -X utf8 cearn_rule_test.py <リポジトリ> <作業フォルダ> [実データの基準の ins=keep_826/base_ins] [実データの腕の ins=keep_832/base_ins] [基準のコミット=cbc11c34] [腕のコミット=2b2fb3f6]
#   出力：各ケースの 期待 と 実際。1 つでも期待と違えば最後に  GATEFAIL  を出して終了コード 1。全部期待どおりなら  GATEOK。
import sys, os, shutil, stat, subprocess, importlib.util, csv, glob

repo = sys.argv[1]
work = sys.argv[2]
real_base = sys.argv[3] if len(sys.argv) > 3 else 'D:/PoB_work/keep_826/base_ins'
real_arm = sys.argv[4] if len(sys.argv) > 4 else 'D:/PoB_work/keep_832/base_ins'
c0 = sys.argv[5] if len(sys.argv) > 5 else 'cbc11c34'
c1 = sys.argv[6] if len(sys.argv) > 6 else '2b2fb3f6'

spec = importlib.util.spec_from_file_location('ident_check', os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'ident', 'ident_check.py'))
ic = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ic)
sys.stdout.reconfigure(encoding='utf-8')

FAIL = []
TAB = chr(9)
NL = chr(10)


def rmtree(d):   # .git の読み取り専用のファイルも消す
    def onerr(f, p, e):
        os.chmod(p, stat.S_IWRITE)
        f(p)
    shutil.rmtree(d, onerror=onerr) if os.path.exists(d) else None


def check(name, expect, got, note=''):
    ok = expect == got
    if not ok:
        FAIL.append(name)
    print(f"{'○' if ok else '✗'} {name}：期待 {expect}・実際 {got} {note}")


def git(d, *a):
    subprocess.run(['git', '-C', d] + list(a), check=True, capture_output=True)


def write(p, text):
    os.makedirs(os.path.dirname(p), exist_ok=True)
    open(p, 'wb').write(text.encode('utf-8'))


def tsv(p, rows):
    write(p, NL.join(TAB.join(r) for r in rows) + NL)


def reset(**kw):
    ic.CEARN.update(root=None, base=None, tree=None, arm_commit=None, maps={}, paths=None)
    ic.CEARN.update(**kw)


def new_rule(pa, pb):
    return ic.cearn_same_but_location(pa, pb)[0]


def old_rule_mismatch(pa, pb):   # 旧の規則＝バイトで比べて、違う列があれば不一致（cols_diff の real が空でない）
    kn, real, extra = ic.cols_diff(pa, pb, {})
    return bool(real) or bool(extra)


# ---------------- 合成：使い捨ての木（行を足しただけ） ----------------
G = os.path.join(work, 'g_cearn').replace(chr(92), '/')
rmtree(G)
os.makedirs(G)
git(G, 'init', '-q')
git(G, 'config', 'user.email', 'x@x')
git(G, 'config', 'user.name', 'x')
base_lines = [f'// base {i}' for i in range(1, 13)]
base_lines[4] = 'double a = Move(x);   // 5 行目：呼び出し A'
base_lines[8] = 'double b = Take(y);   // 9 行目：呼び出し B'
rel = 'PriceOfBread.SimCore/Foo.cs'
write(os.path.join(G, rel), NL.join(base_lines) + NL)
git(G, 'add', '.')
git(G, 'commit', '-q', '-m', 'base')
base = subprocess.run(['git', '-C', G, 'rev-parse', 'HEAD'], capture_output=True, text=True).stdout.strip()

A1 = os.path.join(work, 'g_arm1').replace(chr(92), '/')   # 頭に 3 行足しただけ
rmtree(A1)
write(os.path.join(A1, rel), NL.join(['// added 1', '// added 2', '// added 3'] + base_lines) + NL)
A2 = os.path.join(work, 'g_arm2').replace(chr(92), '/')   # 5 行目を書き換え（呼び出し A の行が消えた）＋頭に 3 行
rmtree(A2)
bl2 = list(base_lines)
bl2[4] = 'double a2 = Other(x);   // 書き換えた行'
write(os.path.join(A2, rel), NL.join(['// added 1', '// added 2', '// added 3'] + bl2) + NL)

H = ['quarter', 'nation', 'outTop1Route', 'outTop2Route', 'inTop1Route', 'other']
base_rows = [H,
             ['0', 'X', 'Foo.cs:5:carryOut:P', 'Foo.cs:9:take:T', 'Foo.cs:5:carryIn:P', '1.5'],
             ['1', 'X', 'Foo.cs:9:take:T', 'Foo.cs:5:carryOut:P', 'Foo.cs:9:take:T', '2.5']]
arm_rows = [H,
            ['0', 'X', 'Foo.cs:8:carryOut:P', 'Foo.cs:12:take:T', 'Foo.cs:8:carryIn:P', '1.5'],
            ['1', 'X', 'Foo.cs:12:take:T', 'Foo.cs:8:carryOut:P', 'Foo.cs:12:take:T', '2.5']]
pa = os.path.join(work, 'g_tsv', 'base.tsv')
pb = os.path.join(work, 'g_tsv', 'arm.tsv')
tsv(pa, base_rows)
tsv(pb, arm_rows)

reset(root=G, base=base, tree=A1)
check('G1 行を足しただけ（合成）：新しい規則', True, new_rule(pa, pb))
check('G1 行を足しただけ（合成）：旧の規則（バイト比べ）は不一致', True, old_rule_mismatch(pa, pb))


def variant(name, mutate, expect, tree=A1):
    rows = [list(r) for r in arm_rows]
    mutate(rows)
    p = os.path.join(work, 'g_tsv', name + '.tsv')
    tsv(p, rows)
    reset(root=G, base=base, tree=tree)
    check(name, expect, new_rule(pa, p))


def set_cell(r, c, v):
    def f(rows):
        rows[r][H.index(c)] = v
    return f


variant('G2 経路を別の呼び出しの行へ替える（outTop1Route の 8→12）', set_cell(1, 'outTop1Route', 'Foo.cs:12:carryOut:P'), False)
variant('G2b 経路を別の動詞へ替える（carryOut→carryIn・行は正しい）', set_cell(1, 'outTop1Route', 'Foo.cs:8:carryIn:P'), False)
variant('G3a 写像に無い行（足した行の 2）へ移る', set_cell(1, 'outTop2Route', 'Foo.cs:2:take:T'), False)
variant('G4 経路でない列を 1 バイト変える（other 1.5→1.6）', set_cell(1, 'other', '1.6'), False)
variant('G4b 経路でない列でないが数の列（quarter 0→2）', set_cell(1, 'quarter', '2'), False)
variant('G6 同じ字のまま行が動いたはずの経路（arm が 5 のまま）', set_cell(1, 'inTop1Route', 'Foo.cs:5:carryIn:P'), False)
# G3b：基準の経路が指す行（5）を腕の木が書き換えた＝写像に無い
reset(root=G, base=base, tree=A2)
check('G3b 基準の経路が指す行を腕の木が書き換えた（写像に無い）：新しい規則', False, new_rule(pa, pb))
reset(root=G, base=None, tree=A1)
check('G5 BASE_COMMIT 無し：規則を使わない（旧のバイト比べのまま）', False, new_rule(pa, pb))
reset(root=G, base=base, tree=A1)
check('G7 経路の列が無い表：規則を使わない', False, new_rule(os.path.join(work, 'g_tsv', 'base.tsv'), os.path.join(work, 'g_tsv', 'nocol.tsv')) if (tsv(os.path.join(work, 'g_tsv', 'nocol.tsv'), [['quarter', 'other'], ['0', '1']]) or True) else None)

# ---------------- 実データ：行が本当に動いた 2 つの木（(828) の cbc11c34 と 2b2fb3f6） ----------------
files = sorted(glob.glob(os.path.join(real_base, 'cearn_s*.tsv')))
if not files or not os.path.isdir(real_arm):
    print('△ 実データの基準か腕の生が無い＝実データの門は未了')
    FAIL.append('REAL_MISSING')
else:
    reset(root=repo, base=c0, tree=None, arm_commit=c1)
    res = []
    for f in files:
        g = os.path.join(real_arm, os.path.basename(f))
        res.append((os.path.basename(f), new_rule(f, g), old_rule_mismatch(f, g)))
    check(f'G1b 実データ {len(res)} 本：新しい規則が全て一致', True, all(r[1] for r in res))
    check(f'G1b 実データ {len(res)} 本：旧の規則（バイト比べ）が全て不一致', True, all(r[2] for r in res))
    f0 = files[0]
    g0 = os.path.join(real_arm, os.path.basename(f0))
    with open(g0, encoding='utf-8', newline='') as fh:
        rows = list(csv.reader(fh, delimiter=TAB))
    h = rows[0]
    ci = h.index('outTop1Route')
    vals = sorted({r[ci] for r in rows[1:] if ':' in r[ci]})
    assert rows[1][ci].count(':') >= 2, rows[1][ci]
    # 実データの突然変異：先頭の行の outTop1Route を、別の値（別の呼び出し）へ
    mrows = [list(r) for r in rows]
    cur = mrows[1][ci]
    other = next(v for v in vals if v != cur)
    mrows[1][ci] = other
    pm = os.path.join(work, 'g_tsv', 'real_mut_other.tsv')
    tsv(pm, mrows)
    check('G2r 実データ：経路を別の呼び出しの行へ替える', False, new_rule(f0, pm), f'（{cur} → {other}）')
    # 実データの突然変異：同じファイルの行番号を 1 増やす（写像の外か別の行）
    mrows = [list(r) for r in rows]
    fn, ln = cur.split(':')[0], int(cur.split(':')[1])
    mrows[1][ci] = cur.replace(f'{fn}:{ln}', f'{fn}:{ln + 1}', 1)
    pm2 = os.path.join(work, 'g_tsv', 'real_mut_plus1.tsv')
    tsv(pm2, mrows)
    check('G3r 実データ：行番号を 1 ずらす', False, new_rule(f0, pm2), f'（{cur} → {mrows[1][ci]}）')
    # 実データの突然変異：経路でない列
    mrows = [list(r) for r in rows]
    ki = h.index('savings')
    mrows[1][ki] = mrows[1][ki] + '1'
    pm3 = os.path.join(work, 'g_tsv', 'real_mut_col.tsv')
    tsv(pm3, mrows)
    check('G4r 実データ：経路でない列（savings）を 1 字変える', False, new_rule(f0, pm3))

print('GATEOK' if not FAIL else 'GATEFAIL ' + ','.join(FAIL))
sys.exit(0 if not FAIL else 1)
