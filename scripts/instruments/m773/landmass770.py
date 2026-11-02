"""(773) 合流の記録：c6pr の反証役の数え（portch_s の港が変わった行のうち、旧の港は州と同じ陸塊・新の港は別の陸塊の行）を陸塊の札 SimCore/landmass.json で読む（読むだけ・門ではない＝次の波の上限の門の材料）。
python -I landmass770.py <ins> [<ins> ...]（反証役の使い捨ての台本 lm770.py を木の landmass.json を読む形へ）"""
import sys, os, json, collections
L = {p['name']: p.get('landmass') for p in json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'PriceOfBread.SimCore', 'landmass.json'), encoding='utf-8-sig'))}
for d in sys.argv[1:]:
    print('=====', d)
    rows=chg=0; new_over=old_over=0; unk=0
    ex=collections.Counter(); km={}
    inl_old_over=collections.Counter()
    for f in sorted(x for x in os.listdir(d) if x.startswith('portch_s')):
        fh=open(os.path.join(d,f),encoding='utf-8'); head=fh.readline().rstrip('\n').split('\t'); ix={h:i for i,h in enumerate(head)}
        for ln in fh:
            c=ln.rstrip('\n').split('\t')
            if len(c)<len(head): continue
            rows+=1
            reg,pu,pg=c[ix['region']],c[ix['portUsed']],c[ix['portGc']]
            lr,lu,lg=L.get(reg),L.get(pu),L.get(pg)
            if None in (lr,lu,lg): unk+=1; continue
            if lr!=lg: old_over+=1
            if pu==pg: continue
            chg+=1
            if lu!=lr and lg==lr:
                new_over+=1; k=(c[ix['nation']],reg,lr,pg,pu,lu); ex[k]+=1; km[k]=(round(float(c[ix['gcKm']])),round(float(c[ix['landKmOld']])),round(float(c[ix['landKmNew']])))
    print(f'rows {rows} unknown-landmass rows {unk} changed {chg}; rows whose old port is on another landmass {old_over}; changed rows that newly jump to another landmass {new_over} ({new_over/max(1,chg):.3f})')
    for k,v in ex.most_common(30): print('  ',v,k,km[k])
