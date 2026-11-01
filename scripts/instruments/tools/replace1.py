# (728) 1 行置き換えの台本（sed -i の代わり）：ファイル中の旧の文字列がちょうど 1 回のときだけ新の文字列に置き換える。
#   バイトで読み書きする＝CR の数と BOM を保つ（文字モードの open は Windows で改行を変える）。
#   python -I replace1.py <ファイル> <旧の文字列> <新の文字列> [--count N] [--file-old <パス>] [--file-new <パス>]
#     既定 N=1。旧が N 回と違う回数なら何も書かずに 1 を返す（0 回＝旧が違う・2 回以上＝一意でない）。
#     引数の文字列の中の バックスラッシュ+n / +r / +t はそれぞれ 改行 / CR / タブ に直す（バックスラッシュ 2 つは 1 つ）。
#     --file-old / --file-new は文字列を別ファイルのバイトのまま読む（直しの文字列が長い・記号が多いとき）。
import sys

sys.stdout.reconfigure(encoding="utf-8")
B, NUL = chr(92), chr(0)


def unesc(s):
    return (s.replace(B + B, NUL).replace(B + 'n', chr(10)).replace(B + 'r', chr(13))
             .replace(B + 't', chr(9)).replace(NUL, B))


def take(argv, flag):
    if flag in argv:
        i = argv.index(flag)
        v = argv[i + 1]
        del argv[i:i + 2]
        return v
    return None


def main(argv):
    n = int(take(argv, '--count') or 1)
    fo, fn = take(argv, '--file-old'), take(argv, '--file-new')
    if len(argv) < 1 + (0 if fo else 1) + (0 if fn else 1):
        print('使い方：replace1.py <ファイル> <旧> <新> [--count N]')
        return 2
    path, rest = argv[0], argv[1:]
    old = open(fo, 'rb').read() if fo else unesc(rest.pop(0)).encode('utf-8')
    new = open(fn, 'rb').read() if fn else unesc(rest.pop(0)).encode('utf-8')
    if not old:
        print('旧の文字列が空')
        return 2
    data = open(path, 'rb').read()
    c = data.count(old)
    if c != n:
        print(f'置き換えない：旧の文字列は {c} 回（期待 {n} 回）・{path}')
        return 1
    out = data.replace(old, new)
    open(path, 'wb').write(out)
    print(f'置き換えた：{c} 箇所・{path}・{len(data)} → {len(out)} バイト')
    return 0


sys.exit(main(sys.argv[1:]))
