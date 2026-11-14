# (1141) 高い帯の冷め方の枝の順（DriftTension：従属→ハイエナ〔敗戦の衝撃≧0.3〕→遺恨）で、遺恨のある組のうちハイエナの枝が先に取る割合（読むだけ・近似）
# 近似：従属の枝は数えない（dep の組み立ては上限）・衝撃は scalars の四半期の末の値（エンジンは月ごと）。
import sys, os
D, LAB, S0, S1 = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
g = gm = gk = 0; fresh = fm = 0
for s in range(S0, S1 + 1):
    sh = {}
    for l in list(open(os.path.join(D, f'scalars_s{s}.tsv'), encoding='utf-8'))[1:]:
        r = l.rstrip('\n').split('\t'); sh[(int(r[0]), r[2])] = float(r[14])
    it = open(os.path.join(D, f'warpair_s{s}.tsv'), encoding='utf-8'); next(it)
    for l in it:
        r = l.rstrip('\n').split('\t')
        if float(r[3]) < 100 or float(r[13]) <= 0: continue
        q = int(r[0]); a, b = sh.get((q, r[1])), sh.get((q, r[2]))
        if a is None and b is None: continue
        g += 1; m = max(x for x in (a, b) if x is not None)
        if m >= 0.3: gm += 1
        if float(r[13]) >= 0.9:
            fresh += 1
            if m >= 0.3: fm += 1
print(f'{LAB}: 高い帯（≧100）で遺恨＞0 の組×四半期（衝撃が読めた行）{g}・うち衝撃≧0.3＝ハイエナの枝が先 {gm}（{gm/max(g,1):.1%}）・遺恨≧0.9（講和の直後）{fresh}・うちハイエナが先 {fm}（{fm/max(fresh,1):.1%}）')
