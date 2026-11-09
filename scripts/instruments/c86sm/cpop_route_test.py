# (963) c86sm：cpop_s の経路の列（outOtherTop1Route）の行の移動を写す道（ident_check の cpop_s 枝）と、cearn_s・cpop_s の宣言が表ごとに別であることの試験。
#   使い方：python -I -X utf8 cpop_route_test.py <作業フォルダ> [ident_check.py のパス＝既定は ../ident/ident_check.py]
#   盤面は手で組む（小さな表と使い捨ての git の木）。cmp_ins まで通して判定を読む。1 つでも期待と違えば最後に GATEFAIL・終了コード 1。全部期待どおりなら GATEOK。
#   環境変数 C86SM_EXPECT_OLD=1：main の道具（cpop_s の経路の移動を写せない）での予言を期待値にする（事前登録の再現用）。
#   再現する組み合わせは 2 つ：(933) 宣言の列と経路の行の移動が同じ表に重なる形（cearn_s）と、cpop_s の経路の行の移動（cpop_s・cearn_s が同じ出力フォルダに並ぶ形も）。
import sys, os, io, shutil, stat, subprocess, importlib.util, contextlib

work = sys.argv[1]
icpath = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'ident', 'ident_check.py')
OLD = os.environ.get('C86SM_EXPECT_OLD') == '1'
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
T1 = work + '/t_head'   # 頭に 3 行足した＝5→8・9→12
write(T1 + '/' + rel, NL.join(['// h1', '// h2', '// h3'] + base_lines) + NL)
T2 = work + '/t_rewrite'   # 5 行目を書き換えた＝基準の 5 行目は写せない
bl = list(base_lines); bl[4] = 'double a2 = Other(x);'
write(T2 + '/' + rel, NL.join(['// h1', '// h2', '// h3'] + bl) + NL)

HP = ['quarter', 'nation', 'region', 'outOtherTop1Route', 'debtInCarryPerQuarter', 'other']
HE = ['quarter', 'nation', 'outTop1Route', 'inTop1Route', 'heirDebtSettledPerQuarter', 'other']


def prows(la, lb, carry='3', other='1.5'):   # cpop_s の盤面（経路の列は 1 本・'-' の行も混ぜる）
    return [HP,
            ['0', 'X', 'r1', f'Foo.cs:{la}:take:P', carry, other],
            ['1', 'X', 'r1', '-', carry, other],
            ['2', 'X', 'r2', f'Foo.cs:{lb}:take:B', carry, other]]


def erows(la, lb, heir='10', other='1.5'):   # cearn_s の盤面
    return [HE,
            ['0', 'X', f'Foo.cs:{la}:give:P', f'Foo.cs:{lb}:give:P', heir, other],
            ['1', 'X', f'Foo.cs:{lb}:take:P', f'Foo.cs:{la}:take:P', heir, other]]


def board(name, tables_base, tables_arm, tree, known, expect_ok, label=None):
    # (977) label＝判定の文に出るはずの〈実際に違った列〉の札（None＝札が出ない＝実際に違った宣言の列が無い）。旧の道（OLD）では見ない。
    root = f'{work}/b_{name}'
    rmtree(root)
    bd, ad = root + '/base', root + '/arm'
    for f, r in tables_base.items(): tsv(f'{bd}/{f}', r)
    for f, r in tables_arm.items(): tsv(f'{ad}/{f}', r)
    ic.CEARN.update(root=G, base=base, tree=tree, arm_commit=None, maps={}, paths=None)
    n0 = len(ic.FAILS)
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        ic.cmp_ins(ad, bd, known, [], [], [])
    out = buf.getvalue()
    sys.stdout.write(out)
    got = len(ic.FAILS) == n0
    check(name, expect_ok, got)
    if expect_ok and not OLD:   # (977) 札の文字列そのものを見る（_actual() が空でも緑になる穴を塞ぐ）
        if label is None:
            check(name + '：札なし', False, '実際に違った列' in out)
        else:
            check(name + '：札 ' + label, True, ('（実際に違った列 ' + label + '）') in out)   # (982) 札の後ろの閉じ括弧まで見る（接頭辞の列名でも緑になる穴を塞ぐ）


KP = ic.parse_known(['cpop_s:debtInCarryPerQuarter'])
KE = ic.parse_known(['cearn_s:heirDebtSettledPerQuarter'])
KB = ic.parse_known(['cpop_s:debtInCarryPerQuarter', 'cearn_s:heirDebtSettledPerQuarter'])
P0 = {'cpop_s1.tsv': prows(5, 9)}
board('a_cpop_s の経路の行だけ動く', P0, {'cpop_s1.tsv': prows(8, 12)}, T1, {}, not OLD, None)
board('b_cpop_s の経路の行も宣言の列も動く（宣言あり）', P0, {'cpop_s1.tsv': prows(8, 12, carry='4')}, T1, KP, not OLD, 'debtInCarryPerQuarter')
board('b2_宣言の列だけ違い経路は動かない（旧の道へ落ちる）', P0, {'cpop_s1.tsv': prows(5, 9, carry='4')}, T1, KP, True)
board('c_cpop_s の経路も動き宣言の外の列も違う', P0, {'cpop_s1.tsv': prows(8, 12, other='1.6')}, T1, KP, False)
board('d_cpop_s の経路が写せない（基準の5行目が書き換わった）', P0, {'cpop_s1.tsv': prows(8, 12)}, T2, KP, False)
board('e_cpop_s の宣言が無く cearn_s の宣言だけ（宣言は表ごと）', P0, {'cpop_s1.tsv': prows(8, 12, carry='4')}, T1, KE, False)
board('f_同じフォルダに cearn_s（宣言の列と経路が重なる）と cpop_s（経路だけ）が並ぶ',
      {**P0, 'cearn_s1.tsv': erows(5, 9)}, {'cpop_s1.tsv': prows(8, 12), 'cearn_s1.tsv': erows(8, 12, heir='11')}, T1, KB, not OLD, 'heirDebtSettledPerQuarter')
board('g_cearn_s の宣言の列は cpop_s の違いを許さない（known の取り違え）',
      {**P0, 'cearn_s1.tsv': erows(5, 9)}, {'cpop_s1.tsv': prows(8, 12, carry='4'), 'cearn_s1.tsv': erows(8, 12, heir='11')}, T1, KE, False)
board('h_行の数が違う', P0, {'cpop_s1.tsv': prows(8, 12) + [['3', 'X', 'r3', '-', '3', '1.5']]}, T1, KP, False)
print('GATEFAIL ' + ' | '.join(FAIL) if FAIL else 'GATEOK')
sys.exit(1 if FAIL else 0)
