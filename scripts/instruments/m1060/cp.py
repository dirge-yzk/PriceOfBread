import os,sys
R='C:/Users/dirge/Documents/PriceOfBread/'
W='D:/PoB_work/'
pairs=[]
for f in ['prereg.txt','plan.txt','q12_out.txt','q3_out.txt','q2b_out.txt','prereg_next_draft.txt','grep_womenrights.txt']:
    pairs.append((W+'r1057_work/'+f, R+'measurements/m1057_20261112/'+f))
for f in ['q12.py','q3.py','q2b.py']:
    pairs.append((W+'r1057_work/'+f, R+'scripts/instruments/r1057/'+f))
for f in ['out_q23.txt','out_q3b.txt','out_q2local.txt','q1_readers.txt','q4_draft.txt','q5_questions.txt','prereg.txt','plan.txt','report.txt','lastsold_readers.txt']:
    pairs.append((W+'r1058_work/'+f, R+'measurements/m1058_20261112/'+f))
for f in ['q23.py','q3b.py','q2local.py']:
    pairs.append((W+'r1058_work/'+f, R+'scripts/instruments/r1058/'+f))
for f in os.listdir(W+'r1059_work/out'):
    if f.endswith('.txt'): pairs.append((W+'r1059_work/out/'+f, R+'measurements/m1059_20261112/'+f))
for f in ['r1059_d1.py','hitpop.py','run_all.sh']:
    pairs.append((W+'r1059_work/scripts/'+f, R+'scripts/instruments/r1059/'+f))
for s,d in pairs:
    b=open(s,'rb').read().replace(b'\r\n',b'\n')
    if b.startswith(b'\xef\xbb\xbf'): b=b[3:]
    os.makedirs(os.path.dirname(d),exist_ok=True)
    open(d,'wb').write(b)
    print(len(b),d[len(R):])
