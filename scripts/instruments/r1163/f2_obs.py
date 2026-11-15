# (1163) F2 の効きを見分ける観測量の紙（CTL と Mb〈F2 を外す〉）：rponset_s の pair 行で classHead＝1（①だけ）・データの州でない・0＜sHead の対の、四半期の中の sEnd÷sHead の中央と、下がった対の割合（種 1・2）
import statistics as st
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
for arm in ('CTL','Mb'):
    for s in (1,2):
        r=[];adj=[]
        with open(f'D:/PoB_work/keep_1157_rej/c113cr_mut/{arm}/ins/rponset_s{s}.tsv',encoding='utf-8') as f:
            h=f.readline().rstrip('\n').split('\t');ix={k:j for j,k in enumerate(h)}
            for l in f:
                c=l.rstrip('\n').split('\t')
                if c[ix['rowKind']]!='pair' or c[ix['classHead']]!='1' or c[ix['dataStateFlag']]=='1': continue
                a,b=fl(c[ix['sHead']]),fl(c[ix['sEnd']])
                if a>0 and b==b:
                    (adj if c[ix['adjacentFlag']]=='1' else r).append(b/a)
        print(arm,'種',s,'①だけ・隣でない 対×四半期',len(r),'sEnd÷sHead の中央',round(st.median(r),5) if r else None,'下がった割合',round(sum(x<1 for x in r)/len(r),4) if r else None,'｜隣',len(adj),round(st.median(adj),5) if adj else None)
