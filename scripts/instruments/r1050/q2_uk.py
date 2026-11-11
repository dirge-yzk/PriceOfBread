import csv,collections,sys
K='D:/PoB_work/keep_1044'
PN={'0':'絶対','1':'専制','2':'幕府','3':'立憲','4':'共和','5':'ブ独','6':'プ独','7':'軍独','8':'無政'}
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
def rows(p):
    with open(p,encoding='utf-8') as f: return list(csv.DictReader(f,delimiter='\t'))
UK='イギリス'
pair=collections.Counter(); win=collections.Counter(); sc=[]; dec=collections.Counter()
allpair=collections.Counter()
for s in seeds:
    imp=rows(f'{K}/{d}/imp_s{s}.tsv')
    cp=rows(f'{K}/{d}/cpeace_s{s}.tsv'); cpk={(r['month'],r['loser']):r for r in cp}
    for r in imp:
        a=float(r['absScoreIndex']); pk=(PN[r['winnerPolityCode']],PN[r['loserPolityBeforeCode']])
        allpair[pk]+=1
        if r['loser']!=UK: continue
        pair[pk]+=1; win[r['winner']]+=1; sc.append(a)
        dec[(r['year'][:3]+'0s')]+=1
        c=cpk.get((r['month'],r['loser']))
        if arm=='base': print(s,r['year'],r['month'],r['winner'],PN[r['winnerPolityCode']],'<-',PN[r['loserPolityBeforeCode']],'->',PN[r['imposedPolityCode']],f"{a:.3f}",r['ideologyGapIndex'],'ord',r['orderInMonthIndex'],'cp:',(c['nCollapse'],c['nOther'],c['nGrouped'],c['shares'][:80]) if c else None)
print('--',arm,'英',len(sc),'件 abs>=1.499:',sum(x>=1.499 for x in sc),' 1.4-1.499:',sum(1.4<=x<1.499 for x in sc),' <1.4:',sum(x<1.4 for x in sc))
print('勝者の政体<-英の前の政体:',pair.most_common())
print('勝者:',win.most_common(10))
print('年代:',sorted(dec.items()))
print('全国の(勝者,敗者前)組:',allpair.most_common(12))
