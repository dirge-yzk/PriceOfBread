# (934) c82it：cearn_same_but_location が宣言の列（--known-extra と KNOWN のうち cearn_s の物）を経路の外のバイト一致から外すことの試験。
#   使い方：python -I -X utf8 cearn_known_test.py <作業フォルダ> [直す前の ident_check.py のパス＝比べ用]
#   盤面は手で組む（cearn_s の小さな 2 表と使い捨ての git の木）。cmp_ins まで通して判定の行（○/✗）を読む。
#   出力：各ケースの 期待 と 実際。1 つでも期待と違えば最後に GATEFAIL・終了コード 1。全部期待どおりなら GATEOK。
#   環境変数 C82IT_EXPECT_OLD=1 を付けると、直す前の道具での予言（(c) が不一致）を期待値にする（事前登録の再現用）。
import sys, os, shutil, stat, subprocess, importlib.util

work = sys.argv[1]
icpath = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'ident', 'ident_check.py')
OLD = os.environ.get('C82IT_EXPECT_OLD') == '1'
spec = importlib.util.spec_from_file_location('ident_check', icpath)
ic = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ic)
sys.stdout.reconfigure(encoding='utf-8')
FAIL = []
TAB = chr(9)
NL = chr(10)


def rmtree(d):
    def onerr(f, p, e):
        os.chmod(p, stat.S_IWRITE)
        f(p)
    shutil.rmtree(d, onerror=onerr) if os.path.exists(d) else None


def check(name, expect, got):
    ok = expect == got
    if not ok:
        FAIL.append(name)
    print(f"{'○' if ok else '✗'} {name}：期待 {expect}・実際 {got}")


def git(d, *a):
    subprocess.run(['git', '-C', d] + list(a), check=True, capture_output=True)


def write(p, text):
    os.makedirs(os.path.dirname(p), exist_ok=True)
    open(p, 'wb').write(text.encode('utf-8'))


def tsv(p, rows):
    write(p, NL.join(TAB.join(r) for r in rows) + NL)


work = work.replace(chr(92), '/')
G = work + '/g_it'
rmtree(G)
os.makedirs(G)
git(G, 'init', '-q'); git(G, 'config', 'user.email', 'x@x'); git(G, 'config', 'user.name', 'x')
base_lines = [f'// base {i}' for i in range(1, 13)]
base_lines[4] = 'double a = Move(x);'
base_lines[8] = 'double b = Take(y);'
rel = 'PriceOfBread.SimCore/Foo.cs'
write(G + '/' + rel, NL.join(base_lines) + NL)
git(G, 'add', '.'); git(G, 'commit', '-q', '-m', 'base')
base = subprocess.run(['git', '-C', G, 'rev-parse', 'HEAD'], capture_output=True, text=True).stdout.strip()
T0 = work + '/t_tail'   # 末尾に足しただけ＝5・9 行目は動かない
write(T0 + '/' + rel, NL.join(base_lines + ['// tail 1', '// tail 2']) + NL)
T1 = work + '/t_head'   # 頭に 3 行足した＝5→8・9→12
write(T1 + '/' + rel, NL.join(['// h1', '// h2', '// h3'] + base_lines) + NL)
T2 = work + '/t_rewrite'   # 5 行目を書き換えた＝基準の 5 行目は写せない
bl = list(base_lines); bl[4] = 'double a2 = Other(x);'
write(T2 + '/' + rel, NL.join(['// h1', '// h2', '// h3'] + bl) + NL)

H = ['quarter', 'nation', 'outTop1Route', 'inTop1Route', 'heirDebtSettledPerQuarter', 'other']
DECL = 'heirDebtSettledPerQuarter'


def rows(route_line_a, route_line_b, heir, other='1.5'):
    return [H,
            ['0', 'X', f'Foo.cs:{route_line_a}:give:P', f'Foo.cs:{route_line_b}:give:P', heir, other],
            ['1', 'X', f'Foo.cs:{route_line_b}:take:P', f'Foo.cs:{route_line_a}:take:P', heir, other]]


def board(name, base_rows, arm_rows, tree, known, expect_ok):
    bd = f'{work}/b_{name}/base'; ad = f'{work}/b_{name}/arm'
    rmtree(f'{work}/b_{name}')
    tsv(bd + '/cearn_s1.tsv', base_rows); tsv(ad + '/cearn_s1.tsv', arm_rows)
    ic.CEARN.update(root=G, base=base, tree=tree, arm_commit=None, maps={}, paths=None)
    n0 = len(ic.FAILS)
    ic.cmp_ins(ad, bd, known, [], [], [])
    got = len(ic.FAILS) == n0
    check(name, expect_ok, got)


K = ic.parse_known(['cearn_s:' + DECL])   # KNOWN と --known-extra の通り道（parse_known）をそのまま通す
KF = ic.parse_known(['cearn_s:' + DECL + '@nation=X'])   # 行の条件つきの宣言
B = rows(5, 9, '10')
board('a_宣言の列だけ違う（経路は動かない）', B, rows(5, 9, '11'), T0, K, True)
board('b_経路の行だけ動く（宣言の列は同じ）', B, rows(8, 12, '10'), T1, K, True)
board('c_宣言の列も経路の行も違う（波81の再現）', B, rows(8, 12, '11'), T1, K, not OLD)
board('c2_宣言の無い宣言（known が空）なら c は不一致', B, rows(8, 12, '11'), T1, {}, False)
board('c3_行の条件つきの宣言（nation=X の行だけ許す）', B, rows(8, 12, '11'), T1, KF, not OLD)
board('d_両方に加えて宣言の外の列も違う', B, rows(8, 12, '11', '1.6'), T1, K, False)
board('d2_宣言の外の列だけ違い経路も動く', B, rows(8, 12, '10', '1.6'), T1, K, False)
board('e_写せない経路（基準の5行目が書き換わった）', B, rows(8, 12, '11'), T2, K, False)
board('e2_写せない経路（宣言の列は同じ）', B, rows(8, 12, '10'), T2, K, False)
# 行の条件が合わない行で宣言の列が違えば不一致（条件つきの宣言は条件の行だけを許す）
rb = [list(r) for r in rows(8, 12, '11')]; rb[2][1] = 'Y'
rbase = [list(r) for r in B]; rbase[2][1] = 'Y'
board('f_条件つきの宣言で条件の外の行が違う', rbase, rb, T1, KF, False)
print('GATEFAIL ' + ' | '.join(FAIL) if FAIL else 'GATEOK')
sys.exit(1 if FAIL else 0)
