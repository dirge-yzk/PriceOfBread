"""(205) スペイン領アメリカ（とフィリピン）から建った国（fx205 の N 行の新しい国・読むだけ）。python an_span205.py <out_dir>"""
import sys, os, re, statistics as st
d = sys.argv[1] + '/fx205'
KEYS = ['中央メキシコ', 'ヌエバ・グラナダ', 'ヌエバ・エスパーニャ', 'ペルー', 'チリ', 'ラ・プラタ', 'ボゴタ', 'キト', 'ラパス', 'カラカス', 'キューバ', 'プエルトリコ', 'フィリピン', 'ルイジアナ・フロリダ', 'トゥクマン', 'レオン', 'アスンシオン', 'サンタフェ', 'サルヴァドール', 'パナマ']
cnt = []; first = []; before1810 = []; allnew = []
for fn in sorted(os.listdir(d)):
    seen = {}
    n61 = 0
    for l in open(os.path.join(d, fn), encoding='utf-8'):
        if not l.startswith('N|'): continue
        p = l.split('|')
        if int(p[3]) < 61: continue
        nm = p[4]
        if nm in seen: continue
        seen[nm] = int(p[2])
    sp = {k: v for k, v in seen.items() if any(x in k for x in KEYS)}
    cnt.append(len(sp)); allnew.append(len(seen))
    if sp: first.append(min(sp.values()))
    before1810.append(sum(1 for v in sp.values() if v < 1810))
print(f'{sys.argv[1]}：新しい国（種あたり）中央 {st.median(allnew)}｜スペイン領アメリカとフィリピンから建った国 種あたり {st.mean(cnt):.1f}（中央 {st.median(cnt)}）・1810 年より前 {st.mean(before1810):.1f}・最初の年の中央 {st.median(first) if first else "—"}（{len(first)}/{len(cnt)} 種）')
