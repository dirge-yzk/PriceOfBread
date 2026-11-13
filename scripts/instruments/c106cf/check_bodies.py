# 門1：部品の本体を順に繋ぐと元の class の本体（ffb383805 の 18..3650 行）とバイト一致。python -X utf8 -I check_bodies.py <木>
import subprocess, sys, glob, os
root = sys.argv[1]
src = subprocess.check_output(['git', '-C', root, 'show', 'ffb3838055a49bc32ef0fa901b473ddeccd52253:PriceOfBread.SimCore/Coefficients.cs'])[3:]
L = src.split(b'\n'); orig = b'\n'.join(L[17:len(L) - 2])
files = sorted(glob.glob(os.path.join(root, 'PriceOfBread.SimCore', 'Coefficients*.cs')), key=lambda p: os.path.basename(p).encode())
files = [f for f in files if os.path.basename(f) == 'Coefficients.cs'] + [f for f in files if os.path.basename(f) != 'Coefficients.cs']
parts = []
for f in files:
    d = open(f, 'rb').read(); assert d.startswith(b'\xef\xbb\xbf') and b'\r' not in d, f
    l = d[3:].split(b'\n'); i = l.index(b'{', 10 if os.path.basename(f) == 'Coefficients.cs' else 0)
    assert l[i - 1] == b'public sealed partial class Coefficients', f
    assert l[-1] == b'' and l[-2] == b'}', f
    parts.append(b'\n'.join(l[i + 1:len(l) - 2]))
ok = b'\n'.join(parts) == orig
print(('OK 部品 %d 本・本体一致' if ok else 'NG 本体が違う（部品 %d 本）') % len(files))
sys.exit(0 if ok else 1)
