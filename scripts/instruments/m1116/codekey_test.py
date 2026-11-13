# (1116) 合流：ident_check.py の経路の行の写しの鍵 _code_key（行末の注を除いたコードの字）の道具の門（両側＋突然変異）。
#   使い方：python -I -X utf8 codekey_test.py <作業フォルダ>
#   ① 行末の注だけ消えた行は写る ② 同じ行の数字を変えたら ? のまま ③ 文字列の中の // は削らない（文字列の // の後ろが違えば ? のまま）
#   ④ /* を含む行は正規化しない（注が変われば ? のまま＝保守側）。突然変異 N（素朴な剥がし＝最初の // で切る）で ③ が赤になることも確かめる。
#   全部期待どおりなら GATEOK（突然変異の行は MUTRED が期待）。
import sys, os, shutil, subprocess, importlib.util, stat
work = os.path.join(sys.argv[1], 'codekey_repo')
spec = importlib.util.spec_from_file_location('ident_check', os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'ident', 'ident_check.py'))
ic = importlib.util.module_from_spec(spec); spec.loader.exec_module(ic)
sys.stdout.reconfigure(encoding='utf-8')

def rm(p):
    def h(f, x, _):
        os.chmod(x, stat.S_IWRITE); f(x)
    if os.path.exists(p): shutil.rmtree(p, onerror=h)

def git(*a):
    return subprocess.run(['git', '-C', work] + list(a), capture_output=True, text=True, check=True).stdout.strip()

BASE = [
    '// 頭の注',
    'class A {',
    '    void F() {',
    '        World.Transfer(a, b, 1.0);   // ① 行末の注',
    '        World.Transfer(a, b, 2.0);   // ② 行末の注',
    '        var u = "http://x"; World.Transfer(a, b, 3.0);   // ③',
    '        World.Transfer(a, b, 4.0); /* ④ */',
    '        World.Transfer(a, b, 5.0);',
    '    }',
    '}',
]
ARM = [
    'class A {',
    '    void F() {',
    '        World.Transfer(a, b, 1.0);',
    '        World.Transfer(a, b, 2.5);',
    '        var u = "http://y"; World.Transfer(a, b, 3.0);',
    '        World.Transfer(a, b, 4.0); /* 変えた */',
    '        World.Transfer(a, b, 5.0);',
    '    }',
    '}',
]
rm(work); os.makedirs(os.path.join(work, 'PriceOfBread.SimCore'))
subprocess.run(['git', 'init', '-q', work], check=True)
git('config', 'user.email', 't@t'); git('config', 'user.name', 't'); git('config', 'core.autocrlf', 'false')
P = os.path.join(work, 'PriceOfBread.SimCore', 'K.cs')
open(P, 'wb').write(('\r\n'.join(BASE) + '\r\n').encode('utf-8')); git('add', '.'); git('commit', '-q', '-m', 'b'); cb = git('rev-parse', 'HEAD')
open(P, 'wb').write(('\r\n'.join(ARM) + '\r\n').encode('utf-8')); git('add', '.'); git('commit', '-q', '-m', 'a'); ca = git('rev-parse', 'HEAD')

def run(label):
    ic.CEARN.update(root=work, base=cb, tree=None, arm_commit=ca, maps={}, paths=None)
    got = {k: ic._cearn_remap(f'K.cs:{n}:out:x') for k, n in (('1', 4), ('2', 5), ('3', 6), ('4', 7), ('5', 8))}
    return got

EXP = {'1': 'K.cs:3:out:x', '2': 'K.cs:5:out:x?', '3': 'K.cs:6:out:x?', '4': 'K.cs:7:out:x?', '5': 'K.cs:7:out:x'}
FAIL = []
got = run('正')
for k in sorted(EXP):
    ok = got[k] == EXP[k]
    print(f'case{k} 期待 {EXP[k]} 実際 {got[k]} {"ok" if ok else "NG"}')
    if not ok: FAIL.append(k)
# 鍵そのもの
for s, e in ((b'  x; // c', b'  x;'), (b'  y = "a//b";', b'  y = "a//b";'), (b"  z = '\\'' ; // c", b"  z = '\\'' ;"), (b'  w = @"a//b"; // c', b'  w = @"a//b"; // c'), (b'  v = "\\"//"; // c', b'  v = "\\"//";')):
    r = ic._code_key(s); ok = r == e
    print(f'key {s!r} → {r!r} {"ok" if ok else "NG"}')
    if not ok: FAIL.append(s)
# 突然変異 N：素朴な剥がし（最初の // で切る）
orig = ic._code_key
ic._code_key = lambda s: s.split(b'//', 1)[0].rstrip()
gm = run('変異')
ic._code_key = orig
mut_red = gm['3'] != EXP['3']
print(f'突然変異 N（素朴な剥がし）case3 実際 {gm["3"]} → {"MUTRED" if mut_red else "MUTGREEN（門の穴）"}')
if not mut_red: FAIL.append('mutN')
rm(work)
print('GATEFAIL ' + ' '.join(map(str, FAIL)) if FAIL else 'GATEOK')
sys.exit(1 if FAIL else 0)
