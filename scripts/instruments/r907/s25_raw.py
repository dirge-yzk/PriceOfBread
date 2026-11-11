# (907) c78lg 読むだけ：S25 外交の門の両側を、既にある計器つき 10 種の生（列の名で読む）から数える。エンジンを呼ばない。
# 使い方: python -X utf8 s25_raw.py <生のフォルダ> <名>   出力は標準出力（s25/raw_<名>.txt へ書く）
import sys, os, csv, collections, math
d, tag = sys.argv[1:3]
def rows(t):
    for s in range(1, 21):
        p = os.path.join(d, f'{t}_s{s}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8', newline='') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                r['_seed'] = s
                yield r
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
out = []
def P(*a): out.append(' '.join(str(x) for x in a))
seeds = sorted({r['_seed'] for r in rows('wars')})
P('生', d, '種', seeds)
# 1 wars
W = list(rows('wars'))
iw = [r for r in W if r['civilWarFlag'] == '0']
P('[wars] 戦争 全', len(W), '国家間', len(iw), '内戦', len(W) - len(iw), '種ごとの国家間', dict(collections.Counter(r['_seed'] for r in iw)))
pairs = collections.Counter((r['_seed'], frozenset((r['sideA'], r['sideB']))) for r in iw)
P('[wars] 国家間の組（種×組）', len(pairs), '3 回以上の組', sum(1 for v in pairs.values() if v >= 3), '最多', max(pairs.values()) if pairs else 0)
m = sorted(fl(r['months']) for r in iw)
if m: P('[wars] 国家間の月数 中央', m[len(m)//2], '最大', m[-1], '1 か月以下', sum(1 for x in m if x <= 1))
# 2 invade
I = list(rows('invade'))
P('[invade] 侵略の試み', len(I), '屈服 submitted=1', sum(1 for r in I if r['submitted'] == '1'), '拒否 0', sum(1 for r in I if r['submitted'] == '0'),
  '相手が国家', sum(1 for r in I if r['targetIsNation'] == '1'), '相手が非国家', sum(1 for r in I if r['targetIsNation'] == '0'))
pa = [fl(r['proximityAdded']) for r in I]
P('[invade] 近隣の列強へ足した緊張 proximityAdded>0', sum(1 for x in pa if x > 0), '=0', sum(1 for x in pa if x == 0), '中央', sorted(pa)[len(pa)//2] if pa else '')
P('[invade] 相手へ足した緊張 addedToTarget の値', dict(collections.Counter(r['addedToTarget'] for r in I).most_common(5)))
sub_by = collections.Counter((r['targetIsNation'], r['submitted']) for r in I)
P('[invade] (相手が国家, 屈服) の組', dict(sub_by))
# 3 cess
C = list(rows('cess'))
P('[cess] 割譲の州', len(C), 'tier', dict(collections.Counter(r['tier'] for r in C)), 'route', dict(collections.Counter(r['route'] for r in C)))
for k in ('sameLandmassFlag', 'landAdjFlag', 'portFlag', 'canCrossFlag', 'colonialReachOnlyFlag', 'e2OkFlag', 'winnerIsNationFlag', 'loserIsNationFlag'):
    P(f'[cess] {k}', dict(collections.Counter(r[k] for r in C)))
gk = [fl(r['gcKm']) for r in C]; rk = [fl(r['reachKm']) for r in C]
P('[cess] gcKm 最大', max(gk) if gk else '', '4000 超', sum(1 for x in gk if x > 4000), 'reachKm 最大', max(rk) if rk else '')
per = collections.Counter((r['_seed'], r['peaceSeq']) for r in C)
colper = collections.Counter((r['_seed'], r['peaceSeq']) for r in C if r['tier'] not in ('0', 'core', 'Core'))
P('[cess] 講和ごとの州の数 分布', dict(collections.Counter(per.values())), '講和', len(per))
P('[cess] tier ごとの講和で 2 州以上', {t: sum(1 for v in collections.Counter((r['_seed'], r['peaceSeq']) for r in C if r['tier'] == t).values() if v >= 2) for t in sorted({r['tier'] for r in C})})
# 4 cessp
CP = list(rows('cessp'))
P('[cessp] 講和', len(CP), 'n1..n4 の合計', [sum(fl(r[f'n{i}Count']) for r in CP) for i in range(1, 5)], 'loserRegionCount 0', sum(1 for r in CP if fl(r['loserRegionCount']) == 0))
# 5 cpeace
Q = list(rows('cpeace'))
P('[cpeace] 講和', len(Q), '賠償 repRealIndex>0', sum(1 for r in Q if fl(r['repRealIndex']) > 0), '捕獲 capturedCount>0', sum(1 for r in Q if fl(r['capturedCount']) > 0),
  '割譲 cedeRealCount>0', sum(1 for r in Q if fl(r['cedeRealCount']) > 0), 'nCollapse>0', sum(1 for r in Q if fl(r['nCollapse']) > 0),
  'repNew<repOld', sum(1 for r in Q if fl(r['repNewIndex']) < fl(r['repOldIndex'])))
# 6 opium
O = [r for r in rows('opium') if r['scope'] != 'region']
P('[opium] scope', dict(collections.Counter(r['scope'] for r in rows('opium'))))
ban = [r for r in O if r['exportBanFlag'] == '1']
ta = [fl(r['tensionAddedPerQuarter']) for r in O]
P('[opium] 国×四半期', len(O), '輸出禁止 exportBanFlag=1', len(ban), '禁止の国', sorted({(r['nation']) for r in ban})[:12])
P('[opium] 禁令の緊張 tensionAddedPerQuarter>0', sum(1 for x in ta if x > 0), '合計', round(sum(x for x in ta if x == x), 1), '組の月 tensionPairMonthsCount>0', sum(1 for r in O if fl(r['tensionPairMonthsCount']) > 0))
P('[opium] 禁止の国で緊張>0', sum(1 for r in ban if fl(r['tensionAddedPerQuarter']) > 0), '所持の法 possessionLawCode', dict(collections.Counter(r['possessionLawCode'] for r in O)))
# 7 rule treaty port
TP = collections.defaultdict(list)
for r in rows('rule'):
    TP[(r['_seed'], r['region'])].append((int(r['quarter']), r['treatyPortInForceFlag']))
on = sum(1 for v in TP.values() for q, f in v if f == '1')
st = lp = 0
for v in TP.values():
    v.sort()
    for (q0, f0), (q1, f1) in zip(v, v[1:]):
        if f0 != '1' and f1 == '1': st += 1
        if f0 == '1' and f1 != '1': lp += 1
P('[rule] 条約港の州×四半期', on, '入った', st, '外れた（失効か移転）', lp, '条約港を持った州（種×州）', sum(1 for v in TP.values() if any(f == '1' for q, f in v)))
# 8 score
S = list(rows('score'))
P('[score] rankInForce', dict(collections.Counter(r['rankInForce'] for r in S)), 'isGreatShare', dict(collections.Counter(r['isGreatShare'] for r in S)))
ts = [fl(r['treatyStrength']) for r in S]
P('[score] treatyStrength>0', sum(1 for x in ts if x > 0), '/', len(ts))
# 9 scalars
SC = list(rows('scalars'))
ds = [fl(r['defeatShock']) for r in SC]; we = [fl(r['warExhaustion']) for r in SC]; ep = [fl(r['externalPressure']) for r in SC]
P('[scalars] 国×四半期', len(SC), 'defeatShock>0', sum(1 for x in ds if x > 0), '>=0.3', sum(1 for x in ds if x >= 0.3), '最大', max(ds))
P('[scalars] warExhaustion>0', sum(1 for x in we if x > 0), '>=0.5', sum(1 for x in we if x >= 0.5), '最大', max(we), 'externalPressure>0', sum(1 for x in ep if x > 0))
# 10 nation threats
N = list(rows('nation'))
for k in ('securityThreatShare', 'armyThreatShare'):
    v = [fl(r[k]) for r in N]
    P(f'[nation] {k}>0', sum(1 for x in v if x > 0), '/', len(v), '最大', max(x for x in v if x == x))
print('\n'.join(out))
