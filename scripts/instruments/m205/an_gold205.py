"""(205) 金本位の国の数と主要国の法（fx205 の N 行・読むだけ）。python an_gold205.py <out_dir> [種の上限]"""
import sys, os, statistics as st
d = sys.argv[1] + '/fx205'; smax = int(sys.argv[2]) if len(sys.argv) > 2 else 99
M = ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "清", "日本", "オーストリア"]
data = {}
for fn in sorted(os.listdir(d)):
    s = int(fn[2:-4])
    if s > smax: continue
    data[s] = [l.split('|') for l in open(os.path.join(d, fn), encoding='utf-8') if l.startswith('N|')]
print(f'# {sys.argv[1]}・{len(data)}種')
for yr in (1813, 1853, 1873, 1893, 1913, 1933):
    gold = []; silv = []; maj = {}
    for s, rows in data.items():
        q = max(int(p[1]) for p in rows if int(p[2]) == yr)
        rs = [p for p in rows if int(p[1]) == q and float(p[24]) > 0]
        gold.append(sum(1 for p in rs if p[5] == '2')); silv.append(sum(1 for p in rs if p[5] == '1'))
        for p in rs:
            if p[4] in M: maj.setdefault(p[4], []).append(p[5])
    print(f"{yr} 金の国 {st.median(gold):.0f}・銀の国 {st.median(silv):.0f}（種の中央）｜主要国が金の種：" + ' '.join(f"{n[:2]}{sum(1 for x in maj.get(n, []) if x == '2')}/{len(maj.get(n, []))}" for n in M))
