import csv,statistics
def fl(x):
    try: return float(x)
    except: return float('nan')
def rows(d,sd,kind,nat='イギリス'):
    with open(f'{d}/{kind}_s{sd}.tsv',encoding='utf-8') as fh:
        return [x for x in csv.DictReader(fh,delimiter='\t') if x['nation']==nat]
def med(v):
    v=[a for a in v if a==a]; return statistics.median(v) if v else float('nan')
for lab,d,seeds in (('A','out_A_10/ins',range(1,6)),('B','out_B_5/ins',range(1,6))):
    buf=[];buf_by={'Q1-40':[],'Q41-160':[],'Q161-':[]}
    for sd in seeds:
        for x in rows(d,sd,'fiscal'):
            b=5*(fl(x['claimInterestPerMonth'])+fl(x['claimAdminPerMonth'])+fl(x['claimArmyPerMonth'])+fl(x['claimNavyPerMonth']))
            q=int(x['monthIndex'])//3
            w='Q1-40' if q<40 else ('Q41-160' if q<160 else 'Q161-')
            buf.append(b); buf_by[w].append(b)
    # 逼迫と法
    st=[];ss=[];pt=[];stage=[]
    for sd in seeds:
        for x in rows(d,sd,'nation'):
            st.append(fl(x['fiscalStressAtDecisionIndex'])); ss.append(fl(x['socialSecurityEndShare'])); pt.append(fl(x['pollTaxEndShare'])); stage.append(fl(x['socialSecurityLawEndStage']))
    print(lab,'財政バッファ近似 5×(利払い+行政+装備陸+海) 月の中央(全期)=%.0f'%med(buf),{k:round(med(v)) for k,v in buf_by.items()},
          '| 逼迫指標(fiscalStressAtDecisionIndex)中央=%.3f 平均=%.3f'%(med(st),statistics.fmean([a for a in st if a==a])),
          '| 福祉の実際(end)平均=%.3f 福祉の段>0の四半期=%d / %d'%(statistics.fmean([a for a in ss if a==a]),sum(1 for a in stage if a>0),len(stage)),
          '| 人頭税(end)平均=%.3f'%statistics.fmean([a for a in pt if a==a]))
