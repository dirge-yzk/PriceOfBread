# c106dc 門の突然変異：使い捨ての worktree（D:/PoB_work/c106dc_work/mut）で 1 本ずつ壊し、check.py が赤になるかを見る
import subprocess, sys, os, shutil
SRC = 'D:/PoB_work/wt_c106dc'
MUT = 'D:/PoB_work/c106dc_work/mut'


def sh(*a, cwd=None):
    return subprocess.run(a, cwd=cwd, capture_output=True)


def reset():
    if os.path.exists(MUT):
        sh('git', '-C', SRC, 'worktree', 'remove', '--force', MUT)
    r = sh('git', '-C', SRC, 'worktree', 'add', '--detach', MUT, 'HEAD')
    assert r.returncode == 0, r.stderr


def check():
    r = sh('python', '-X', 'utf8', '-I', SRC + '/scripts/instruments/c106dc/check.py', '--tree', MUT)
    out = r.stdout.decode('utf-8', 'replace')
    reds = [l for l in out.split('\n') if l.startswith('赤')]
    return r.returncode, reds


def rw(path, fn):
    p = MUT + '/' + path
    b = open(p, 'rb').read()
    open(p, 'wb').write(fn(b))


LOG = 'docs/202609/99_実装ログ_20260903.md'
A10 = 'docs/202609/99_実装ログ_書庫_202610.md'
A9 = 'docs/202609/99_実装ログ_書庫_202609.md'
SHORT = 'docs/README_線の担当.md'


def m_g1(): rw(A10, lambda b: b[:b.rindex(b'\n', 0, len(b) - 1) + 1])


def m_g2():
    def f(b):
        ls = b.split(b'\n')
        ls[20], ls[21] = ls[21], ls[20]
        return b'\n'.join(ls)
    rw(LOG, f)


def m_g3(): rw(A9, lambda b: b.replace(b'\r\n', b'\n', 1))


def m_g4(): rw(SHORT, lambda b: b + ('- 水増し 〔元 §1-1 L32〕\n' * 600).encode('utf-8'))


def m_g5():
    p = MUT + '/scripts/instruments/c106dc/measure_rg.py'
    open(p, 'ab').write(b'\ngo' + b'od = 12\n')   # 台本自身が規則に当たらないよう、書き方を分ける


def m_g6():
    p = MUT + '/PriceOfBread.SimCore/World.cs'
    open(p, 'ab').write(b'\n// c106dc mutation\n')
    sh('git', 'add', 'PriceOfBread.SimCore/World.cs', cwd=MUT)
    sh('git', '-c', 'user.name=m', '-c', 'user.email=m@m', 'commit', '-q', '-m', 'mut', cwd=MUT)


MUTS = [('G1 書庫の最後の 1 行を落とす', m_g1), ('G2 残りの 2 行を入れ替える', m_g2), ('G3 書庫の CRLF を LF にする（1 行）', m_g3),
        ('G4 短い版を 2 万字超にする', m_g4), ('G5 台本に財の番号の代入を足す', m_g5), ('G6 .cs を 1 つ触ってコミットする', m_g6)]
reset()
base_code, base_reds = check()
print('無変異：終了コード %d・赤 %d 本' % (base_code, len(base_reds)))
for name, fn in MUTS:
    reset()
    fn()
    code, reds = check()
    print('%s → 終了コード %d・赤 %d 本' % (name, code, len(reds)))
    for l in reds:
        print('    ' + l[:110])
sh('git', '-C', SRC, 'worktree', 'remove', '--force', MUT)
