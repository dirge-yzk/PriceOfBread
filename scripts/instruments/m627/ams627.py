import csv,sys
for lab,d in [('m617','keep_617/m617_ins'),('m627','m627/out_insn_10/ins'),('N1','m627/out_n1_10/ins'),('N2','m627/out_n2_10/ins')]:
    out=[]
    with open(f'{d}/region_s7.tsv',encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
        for x in r:
            if x[ix['region']]=='アムステルダム' and int(x[ix['quarter']])%4==0 and int(x[ix['year']])%20==3:
                out.append(f"{x[ix['year']]}:{float(x[ix['populationK']]):.0f}/{1-float(x[ix['ruralPopulationK']])/max(1e-9,float(x[ix['populationK']])):.2f}/{float(x[ix['foodFillShare']]):.2f}/{x[ix['nation']][-6:]}")
    print(lab,' '.join(out))
