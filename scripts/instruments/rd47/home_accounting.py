"""(698) rd47：1 国の本国格の州（Core/Integrated）と植民地格の州の人口・州の数（各年の q%4==3 の断面）と、州の間の移民・入植・越境の流れ、国全体（pop_s の N 行＝植民地込み）の出生・死の内訳を年ごとに並べる（読むだけ）。
使い方：python -X utf8 -I home_accounting.py <計器の出力フォルダ> <種> <国の名（開幕の名）>
限界：出生・死は国全体（本国だけに分けられない）・5 年ごと（1790 年までは毎年）だけ出す・国の鍵は開幕の nationUid。
"""
import csv,sys,collections
csv.field_size_limit(10**9)
d,s,nat=sys.argv[1],sys.argv[2],sys.argv[3]
def rows(p):
    with open(p,encoding='utf-8',newline='') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
        for x in r:
            if len(x)==len(h): yield ix,x
def fl(v):
    try: return float(v)
    except: return 0.0
uid={}
for ix,x in rows(f'{d}/nation_s{s}.tsv'):
    if x[ix['quarter']]=='0' and x[ix['nation']]==nat: U=x[ix['nationUid']]
    uid[(x[ix['quarter']],x[ix['nationIndex']])]=x[ix['nationUid']]
Y=collections.defaultdict(lambda: collections.defaultdict(float))
for ix,x in rows(f'{d}/region_s{s}.tsv'):
    q=int(x[ix['quarter']])
    if uid.get((x[ix['quarter']],x[ix['nationIndex']]))!=U: continue
    y=1783+q//4; home=x[ix['tier']]!='Colonial'
    k='H' if home else 'C'
    if q%4==3: Y[y][k+'P']+=fl(x[ix['populationK']]); Y[y][k+'R']+=1
    for c in ('migrationOutKPerQuarter','migrationInKPerQuarter','settlerOutKPerQuarter','settlerInKPerQuarter','crossBorderOutKPerQuarter','crossBorderInKPerQuarter'):
        Y[y][k+c[:5]+('O' if 'Out' in c else 'I')]+=fl(x[ix[c]])
for ix,x in rows(f'{d}/pop_s{s}.tsv'):
    if x[ix['scope']]!='N' or x[ix['nationUid']]!=U: continue
    y=1783+int(x[ix['quarter']])//4
    for c in ('birthsK','deathsNormalK','deathsFamineK','deathsOtherK','warDeadK'): Y[y][c]+=fl(x[ix[c]])
cols=['HP','HR','CP','CR','HmigraO','HmigraI','HsettlO','HsettlI','HcrossO','HcrossI','CsettlI','birthsK','deathsNormalK','deathsFamineK','deathsOtherK','warDeadK']
print('year\t'+'\t'.join(cols))
for y in sorted(Y):
    if y%5==0 or y<1790: print(str(y)+'\t'+'\t'.join(f'{Y[y][c]:.0f}' for c in cols))
