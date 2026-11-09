# (1002) c92gf：G-L3 の isfinite の合成の確かめ。使い方: python -X utf8 -I gl3_selftest1002.py [<検査する gates の .py>]（既定は同じ場所の gates1002.py）
#   合成の landsale_s1.tsv に正常な行 1 本＋r3 が inf／nan の行を足し、新（既定）が G-L3 赤、旧（gates997.py）が緑になることを確かめる。緑＝期待どおり・rc=0／1。
import os, subprocess, sys, tempfile
HERE = os.path.dirname(os.path.abspath(__file__)); OLD = os.path.join(HERE, '..', 'c91sl', 'gates997.py')
NEW = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, 'gates1002.py')
H = 'nation\tsaleActualPaidPerQuarter\tsaleIncludedUnpaidPerQuarter\tsaleLawOpenFlag\tsalePaidCopyMaxRelDiff\tsaleCopyPaidPerQuarter\tsaleSlotSumMaxAbsDev\tsaleEventCount'
def run(script, r3rows):
    d = tempfile.mkdtemp(dir='D:/PoB_work/c92gf_work')
    rows = [H, 'X\t1\t1\t1\t0\t1\t0\t1'] + [f'X\t1\t1\t1\t{r}\t1\t0\t1' for r in r3rows]
    open(os.path.join(d, 'landsale_s1.tsv'), 'wb').write(('\n'.join(rows) + '\n').encode('utf-8'))
    o = subprocess.run([sys.executable, '-X', 'utf8', '-I', script, d, '1', '1'], capture_output=True, encoding='utf-8').stdout
    return [l for l in o.split('\n') if l.startswith('G-L3')][0]
bad = 0
def ok(n, c):
    global bad
    print('緑' if c else '赤', n)
    bad += (not c)
green = lambda l: l.rstrip().endswith('通る'); red = lambda l: l.rstrip().endswith('赤')
ok('正常の行だけ：新が通る', green(run(NEW, [])))
for tag, v in (('inf', 'inf'), ('nan', 'nan')):
    ok(f'r3＝{tag} の行：新が赤', red(run(NEW, [v])))
    ok(f'r3＝{tag} の行：旧（gates997）が緑（穴の再現）', green(run(OLD, [v])))
sys.exit(1 if bad else 0)
