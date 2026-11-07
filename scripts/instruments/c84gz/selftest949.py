# (949) c84gz 自己試験（両側）：judge949.py の判定と gdp_fall949.py の下がり。期待は盤面の意図から書く（台本の出力から写していない）。
#   使い方: python -X utf8 -I selftest949.py [--judge <judge949.py>] [--fall <gdp_fall949.py>] [--work <作業フォルダ>]   （突然変異の試験は --judge・--fall に変えた写しを渡す）
import os, re, subprocess, sys, tempfile
A = sys.argv[1:]
def opt(n, d): return A[A.index(n) + 1] if n in A else d
HERE = os.path.dirname(os.path.abspath(__file__))
JUDGE = opt('--judge', os.path.join(HERE, 'judge949.py')); FALL = opt('--fall', os.path.join(HERE, 'gdp_fall949.py'))
W = opt('--work', tempfile.mkdtemp()); os.makedirs(W, exist_ok=True)
def stop_file(name, c3, c4):   # stoplines906.sh の出力の二つの節だけを写した最小の形
    p = os.path.join(W, name)
    open(p, 'w', encoding='utf-8', newline='').write(
        f'== gdpjump833（3 区分・行つき）\n# h\nM\t種 10\t100 倍以上 {c3}（建国の直後〔≦4 四半期〕0・分母の痩せ〔x〕0・それ以外 {c3}）\t1e10 倍以上 0\n'
        f'== gdp_zero10\nM: x\n== 記録（906・新しい区分・採否に使わない）：gdp_jump906 --founding\n# h\nM\t種 10\t100 倍以上 {c4}（建国の直後〔≦4 四半期〕0・建国の後の 0 の続き〔y〕0・分母の痩せ 0・それ以外 {c4}）\t1e10 倍以上 0\n')
    return p
def judge(rule, arm, nulls, basis='4'):
    a = [sys.executable, '-X', 'utf8', '-I', JUDGE, '--rule', rule, '--basis', basis, '--arm', arm]
    for n in nulls: a += ['--null', n]
    return subprocess.run(a, capture_output=True, encoding='utf-8').returncode
n1 = stop_file('n1', 1, 1); n2 = stop_file('n2', 1, 0); n0 = stop_file('n0', 0, 0)
eq = stop_file('eq', 1, 1); plus = stop_file('plus', 2, 2); zero = stop_file('zero', 0, 0); b3 = stop_file('b3', 2, 1)
res = []
def ok(name, cond): res.append(bool(cond)); print(('緑 ' if cond else '赤 ') + name)
NU = [n1, n2]
ok('J1 pair・ヌル腕の最大と同数（腕 1・参照 1）は記録（rc 0）', judge('pair', eq, NU) == 0)
ok('J2 pair・ヌル腕の最大より 1 多い（腕 2・参照 1）は止める（rc 1）', judge('pair', plus, NU) == 1)
ok('J3 strict・同数でも止める（rc 1）', judge('strict', eq, NU) == 1)
ok('J4 pair・腕 0 は記録', judge('pair', zero, NU) == 0)
ok('J5 strict・腕 0 は記録', judge('strict', zero, NU) == 0)
ok('J6 数える区分 4 では腕（3 区分 2・4 区分 1）は記録・区分 3 では参照 1 に対し 2 で止める', judge('pair', b3, NU, '4') == 0 and judge('pair', b3, NU, '3') == 1)
ok('J7 pair にヌル腕が無ければ入力の誤り（rc 2）', judge('pair', eq, []) == 2)
ok('J8 ヌル腕が全て 0 なら参照 0＝腕 1 は止める', judge('pair', eq, [n0]) == 1)
def fall():
    p = subprocess.run([sys.executable, '-X', 'utf8', '-I', FALL, '--rows', f'FIX={os.path.join(HERE, "fixture")}'], capture_output=True, encoding='utf-8').stdout
    return p, {m.group(2): m.group(1) for m in (re.match(r"\s+(\S+)\s+\(\d+, '([^']*)'", l) for l in p.split('\n')) if m}
out, got = fall()
EXP = {'a安定の下がり': 'それ以外', 'b痩せの下がり': '分母の痩せ', 'c建国の直後の下がり': '建国の直後', 'e100分の1ちょうど': 'それ以外'}
bad = [f'{k}:{v}→{got.get(k, "無し")}' for k, v in EXP.items() if got.get(k) != v]
ok('F1 下がりの区分（建国の直後・痩せ・それ以外・100 分の 1 ちょうどは数える）' + (' ' + ' '.join(bad) if bad else ''), not bad)
ok('F2 99 分の 1・上がりだけ・0 への落ちは下がりの行に入らない', 'd99分の1' not in got and 'g上がりだけ' not in got and 'f0への落ち' not in got)
ok('F3 0 以下への落ちは別の列に 1 件・下がりは 4 件', '0 以下への落ち 1' in out and '下がり 4（' in out)
print('合計', len(res), '赤', res.count(False)); sys.exit(0 if all(res) else 1)
