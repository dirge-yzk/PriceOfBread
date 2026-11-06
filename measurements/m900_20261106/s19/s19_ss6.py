# (900) c77lg：社会保障の戦線が〈前提で固定〉（openFrontStates の L.SocialSecurity:6）の国×四半期を、monlaw_s の同じ国×四半期の
#  官僚制度（<資格任用＝官僚の前提）と目標の段（>今）で分ける（読むだけ）。緊縮の上限は残り（官僚の前提が満たされている行）と読む。
#  あわせて lawGateOpen:Guild・CivilianArms の値の分布を数える。
# 使い方: python -X utf8 s19_ss6.py <生のフォルダ>
import sys, os, collections
d = sys.argv[1]; c = collections.Counter(); g = collections.Counter()
for s in range(1, 11):
    ml = {}
    f = open(os.path.join(d, f'monlaw_s{s}.tsv'), 'rb'); h = f.readline().decode().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for raw in f:
        r = raw.decode().rstrip('\r\n').split('\t')
        ml[(r[ix['nationUid']], r[0])] = (int(r[ix['Bureaucracy']]), int(r[ix['SocialSecurity']]), int(r[ix['SocialSecurityTarget']]))
        g[('Guild門', r[ix['lawGateOpen:Guild']])] += 1; g[('CivilianArms門', r[ix['lawGateOpen:CivilianArms']])] += 1
    f = open(os.path.join(d, f'nation_s{s}.tsv'), 'rb'); h = f.readline().decode().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for raw in f:
        r = raw.decode().rstrip('\r\n').split('\t')
        if 'L.SocialSecurity:6' not in r[ix['openFrontStates']]: continue
        m = ml.get((r[ix['nationUid']], r[0]))
        if m is None: c['monlaw に行なし'] += 1; continue
        bur, cur, tg = m
        if bur < 1 and cur + 1 >= 2: c['官僚の前提（失業保険以上へ・官僚<資格任用）'] += 1
        else: c['残り＝緊縮の上限と読む'] += 1
print(dict(c)); print(dict(g))
