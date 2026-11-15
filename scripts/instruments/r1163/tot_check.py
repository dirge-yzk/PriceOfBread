# (1163) 力の和の組み直しの確かめ：rulepower_s の fState÷(fState＋fInter＋fResist＋fForeignEng＋fUngov) とエンジンの t0EngShare（全ての州・四半期 40 と 79・種 1・2）
import math
D='D:/PoB_work/keep_1157_rej/c113cr_mut/CTL/ins'
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
for s in (1,2):
    ok=bad=0
    with open(f'{D}/rulepower_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t');ix={k:j for j,k in enumerate(h)}
        for l in f:
            c=l.rstrip('\n').split('\t')
            if c[0] not in ('40','79'): continue
            v=[fl(c[ix[k]]) for k in ('fStateIndex','fInterIndex','fResistIndex','fForeignEngIndex','fUngovIndex')]
            t0=fl(c[ix['t0EngShare']]); tot=sum(v)
            if not (tot>0) or math.isnan(t0): continue
            if abs(v[0]/tot-t0)<=1e-9*max(1,t0): ok+=1
            else: bad+=1
    print('種',s,'t0 の組み直し 一致',ok,'不一致',bad)
