# (982) SimCore の差が注（// 以降）と空白だけであることを示す台本。使い方：python -I -X utf8 commentonly.py <基準の rev> [出力]
# 変わったファイルごとに、基準と作業ツリーの全行から〈// 以降〉と空白を落とした列が一致するかを見る。文字列の中の // は無い行だけが対象（変わった行に // を含む文字列が無いことも確かめる）。
import subprocess, sys, re
base = sys.argv[1]
files = subprocess.run(['git', 'diff', '--name-only', base, '--', 'PriceOfBread.SimCore'], capture_output=True, text=True, encoding='utf-8').stdout.split()
def strip(t):
    out = []
    for l in t.replace('\r', '').split('\n'):
        i = l.find('//')
        out.append(re.sub(r'\s+', '', l if i < 0 else l[:i]))
    return [x for x in out if x]
bad = 0; lines = []
for f in files:
    old = subprocess.run(['git', 'show', f'{base}:{f}'], capture_output=True, encoding='utf-8').stdout
    new = open(f, 'rb').read().decode('utf-8')
    ok = strip(old) == strip(new)
    n = sum(1 for a, b in zip(old.replace('\r','').split('\n'), new.replace('\r','').split('\n')) if a != b)
    lines.append(f'{f}\t{"注と空白だけ" if ok else "コードが違う"}\t変わった行 {n}')
    bad += 0 if ok else 1
lines.append(f'変わったファイル {len(files)}・コードが違うファイル {bad}・判定 {"一致（注だけ）" if bad == 0 else "不一致"}')
txt = '\n'.join(lines) + '\n'
sys.stdout.write(txt)
if len(sys.argv) > 2: open(sys.argv[2], 'wb').write(txt.encode('utf-8'))
sys.exit(1 if bad else 0)
