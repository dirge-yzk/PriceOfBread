# metal_s の年の産出の和で 1913÷1784（種ごと・中央）
import sys, csv, statistics as st, os
D=sys.argv[1]
for col in ('silverProduced','goldProduced'):
    rs=[];l84=[];l13=[]
    for s in range(1,11):
        p=os.path.join(D,f'metal_s{s}.tsv')
        if not os.path.exists(p): continue
        y={}
        with open(p,encoding='utf-8') as f:
            for r in csv.DictReader(f,delimiter='\t'):
                y[int(r['year'])]=y.get(int(r['year']),0)+float(r[col])
        if 1784 in y and 1913 in y and y[1784]>0: rs.append(y[1913]/y[1784]); l84.append(y[1784]); l13.append(y[1913])
    print(D, col, 'n', len(rs), 'median ratio %.3f'%st.median(rs), '1784 %.0f'%st.median(l84), '1913 %.0f'%st.median(l13), 'per-seed', ' '.join('%.2f'%x for x in rs))
