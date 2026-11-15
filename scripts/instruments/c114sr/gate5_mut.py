# (1160) c114sr：c113sw/gate5_mut.py の写し（子に -X utf8 を渡す＝出力の文字化けを直す）。元：(1154) c113sw：c112tt/gate5.py の突然変異の確かめ。基準の rev の木を使い捨ての worktree に出し、
#   (0) 何も変えない木 → 全て OK（終了 0）  (1) 注を 1 本消した木 → RED（終了 1）  (2) 注を 121 字に伸ばした木 → RED
# 使い方：python gate5_mut.py <リポジトリ> <使い捨ての置き場> <基準の rev>   ＝ 置き場は無い名前（終わりに外して消す）。
import os, re, subprocess, sys
repo, wt, rev = sys.argv[1], sys.argv[2], sys.argv[3]
here = os.path.dirname(os.path.abspath(__file__))
gate5 = os.path.join(here, '..', 'c112tt', 'gate5.py')
def sh(*a, **k): return subprocess.run(a, capture_output=True, **k)
sh('git', '-C', repo, 'worktree', 'add', '--detach', wt, rev)
res = {}
try:
    p = os.path.join(wt, 'PriceOfBread.SimCore', 'Coefficients.p04_trade.cs')
    orig = open(p, 'rb').read()
    def run(tag):
        r = subprocess.run([sys.executable, '-X', 'utf8', '-I', gate5, wt, rev], capture_output=True)
        res[tag] = r.returncode
        print(tag, '終了', r.returncode, (r.stdout.decode('utf-8', 'replace').strip().splitlines() or [''])[-1])
    run('0 変更なし')
    lines = orig.split(b'\n')
    k = next(i for i, l in enumerate(lines) if re.match(rb'\s*public double \w+ = [^;]+;\s*//', l))
    m = lines[k]; j = m.index(b'//')
    open(p, 'wb').write(b'\n'.join(lines[:k] + [m[:j].rstrip()] + lines[k + 1:]))
    run('1 注を 1 本消す')
    open(p, 'wb').write(b'\n'.join(lines[:k] + [m[:j] + b'// ' + ('あ' * 130).encode('utf-8')] + lines[k + 1:]))
    run('2 注を 130 字に伸ばす')
finally:
    sh('git', '-C', repo, 'worktree', 'remove', '--force', wt)
ok = res.get('0 変更なし') == 0 and res.get('1 注を 1 本消す') == 1 and res.get('2 注を 130 字に伸ばす') == 1
print('突然変異の確かめ：', '合格（変更なし OK・消す RED・伸ばす RED）' if ok else '不合格')
sys.exit(0 if ok else 1)
