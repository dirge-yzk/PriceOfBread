"""(270) 線B2：軍事費の合計の施行の一歩の内訳（militaryMove* の列）を国ごとに読む。python an_move.py <計器の出力ディレクトリ> <nationIndex>（英＝2・仏＝0）。解禁／交戦中で未解禁／平時の3区分の 結果・歩幅・抵抗・政治予算の係数・戦線の数・純支持・施行のシェア。"""
import csv,collections,statistics,sys
D=sys.argv[1]; NAT=sys.argv[2]
rows=[]
for s in range(1,11):
    with open(f'{D}/nation_s{s}.tsv',encoding='utf-8',errors='replace') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['nationIndex']==NAT: r['seed']=s; rows.append(r)
def fl(x):
    try: return float(x)
    except: return float('nan')
def isnan(x): return x!=x
print('nation',NAT,'rows',len(rows))
def desc(v):
    v=[x for x in v if not isnan(x)]
    if not v: return 'n=0'
    v.sort()
    return f'n={len(v)} mean={statistics.mean(v):.4f} p10={v[len(v)//10]:.4f} med={statistics.median(v):.4f} p90={v[9*len(v)//10]:.4f}'
for label,pred in [('open quarters',lambda r:r['warStageOpenAfterGovFlag']=='1'),('at war not open',lambda r:r['atWarAtDecisionFlag']=='1' and r['warStageOpenAfterGovFlag']!='1'),('peace',lambda r:r['atWarAtDecisionFlag']=='0')]:
    sel=[r for r in rows if pred(r) and r['militaryMoveOutcomeCode']!='-1']
    print('==',label,len(sel))
    print(' outcome',collections.Counter(r['militaryMoveOutcomeCode'] for r in sel))
    for c in ['militaryMoveStepFraction','militaryMoveResistIndex','militaryMoveResistTradIndex','militaryMoveResistModernIndex','militaryMoveResistMassIndex','militaryMovePushBudgetFactor','militaryMoveOpenFrontsCount','militaryMoveNetSupportIndex','militaryMoveCurrentShare','militaryMoveTargetShare']:
        print(' ',c,desc([fl(r[c]) for r in sel]))
    print('  capital spent/max', desc([fl(r['politicalCapitalSpentIndex'])/fl(r['politicalCapitalMaxIndex']) for r in sel if fl(r['politicalCapitalMaxIndex'])>0]))
