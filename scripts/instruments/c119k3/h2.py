"""(1194) c119k3 史実の桁の線 H2：wars_s の分離（entryCode secession）の数を種ごとに数え、どれかの種で 300（史実の約 30〈推〉の 10 倍）を超えたら赤。
最初の分離の年は記録。使い方: python h2.py <ins の生のフォルダ> [種の始め 種の終わり]"""
import sys, os
d = sys.argv[1]; s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; s1 = int(sys.argv[3]) if len(sys.argv) > 3 else 10
LIM = 300; red = 0; per = []; years = []
for s in range(s0, s1 + 1):
    p = os.path.join(d, f'wars_s{s}.tsv')
    if not os.path.exists(p): per.append(None); continue
    n = 0; y = None
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ie, iy = h.index('entryCode'), h.index('startYear')
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[ie] == 'secession':
                n += 1; y = min(y, int(float(x[iy]))) if y is not None else int(float(x[iy]))
    per.append(n); years.append(y); red += n > LIM
print(f'種ごとの分離={per} 最初の年={years}')
print(f'H2: {"赤" if red else "緑"}（{LIM} を超えた種 {red}）')
