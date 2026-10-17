# (387) 合流：専制→絶対王政の遷移を scalars_s の crown 列（0=None 1=Absolutist 2=Despotic 3=Shogunate）から数える読み手（読むだけ・反証役 ae〔bv〕中#3）。
# 使い方：PYTHONUTF8=1 python crown_tr.py <腕の出力フォルダ（ins の親）> [...]　＝(前の形, 次の形) ごとの件数と 2→1 の (種, 国, 年)。国は (nation, born) で追う。
import sys,glob,collections
for arm in sys.argv[1:]:
    tot=collections.Counter(); per=collections.Counter()
    for f in sorted(glob.glob(arm+'/ins/scalars_s*.tsv')):
        seed=f.split('scalars_s')[1].split('.')[0]
        last={}
        with open(f,encoding='utf-8') as fh:
            h=fh.readline().rstrip('\n').split('\t'); ci=h.index('crown'); ni=h.index('nation'); bi=h.index('born')
            for line in fh:
                r=line.rstrip('\n').split('\t'); k=(r[ni],r[bi]); c=r[ci]
                p=last.get(k)
                if p is not None and p!=c:
                    tot[(p,c)]+=1
                    if p=='2' and c=='1': per[(seed,r[ni],r[1])]+=1
                last[k]=c
    print(arm,'transitions(from,to)=count  [0None 1Abs 2Desp 3Shog]')
    for k,v in sorted(tot.items()): print('  ',k,v)
    print('  Desp->Abs detail:',sorted(per)[:30])
