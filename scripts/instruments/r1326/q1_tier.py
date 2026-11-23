# (1326) 問い1：マラーター帝国が成立した四半期に、成立した国がマラーター文化の 6 州と Rules の条件の州を持っていたか（＝どの段で成立したか）
import csv,sys
R=sys.argv[1]; out=open(sys.argv[2],'w',encoding='utf-8')
MC=['プーナ','ナーグプル','グワーリヤル','インドール','バローダ','バサイン']
ANY=['ハイデラバード','シュリーランガパトナ','ラクナウ']
for s in range(1,11):
    form=None
    with open(f'{R}/nation_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['displayName']=='マラーター帝国': form=(int(r['quarter']),r['nation'],r['year']); break
    if not form: out.write(f's{s}\t成立なし\n'); continue
    q0,b,y=form; own={}
    with open(f'{R}/region_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if int(r['quarter'])==q0 and r['region'] in MC+ANY: own[r['region']]=r['nation']
    mc=sum(own.get(c)==b for c in MC); an=sum(own.get(c)==b for c in ANY)
    tier='3(CR・全6州)' if mc==6 and b!='マラータ同盟' else ('2(Rules)' if mc>=5 else '?')
    out.write(f's{s}\t{y}\t{b}\tマラーター6州のうち持つ={mc}\tAnyOf={an}\t推定の段={tier}\t持ち主={own}\n')
