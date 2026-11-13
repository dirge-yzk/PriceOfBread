# 門 4：消した後に残った注が [PORT:] を含む物だけか（走査の見落としを見る）。移した字数も .md と突き合わせる。
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cslex import find_comments

root = sys.argv[1]      # PriceOfBread.SimCore
before = sys.argv[2]    # 前の本文のフォルダ
hist = sys.argv[3]      # docs/history
files = open(sys.argv[4]).read().split()
bad = 0
kept = 0
moved_before = 0
moved_md = 0
for f in files:
    new = open(os.path.join(root, f), 'rb').read().decode('utf-8')
    old = open(os.path.join(before, f), 'rb').read().decode('utf-8')
    for (s, e, k) in find_comments(new):
        t = new[s:e]
        if '[PORT:' in t:
            kept += 1
        else:
            bad += 1
            print('残り', f, new.count('\n', 0, s) + 1, t[:60])
    allold = sum(len(old[s:e]) for (s, e, k) in find_comments(old))
    keptold = sum(len(old[s:e]) for (s, e, k) in find_comments(old) if '[PORT:' in old[s:e])
    moved_before += allold - keptold
    p = os.path.join(hist, f + '.md')
    if os.path.exists(p):
        md = open(p, 'rb').read().decode('utf-8')
        # 4 つのバッククォートの囲みの中の字数（改行を除く）
        blocks = md.split('````\n')
        inner = ''.join(b for i, b in enumerate(blocks) if i % 2 == 1)
        moved_md += len(inner.replace('\n', ''))
    elif allold != keptold:
        bad += 1
        print('履歴なし', f)
print('残った注（[PORT:]）', kept, '残りの注の数（[PORT:]以外）', bad)
print('元の字数（消す分・改行を除く概算）', moved_before, '履歴の字数', moved_md)
sys.exit(1 if bad else 0)
