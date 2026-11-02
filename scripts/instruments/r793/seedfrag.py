# (793) 成績表の種ごとの断片（「seedN=…」「seedN Q…」）を種 1〜3 だけ抜いて、2 枚の成績表で集合が一致するかを見る（読むだけ・補助の確かめ）
import sys, re
from collections import Counter
def frags(p, seeds):
    out = Counter()
    for line in open(p, encoding='utf-8'):
        for m in re.finditer(r'seed(\d+)([=＝ ][^　]*?)(?=　seed\d|$)', line.rstrip('\n')):
            if m.group(1) in seeds: out[(m.group(1), m.group(2).strip())] += 1
    return out
a, b = sys.argv[1], sys.argv[2]; seeds = set(sys.argv[3].split(','))
fa, fb = frags(a, seeds), frags(b, seeds)
print('断片', sum(fa.values()), sum(fb.values()), '共通', sum((fa & fb).values()))
for k in list((fa - fb).keys())[:8]: print('  腕だけ', k[0], k[1][:150])
for k in list((fb - fa).keys())[:8]: print('  基準だけ', k[0], k[1][:150])
