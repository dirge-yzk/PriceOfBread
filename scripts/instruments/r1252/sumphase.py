# traj.py の出力を区間ごとに足す。使い方: sumphase.py <traj.tsv> <q0:q1> [<q0:q1> ...]
import sys,csv
rows=list(csv.reader(open(sys.argv[1],encoding='utf-8',newline=''),delimiter='\t'))[1:]
print("区間\tP始\tP終\tΔP\tmigIn\tmigOut\txIn\txOut\t残差\t残差のうち+1%/q超の四半期の和\t同四半期数")
for sp in sys.argv[2:]:
    a,b=map(int,sp.split(':')); R=[x for x in rows if a<int(x[0])<=b and x[5]!='']
    P0=[float(x[4]) for x in rows if int(x[0])==a][0]; P1=float(R[-1][4])
    s=lambda i:sum(float(x[i]) for x in R)
    big=[float(x[10]) for x,pp in zip(R,[P0]+[float(y[4]) for y in R[:-1]]) if float(x[10])>0.01*pp]
    print(f"{a}:{b}\t{P0:.2f}\t{P1:.2f}\t{P1-P0:.2f}\t{s(6):.2f}\t{s(7):.2f}\t{s(8):.2f}\t{s(9):.2f}\t{s(10):.2f}\t{sum(big):.2f}\t{len(big)}")
