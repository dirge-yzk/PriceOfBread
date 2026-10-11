import csv,sys,statistics
def run(D):
    out=[]
    for s in range(1,11):
        a=f=0
        with open(f'{D}/nation_s{s}.tsv',encoding='utf-8',errors='replace') as fh:
            for r in csv.DictReader(fh,delimiter='\t'):
                for st in r['openFrontStates'].split(';'):
                    if st.startswith('L.'):
                        a+=1
                        if st.rsplit(':',1)[1].rstrip('x')=='2': f+=1
        out.append(100.0*f/a)
    return out
A=run('D:/PoB_work/n282/out_A_10/ins'); B=run('D:/PoB_work/n282/out_B_10/ins')
print('A',[round(x,1) for x in A],'mean %.2f sd %.2f'%(statistics.mean(A),statistics.stdev(A)))
print('B',[round(x,1) for x in B],'mean %.2f sd %.2f'%(statistics.mean(B),statistics.stdev(B)))
d=[b-a for a,b in zip(A,B)]
print('diff',[round(x,1) for x in d],'mean %.2f sd %.2f se %.2f'%(statistics.mean(d),statistics.stdev(d),statistics.stdev(d)/10**0.5))
