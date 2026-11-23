# (1322) st_gate_born.py の自己試験：合成した小さな生で 緑（突き合う）→rc 0・赤（actualT が台帳と違う）→rc 1・表の欠け→rc 2・余った記帳は既定で緑／--strict で赤。
import os, sys, tempfile, subprocess
HERE = os.path.dirname(os.path.abspath(__file__))
S = os.environ.get('SELFTEST_GATE') or os.path.join(HERE, 'st_gate_born.py')   # 突然変異は書き換えた写しを渡す
def w(d, a, led):
    open(os.path.join(d, 'sectreas_s1.tsv'), 'w', encoding='utf-8', newline='').write('quarter\tparent\tactualT\n' + ''.join(f'{q}\tP\t{v}\n' for q, v in a))
    open(os.path.join(d, 'treasledger_s1.tsv'), 'w', encoding='utf-8', newline='').write('quarter\titem\tvalue\n' + ''.join(f'{q}\t_bornTreasury\t{v}\n' for q, v in led))
def run(d, *x):
    return subprocess.run([sys.executable, '-X', 'utf8', '-I', S, d, '1', '1', *x], capture_output=True, text=True, encoding='utf-8').returncode
bad = []
with tempfile.TemporaryDirectory() as d:
    w(d, [('3', 10.0), ('4', 0.0)], [('3', 10.0), ('4', 0.0)])
    if run(d) != 0: bad.append('緑の組が rc 0 でない')
    w(d, [('3', 10.0)], [('3', 11.0)])
    if run(d) != 1: bad.append('食い違いの組が rc 1 でない')
    w(d, [('3', 10.0)], [('3', 10.0), ('5', 2.0)])
    if run(d) != 0: bad.append('余った記帳が既定で緑でない')
    if run(d, '--strict') != 1: bad.append('余った記帳が --strict で赤でない')
with tempfile.TemporaryDirectory() as d:
    if run(d) != 2: bad.append('表の欠けが rc 2 でない')
print('自己試験', '外れ：' + '；'.join(bad) if bad else '全て期待どおり')
sys.exit(1 if bad else 0)
