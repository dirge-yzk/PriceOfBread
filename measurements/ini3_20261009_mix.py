# 段1：英の階級の構成の写し（(224)・仮番号）＝出典の数字から world.json の英の欄を計算する（読むだけ・再現用）
# 出典：Allen, R.C. (2017) NYUAD Working Paper 0002「Class Structure and Inequality during the Industrial Revolution: Lessons from England's Social Tables, 1688-1867」
#   Appendix Table 3＝Colquhoun の社会表（1801-03）を Lindert & Williamson (1982) が改訂し、Allen が使用人を世帯から外して再集計した表（England & Wales・Allen は 1798 と日付）。
#   Table 2（農業の労働力の比 1759 38%・1798 33%）。公刊＝Economic History Review 72(1), 2018, 88-125。
# 列＝「people minus servants」（世帯の使用人を除いた人数）。使用人は domestic servants（384,057）として別に数える。
import collections, sys
G = {  # グループ記号→シムの階級: N貴族 C聖職 P専門職(教師・官僚・医者) E技師 K資本家 M商人 A職人 W工場労働者 F農民 S使用人 X軍(按分で農民・労働者へ戻す) PA貧民(同)
}
rows = [  # (出典の行名, people minus servants, グループ)
('peers',1292,'N'),('baronets',2430,'N'),('knights',1575,'N'),('esquires',27000,'N'),('gentlemen',90000,'N'),('king',4.5,'N'),
('bishops',117,'C'),('eminent clergy',4500,'C'),('lesser clergy',45000,'C'),('dissenting clergy',11250,'C'),
('high office',9000,'P'),('low office',47250,'P'),('lawyers',49500,'P'),('univ teach',2000,'P'),('school teach',90000,'P'),('liberal arts',73350,'P'),('theatre',4000,'P'),
('engineers',22500,'E'),
('merchants, big',9000,'K'),('shipowners',22500,'K'),('warehouse',2250,'K'),('manufacturers',112500,'K'),('shipbuilders',1350,'K'),
('merchants, little',58500,'M'),('clerks',270000,'M'),
('shopkeepers',335250,'A'),('pubs',225000,'A'),('peddlers',3600,'A'),('tailors, etc',112500,'A'),
('workers in manufacturing',2005767,'W'),('labourers in mines',180000,'W'),('merchant sailors',148179,'W'),('debtors',9000,'W'),('lunatics',2500,'W'),('lunatic asylums',180,'W'),
('freeholders-big',180000,'F'),('freeholders-small',540000,'F'),('farmers',720000,'F'),('labourers in agric & females',1530000,'F'),('farm servants',340000,'F'),
('domestic servants',384057,'S'),
('naval officers',31500,'X'),('military officers',58788,'X'),('naval personnel',158718,'X'),('soldiers',243970,'X'),('half pay officers',18068,'X'),('pensioned soldiers',30500,'X'),
('paupers at work',1040716,'PA'),('vagrants',175218,'PA'),
]
def build(ag_irl=0.70, weights=(7.54,1.5,4.0), half_workers_to_artisans=False, total=14000.0):
    c = collections.Counter()
    for n,v,g in rows: c[g]+=v
    X=c.pop('X'); PA=c.pop('PA'); f=c['F']; w=c['W']
    c['F']+=(X+PA)*f/(f+w); c['W']+=(X+PA)*w/(f+w)           # 規則：軍と貧民は農民・労働者の比で按分
    tot=sum(c.values()); ew={k:v/tot for k,v in c.items()}      # E&W 1798 の構成（使用人込み）
    ag83=38-(38-33)*(1783-1759)/(1798-1759); ratio=ag83/33.0   # 日付合わせ（Allen Table 2・農業の労働力の比を 1798→1783 へ）
    agn=ew['F']*ratio; sc=(1-agn)/(1-ew['F']); e={k:(agn if k=='F' else v*sc) for k,v in ew.items()}
    non=1-e['F']; ir={k:(ag_irl if k=='F' else v*(1-ag_irl)/non) for k,v in e.items()}   # アイルランド：農業 ag_irl・他は E&W の非農業の比
    W_ew,W_sc,W_ir=weights; T=W_ew+W_sc+W_ir
    mix={k:((W_ew+W_sc)*e[k]+W_ir*ir[k])/T for k in e}            # スコットランドは E&W と同じ形【仮置き】
    hN=mix['N']+mix['C']; hK=mix['K']; hS=mix['S']
    a=1/(1-hS+hN+hK)      # 使用人＝（貴族欄＋資本家欄）の式のまま・欄の和＋使用人＝total になる尺度
    cfg={'nobility':a*hN*total,'peasant':a*mix['F']*total,'worker':a*mix['W']*total,'bourgeois':a*hK*total,
         'professional':a*(mix['P']+mix['E'])*total,'merchant':a*mix['M']*total,'artisan':a*mix['A']*total}
    if half_workers_to_artisans:
        h=cfg['worker']/2; cfg['worker']-=h; cfg['artisan']+=h
    out={k:round(v) for k,v in cfg.items()}
    serv=out['nobility']+out['bourgeois']
    out['peasant']+= total-(sum(out.values())+serv)            # 端数は農民へ
    return dict(e_w=ew, e_w1783=e, irl=ir, mix=mix, cfg=out, servants=serv, clergyFrac=mix['C']/hN, w=weights, ag_irl=ag_irl)
if __name__=='__main__':
    for half in (False,True):
        r=build(half_workers_to_artisans=half)
        print('ARM','B(半分を職人へ)' if half else 'A', r['cfg'], 'servants',r['servants'],'sum',sum(r['cfg'].values())+r['servants'],'clergyFrac %.4f'%r['clergyFrac'])
    r=build()
    for nm in ('e_w','e_w1783','irl','mix'): print(nm,{k:round(v*100,2) for k,v in r[nm].items()})
    tot=sum(r['cfg'].values())+r['servants']
    print({k:round(100*v/tot,2) for k,v in r['cfg'].items()}, 'servants',round(100*r['servants']/tot,2))
    for ag in (0.60,0.70,0.80):
        rr=build(ag_irl=ag); print('ag_irl',ag,rr['cfg'],round(rr['clergyFrac'],3))
