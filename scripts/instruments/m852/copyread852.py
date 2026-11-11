# (852) 合流：読む線・設計の材料の線の作業フォルダ（コミットしない線＝㉖）の台本と表をリポジトリへ写す。
#   python -I -X utf8 copyread852.py <元> <先> [<元> <先> ...]   元がフォルダなら下を全て写す。
#   文字の物（.txt .tsv .py .sh .md .sha256 .log）は CRLF を LF に揃える（中身は CR を除いて同じ）。他はバイトのまま。揃えた本数を出す。
import sys, os, shutil
TEXT = ('.txt', '.tsv', '.py', '.sh', '.md', '.sha256', '.log')
a = sys.argv[1:]
assert len(a) % 2 == 0
n = conv = 0
def one(s, d):
    global n, conv
    os.makedirs(os.path.dirname(d), exist_ok=True)
    if s.endswith(TEXT):
        b = open(s, 'rb').read(); c = b.replace(b'\r\n', b'\n')
        if c != b: conv += 1
        open(d, 'wb').write(c)
    else:
        shutil.copyfile(s, d)
    n += 1
for s, d in zip(a[0::2], a[1::2]):
    if os.path.isdir(s):
        for r, _, fs in os.walk(s):
            for f in fs:
                p = os.path.join(r, f); one(p, os.path.join(d, os.path.relpath(p, s)))
    else:
        one(s, d)
print(f'写した {n} 本・CRLF を LF に揃えた {conv} 本')
