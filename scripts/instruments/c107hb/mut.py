# 突然変異：後の SimCore を使い捨てのフォルダへ写し、1 か所だけ壊す。
# 使い方: python -I mut.py <後の SimCore> <出力フォルダ> <M1|M2|M3> <前の本文のフォルダ>
import sys
import os
import re
import shutil

src, out, which, before = sys.argv[1:5]
if os.path.exists(out):
    shutil.rmtree(out)
os.makedirs(out)
for dp, dn, fn in os.walk(src):
    if os.sep + 'obj' in dp or os.sep + 'bin' in dp:
        continue
    for f in fn:
        if f.endswith('.cs'):
            rel = os.path.relpath(os.path.join(dp, f), src)
            os.makedirs(os.path.dirname(os.path.join(out, rel)) or out, exist_ok=True)
            shutil.copyfile(os.path.join(dp, f), os.path.join(out, rel))


def rw(name, fn):
    p = os.path.join(out, name)
    t = open(p, 'rb').read().decode('utf-8')
    n = fn(t)
    assert n != t
    open(p, 'wb').write(n.encode('utf-8'))


if which == 'M1':
    # 素朴な剥がし：文字列の中の // の後ろも削る（元の本文から作る）
    for f in os.listdir(before):
        t = open(os.path.join(before, f), 'rb').read().decode('utf-8')
        if re.search(r'"[^"\n]*//[^"\n]*"', t) and '[PORT:' not in t:
            n = re.sub(r'(?m)[ \t]*//[^\r\n]*', '', t)
            open(os.path.join(out, f), 'wb').write(n.encode('utf-8'))
            print('M1 を当てたファイル', f)
            break
elif which == 'M2':
    def f2(t):
        ls = t.split('\n')
        i = next(k for k, l in enumerate(ls) if k > 400 and l.strip().endswith(';') and 'return' not in l)
        print('M2 5 行を落とす', i + 1)
        return '\n'.join(ls[:i] + ls[i + 5:])
    rw('Fiscal.cs', f2)
elif which == 'M3':
    def f3(t):
        m = re.search(r'(?m)^(?!\s*//).*[^0-9.\w]([0-9]+\.[0-9]+)[^0-9.\w].*$', t[20000:])
        line = m.group(0)
        num = m.group(1)
        new = line.replace(num, str(float(num) + 1.0), 1)
        print('M3 定数を変える', line.strip()[:80], '->', new.strip()[:80])
        return t.replace(line, new, 1)
    rw('Model.cs', f3)
