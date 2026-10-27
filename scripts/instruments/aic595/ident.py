import sys,glob,os,csv,hashlib
# 計器の生の表ごとの比べ：基線の種 s と新しい出力の種 s。
# 使い方：python ident.py <基線のフォルダ> <新しい出力のフォルダ> <種>（引数の順はどちらでもよい＝aib_s は短い方の見出しまでの列で比べる）
# 例外（旗 0 でも変わる記録の列）：aib_s＝新しい列は落として比べる／flow_s＝sourceLine（呼び元のソースの行番号＝コードの行が増えれば動く記録で、力学ではない）は落として比べる／
#   壁の時間を書く表（burscan_s・clock_s）は元から揺れる。
a,b,s=sys.argv[1],sys.argv[2],sys.argv[3]
csv.field_size_limit(10**9)
def h(p): return hashlib.sha256(open(p,'rb').read()).hexdigest()[:12]
def rows(p):
    with open(p,encoding='utf-8-sig',newline='') as fh:
        r=list(csv.reader(fh,delimiter='\t')); return r[0],r[1:]
same=diff=0; dl=[]
for f in sorted(glob.glob(f'{a}/*_s{s}.tsv')):
    n=os.path.basename(f); g=f'{b}/{n}'
    if not os.path.exists(g): dl.append(('missing',n)); continue
    if n.startswith('aib_s'):
        h1,r1=rows(f); h2,r2=rows(g); k=min(len(h1),len(h2))
        ok = h1[:k]==h2[:k] and len(r1)==len(r2) and all(x[:k]==y[:k] for x,y in zip(r1,r2))
    elif n.startswith('flow_s'):
        h1,r1=rows(f); h2,r2=rows(g)
        d1=[i for i,c in enumerate(h1) if c!='sourceLine']; d2=[i for i,c in enumerate(h2) if c!='sourceLine']
        ok = [h1[i] for i in d1]==[h2[i] for i in d2] and len(r1)==len(r2) and all([x[i] for i in d1]==[y[i] for i in d2] for x,y in zip(r1,r2))
    else:
        ok = h(f)==h(g)
    if ok: same+=1
    else: diff+=1; dl.append(('diff',n))
print('same',same,'diff',diff,dl[:12])
