#!/usr/bin/env python3
"""(935) c82sm: SimCore の差が注だけであることを示す。両側の木から注（// と /* */）を剥いで比べ、注の外の差を数える。
使い方: python scripts/instruments/c82sm/strip_cmp.py <基準の rev> <木のルート> [--dir PriceOfBread.SimCore] [--self-test]
- 文字列（通常・逐語・補間）と文字リテラルの中の // は注と見なさない。文字列と文字リテラルの中の生のタブは、バックスラッシュ t の綴りへ正規化する（値が同じ・正規化した件数の差を数えて名乗る）。
- 各行の末尾の空白は剥ぐ。行の数は剥ぐ前に両側で比べる（SimCore の行の数を変えない約束）。出力: ファイルごとの行数の差と注の外の差の行数。
- 終了コード: 0=全ファイルで行数の差 0 かつ注の外の差 0 / 1=どちらかが 0 でない。
- --self-test: 注でない 1 字の違いを入れた写しで、注の外の差が出ることを確かめる（Simulation.cs の 2 行に空白と 1 字を入れる）。"""
import subprocess, sys, os

BS = chr(92)
TAB = chr(9)
TAB_ESC = BS + 't'


def strip(src):
    """注を剥ぎ、(剥いだ後のテキスト, 生のタブの件数) を返す。生のタブはバックスラッシュ t へ直す。"""
    out = []
    i = 0
    n = len(src)
    tabs = 0
    while i < n:
        c = src[i]
        nx = src[i + 1] if i + 1 < n else ''
        if c == '/' and nx == '/':
            j = src.find('\n', i)
            i = n if j < 0 else j
            continue
        if c == '/' and nx == '*':
            j = src.find('*/', i + 2)
            j = n if j < 0 else j + 2
            out.append('\n' * src.count('\n', i, j))
            i = j
            continue
        if c == '"' or (c in '$@' and (nx == '"' or (nx in '$@' and i + 2 < n and src[i + 2] == '"'))):
            k = i
            verbatim = False
            while src[k] in '$@':
                if src[k] == '@':
                    verbatim = True
                out.append(src[k])
                k += 1
            out.append('"')
            k += 1
            while k < n:
                ch = src[k]
                if verbatim:
                    if ch == '"':
                        if k + 1 < n and src[k + 1] == '"':
                            out.append('""')
                            k += 2
                            continue
                        break
                    out.append(ch)
                    k += 1
                    continue
                if ch == BS:
                    out.append(src[k:k + 2])
                    k += 2
                    continue
                if ch == '"':
                    break
                if ch == TAB:
                    out.append(TAB_ESC)
                    tabs += 1
                    k += 1
                    continue
                out.append(ch)
                k += 1
            out.append('"')
            i = k + 1
            continue
        if c == "'":
            k = i + 1
            while k < n and src[k] != "'":
                if src[k] == BS:
                    k += 1
                k += 1
            seg = src[i:k + 1]
            if TAB in seg:
                tabs += seg.count(TAB)
                seg = seg.replace(TAB, TAB_ESC)
            out.append(seg)
            i = k + 1
            continue
        out.append(c)
        i += 1
    txt = ''.join(out)
    return '\n'.join(l.rstrip() for l in txt.split('\n')), tabs


def git_show(rev, path):
    r = subprocess.run(['git', 'show', rev + ':' + path], capture_output=True)
    return r.stdout.decode('utf8') if r.returncode == 0 else None


def main():
    a = sys.argv[1:]
    rev, tree = a[0], os.path.abspath(a[1])
    d = 'PriceOfBread.SimCore'
    if '--dir' in a:
        d = a[a.index('--dir') + 1]
    selftest = '--self-test' in a
    os.chdir(tree)
    files = subprocess.run(['git', 'ls-tree', '-r', '--name-only', rev, d], capture_output=True, text=True, encoding='utf8').stdout.split('\n')
    files = [f for f in files if f.endswith('.cs')]
    bad = 0
    changed = 0
    tabs_norm = 0
    extra = 0
    for f in files:
        base = git_show(rev, f)
        if not os.path.exists(f):
            print(f + '\t新しい側に無い')
            bad += 1
            continue
        new = open(f, 'rb').read().decode('utf8')
        if selftest and f.endswith('Simulation.cs'):
            new = new.replace('World.Suffrage = World.Policy.Suffrage;', 'World.Suffrage = World.Policy.Suffrage ;', 1)
            new = new.replace('World.Censorship = World.Policy.Censorship;', 'World.Censorship = World.Policy.Censorshiq;', 1)
        ld = new.count('\n') - base.count('\n')
        sb, tb = strip(base)
        sn, tn = strip(new)
        tabs_norm += tn - tb
        lb, ln = sb.split('\n'), sn.split('\n')
        diff = sum(1 for x, y in zip(lb, ln) if x != y) + abs(len(lb) - len(ln))
        if base != new:
            changed += 1
        if ld != 0 or diff != 0:
            print(f + '\t行数の差 ' + str(ld) + '\t注の外の差の行 ' + str(diff))
            bad += 1
        extra += diff
    print('# 比べたファイル ' + str(len(files)) + '・バイトが違うファイル ' + str(changed) + '・行数の差か注の外の差があるファイル ' + str(bad)
          + '・注の外の差の行の合計 ' + str(extra) + '・生のタブを綴りへ正規化した件数の差（新-旧） ' + str(tabs_norm))
    sys.exit(1 if bad else 0)


main()
