# (853) 事前登録の材料：region_s の nationIndex の変化と subst_s の nation の字の変化（旗 0・ヌル腕の生だけ・読むだけ）
import sys, collections
sys.stdout.reconfigure(encoding='utf-8')
def f(x):
    try: return float(x)
    except: return float('nan')
for d in ['keep_852/base_ins','keep_852/n1','keep_852/n2']:
    C=collections.Counter()
    for s in range(1,11):
        own={}  # (q,region)->nationIndex  from region_s
        rows_per_q=collections.Counter()
        with open(f'D:/PoB_work/{d}/region_s{s}.tsv',encoding='utf-8') as fh:
            h=fh.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
            for l in fh:
                c=l.rstrip('\n').split('\t'); own[(int(c[ix['quarter']]),c[ix['region']])]=(c[ix['nationIndex']],c[ix['nation']])
                rows_per_q[int(c[ix['quarter']])]+=1
        C['region_s rows']+=len(own)
        last={}
        prevS={}
        with open(f'D:/PoB_work/{d}/subst_s{s}.tsv',encoding='utf-8') as fh:
            h=fh.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
            for l in fh:
                c=l.rstrip('\n').split('\t'); q=int(c[ix['quarter']]); reg=c[ix['region']]; nat=c[ix['nation']]
                o=own.get((q,reg))
                if o is None: C['subst row without region_s row']+=1; continue
                if o[1]!=nat: C['name mismatch region_s vs subst_s']+=1
                if c[ix['s2RhoSource']]!='1' or f(c[ix['s2EEnd']])!=f(c[ix['s2EEnd']]): 
                    last[reg]=(q,o[0],nat); continue
                p=last.get(reg)
                chg_idx = p is not None and p[1]!=o[0]
                chg_str = p is not None and p[2]!=nat
                C['cand']+=1
                if chg_idx: C['chg_idx']+=1
                if chg_str: C['chg_str']+=1
                if chg_idx!=chg_str: C['idx!=str']+=1
                if chg_idx and f(c[ix['s2SoldEnd']])==0: C['chg_idx&sold0']+=1
                if chg_idx and f(c[ix['s2EEnd']])>0: C['chg_idx&E>0']+=1
                # region_s の前の四半期の index との比べ（subst_s の行が抜けた四半期をまたぐ場合）
                po=own.get((q-1,reg))
                if po is not None and po[0]!=o[0]: C['chg_vs_prev_quarter_region_s']+=1
                if po is not None and po[0]!=o[0] and not chg_idx: C['prevq_chg_not_in_chg_idx']+=1
                last[reg]=(q,o[0],nat)
    print(d, dict(C))
