# (1141) AI 同士の屈服（S25-26）と砲艦外交の屈服（S25-54）の組を、首都の大圏距離と陸塊で読む（観戦 S4・Q12・X9 の型＝遠い組の屈服）（読むだけ）
# 使い方：python -X utf8 -I submits.py <repo> <生のフォルダ> <ラベル> <種の始め> <種の終わり> <出力>
# ⚠ 距離は world.json の開幕の首都（国名で引く・改名した国と分離独立の新国は引けない＝「不明」）。投射は数えない（エンジンの Projection を呼ばない）。
import sys, os, json, math, statistics
from collections import Counter, defaultdict
REPO, D, LAB, S0, S1, OUT = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5]), sys.argv[6]
W = json.load(open(os.path.join(REPO, 'PriceOfBread.SimCore', 'world.json'), encoding='utf-8-sig'))
pos = {n['name']: (n['latitude'], n['longitude']) for n in W['nations']}
def km(a, b):
    (la1, lo1), (la2, lo2) = a, b
    p1, p2 = math.radians(la1), math.radians(la2); dl = math.radians(lo2 - lo1)
    c = math.sin(p1) * math.sin(p2) + math.cos(p1) * math.cos(p2) * math.cos(dl)
    return 6371.0 * math.acos(max(-1.0, min(1.0, c)))
out = open(OUT, 'w', encoding='utf-8', newline='\n')
out.write(f'# (1141) submits.py arm={LAB} dir={D} seeds {S0}..{S1}\n')
tot = Counter(); far = Counter(); unk = Counter(); perpair = Counter(); kmlist = []
for s in range(S0, S1 + 1):
    p = os.path.join(D, f'warpair_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t'); n = int(r[ix['submits']])
            if n == 0: continue
            a, b = r[ix['nationA']], r[ix['nationB']]
            tot['submit'] += n; perpair[(s, a, b)] += n
            if a in pos and b in pos:
                d = km(pos[a], pos[b]); kmlist += [d] * n
                if d > 4000: far['submit>4000km'] += n
                if d > 8000: far['submit>8000km'] += n
            else: unk['submit'] += n
out.write(f"AI 同士の屈服（warpair submits）の和 {tot['submit']}・距離の引けない組 {unk['submit']}\n")
if kmlist:
    q = statistics.quantiles(kmlist, n=10)
    out.write(f"首都の大圏 km：中央 {statistics.median(kmlist):.0f}・十分位 1 {q[0]:.0f}・9 {q[8]:.0f}・4000km 超 {far['submit>4000km']}（{far['submit>4000km']/len(kmlist):.1%}）・8000km 超 {far['submit>8000km']}（{far['submit>8000km']/len(kmlist):.1%}）\n")
pairs = Counter((a, b) for (s, a, b) in perpair)
out.write(f"屈服が起きた（種×組）{len(perpair)}・1 つの（種×組）の屈服の回数 中央 {statistics.median(perpair.values()):.0f}・最大 {max(perpair.values())}\n")
out.write('回数の多い（種×組）上位 8：' + '・'.join(f'種{s} {a}×{b} {n}' for (s, a, b), n in perpair.most_common(8)) + '\n')
# invade_s：砲艦外交（侵略の要求）の屈服と開戦を距離で
sub = Counter(); kmi = defaultdict(list)
for s in range(S0, S1 + 1):
    p = os.path.join(D, f'invade_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t')
            if r[ix['targetIsNation']] != '1': continue
            a, b, ok = r[ix['invader']], r[ix['target']], r[ix['submitted']] == '1'
            if a in pos and b in pos: kmi['sub' if ok else 'war'].append(km(pos[a], pos[b]))
            else: sub['unk'] += 1
for k in ('sub', 'war'):
    v = kmi[k]
    if v: out.write(f"侵略の要求（国のある標的）{'屈服' if k=='sub' else '開戦'} {len(v)}・首都の大圏 km 中央 {statistics.median(v):.0f}・4000km 超 {sum(1 for x in v if x>4000)}（{sum(1 for x in v if x>4000)/len(v):.1%}）\n")
out.write(f"距離の引けない侵略の行 {sub['unk']}\n")
