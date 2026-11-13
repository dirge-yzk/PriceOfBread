# 門 1・門 2 の突然変異（紙の台本を実行する）。使い方：python -X utf8 -I mut_gates12.py <作業フォルダ>
# M0 正しい台本＝緑。M1 文字列の中の // を台本が削る変種（素朴な走査）＝門 2 赤。M2 履歴から 1 かたまり落とす＝門 1 赤。M3 注の隣のコードの数を 1 つ変える＝門 2 赤。
import sys, os, subprocess, shutil, re
W = sys.argv[1]
HERE = os.path.dirname(os.path.abspath(__file__))
SAMPLE = '''class S {
    // 本物の注（印なし）
    string u = "a//b";   // (1234) 経緯のある行末の注。これは移る
    string v = @"c:\\x//y"; // 波12 の注
    // (1999) 経緯のかたまりの 1 行目。
    // 2 行目。
    int Foo() { return 1; }
}
'''
REL = 'PriceOfBread.SimCore/Sample.cs'


def sh(*a, cwd=None, ok=True):
    r = subprocess.run(a, cwd=cwd, capture_output=True)
    if ok and r.returncode != 0:
        raise SystemExit(r.stderr.decode('utf-8', 'replace'))
    return r


def fresh(name):
    d = os.path.join(W, name)
    shutil.rmtree(d, ignore_errors=True)
    os.makedirs(os.path.join(d, 'PriceOfBread.SimCore'))
    os.makedirs(os.path.join(d, '_hist'))
    sh('git', 'init', '-q', '.', cwd=d)
    sh('git', 'config', 'core.autocrlf', 'false', cwd=d)
    open(os.path.join(d, REL), 'wb').write(SAMPLE.encode('utf-8'))
    sh('git', 'add', '.', cwd=d)
    sh('git', '-c', 'user.email=a@b', '-c', 'user.name=n', 'commit', '-q', '-m', 's', cwd=d)
    c = sh('git', 'rev-parse', 'HEAD', cwd=d).stdout.decode().strip()
    open(os.path.join(d, '_files.txt'), 'wb').write(REL.encode())
    return d, c


def move(d, c, scripts):
    sh('python', '-X', 'utf8', '-I', os.path.join(scripts, 'move_notes.py'), d, os.path.join(d, '_hist'), c, REL)


def check(d, c):
    r = sh('python', '-X', 'utf8', '-I', os.path.join(HERE, 'check_notes.py'), d, c, os.path.join(d, '_hist'), os.path.join(d, '_files.txt'), ok=False)
    err = r.stderr.decode('utf-8', 'replace').strip().split('\n')[-1:]
    return r.returncode, (r.stdout.decode('utf-8', 'replace').strip().replace('\n', ' / ') + ' ' + ' '.join(err)).strip()


res = []
d, c = fresh('m0')
move(d, c, HERE)
res.append(('M0 正しい台本', check(d, c)))
p = os.path.join(d, REL)
shutil.copy(p, p + '.keep')
# M3 注の隣のコードの数を 1 つ変える
t = open(p, 'rb').read().decode('utf-8')
open(p, 'wb').write(t.replace('return 1;', 'return 2;').encode('utf-8'))
res.append(('M3 コードの数を 1 つ変える', check(d, c)))
shutil.copy(p + '.keep', p)
# M2 履歴から 1 かたまり落とす
hp = os.path.join(d, '_hist', 'Sample.cs.md')
h = open(hp, 'rb').read().decode('utf-8')
i = h.index('## §2')
open(hp, 'wb').write((h[:i]).encode('utf-8'))
res.append(('M2 履歴から 1 かたまり落とす', check(d, c)))
# M1 素朴な走査（文字列の中の // も注）の台本
d, c = fresh('m1')
nd = os.path.join(W, 'naive')
shutil.rmtree(nd, ignore_errors=True)
shutil.copytree(HERE, nd, ignore=shutil.ignore_patterns('__pycache__'))
cp = os.path.join(nd, 'csscan.py')
t = open(cp, 'rb').read().decode('utf-8')
t += '\n\ndef scan(t):\n    out = []\n    for m in re.finditer("//[^" + chr(10) + "]*", t):\n        out.append(("line", m.start(), m.end()))\n    return out\n'
open(cp, 'wb').write(t.encode('utf-8'))
move(d, c, nd)
res.append(('M1 素朴な走査の台本', check(d, c)))
for n, (rc, out) in res:
    print('%s\t終了=%d\t%s' % (n, rc, out))
