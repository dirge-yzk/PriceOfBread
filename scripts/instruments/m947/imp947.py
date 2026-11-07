# (947) 合流：c83ic の反証役の (ii) を拾う数（君主系の勝者の行で imposedPolityCode が講和の段の頭の王冠からの写しと違う件数）を imp_s から数える（反証役の scratchpad の imp.py の写し・読むだけ）。
#   python -X utf8 -I imp947.py <ins のフォルダ> <種,種,...>
import sys, csv, os, collections
d=sys.argv[1]; seeds=[int(x) for x in sys.argv[2].split(',')]
mp={0:0,1:0,2:1,3:2}  # crown code -> polity code (None->Abs, Abs->Abs, Desp->Desp, Shog->Shog)
crownOf={0:1,1:2,2:3}
tot=0; mw=0; badPol=0; badCrown=0; trans=collections.Counter(); despToAbs=0; chainBad=0; per=[]
for s in seeds:
    rows=list(csv.DictReader(open(os.path.join(d,f'imp_s{s}.tsv'),encoding='utf-8'),delimiter='\t'))
    per.append(len(rows)); tot+=len(rows)
    head={}
    for r in rows:
        k=(r['month'],r['loser'])
        if k not in head: head[k]=int(r['loserCrownBeforeCode'])
    for r in rows:
        if int(r['winnerPolityCode']) not in (0,1,2): continue
        mw+=1; h=head[(r['month'],r['loser'])]; exp=mp[h]
        ip=int(r['imposedPolityCode']); ca=int(r['loserCrownAfterCode'])
        trans[(h,ca)]+=1
        if ip!=exp: badPol+=1
        if ca!=crownOf[exp]: badCrown+=1
        if h==2 and ca==1: despToAbs+=1
print(d.split('/')[-3] if 'keep' not in d else d, 'G2',tot,per,'monarchW',mw,'imposedMismatch',badPol,'crownAfterMismatch',badCrown,'desp->abs',despToAbs, dict(trans))
