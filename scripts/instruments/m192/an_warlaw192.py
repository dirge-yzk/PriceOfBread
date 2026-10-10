"""(192) 戦時に徴兵法を上げた件数を「戦争1回につき1」で数える（読むだけ・反証役 vm1#6）。an_mil182 の ④ は段を1つ上げるごとに1件（志願→皆兵の2段で2件）＝史実の「法を作った回数」と数え方が違う。
python an_warlaw192.py <hist の計器の生の行の dir>   （m180/inj_mil8 の A 行＝an_mil182 と同じ列：q・y・国・alive・war・cw・law）
戦争1回＝交戦（対外 war>0 か内戦 cw>0）が続く四半期の連なり（生きている国）。その連なりの中で徴兵法が連なりの直前の段より上がった四半期が1つでもあれば1件。
史実の件数【推】＝英 2（1916 兵役法・1939 国民兵役法）・米 3（1863 徴兵法・1917 選抜徴兵法・1940 選抜訓練法）・仏 1（1793 国民総動員）・普 1（1813 一般兵役）・ほか0
＝期間あたりの件数は床 1（1783〜1950 に1回）で割る（README §1-2＝比＝シミュ÷max(史実, 床)・10倍以上で NG）。"""
import sys, os, glob, collections
D = sys.argv[1]
HIST = {'イギリス': 2, 'アメリカ': 3, 'フランス': 1, 'プロイセン': 1, 'ロシア': 0, 'オーストリア': 0, 'スペイン': 0, 'オスマン': 0, '日本': 0, '清': 0}
FLOOR = 1.0


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


by = collections.defaultdict(list); seeds = set()
for f in sorted(glob.glob(os.path.join(D, 'a*.txt'))):
    sd = int(os.path.basename(f)[1:-4]); seeds.add(sd)
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'A' or len(t) < 85 or t[4] not in HIST: continue
        by[(sd, t[4])].append((int(t[1]), int(t[5]), fl(t[9]) > 0 or fl(t[10]) > 0, int(t[11])))
nsd = len(seeds) or 1
ep = collections.Counter(); epr = collections.Counter(); steps = collections.Counter()
for (sd, n), v in by.items():
    v.sort()
    i = 0
    while i < len(v):
        q, alive, war, law = v[i]
        if not (alive == 1 and war): i += 1; continue
        pre = v[i - 1][3] if i > 0 else law
        j = i; raised = False
        while j < len(v) and v[j][1] == 1 and v[j][2]:
            if v[j][3] > pre: raised = True
            if j > i and v[j][3] > v[j - 1][3]: steps[n] += v[j][3] - v[j - 1][3]
            j += 1
        if i > 0 and v[i][3] > v[i - 1][3]: steps[n] += v[i][3] - v[i - 1][3]
        ep[n] += 1
        if raised: epr[n] += 1
        i = j
print(f'# 戦時に徴兵法を上げた件数（戦争1回につき1・種 {nsd}・種あたり）｜戦争の回数｜上げた段の数（an_mil182 ④ の数え方）｜史実【推】｜比（床 {FLOOR:.0f}）｜判定')
ng = []
for n in HIST:
    a = epr[n] / nsd; r = a / max(HIST[n], FLOOR)
    j = 'NG' if r >= 10 else '記録' if r >= 3 else '可'
    print(f'{n}: {a:.1f}｜戦争 {ep[n] / nsd:.1f}｜段 {steps[n] / nsd:.1f}｜史実 {HIST[n]}｜比 {r:.1f}｜{j}')
    if j == 'NG': ng.append(f'{n} {a:.1f}件［{HIST[n]}］{r:.1f}倍')
print('NG: ' + ('・'.join(ng) if ng else 'なし'))
