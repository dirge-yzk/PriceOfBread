# (1163) 四半期 40 に候補の行が無い P15 の外国（清・両シチリア ほか）の O÷L の系列と、州の外国の取り分が 0.01 を超えた最初の四半期（CTL・種 1・2）
import collections
D='D:/PoB_work/keep_1157_rej/c113cr_mut/CTL/ins'
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
want={1:[('フエ','清'),('ハノイ','清'),('ヴィエンチャン','清'),('カトマンズ','清')],2:[('アマラプラ','清'),('ラングーン','清'),('カディス','両シチリア'),('ボローニャ','両シチリア'),('ドレスデン','ポーランド')]}
for s,ws in want.items():
    o=collections.defaultdict(dict); first={}
    with open(f'{D}/rulepower_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t');ix={k:j for j,k in enumerate(h)}
        for l in f:
            c=l.rstrip('\n').split('\t');q=int(c[0]);n=c[ix['nation']]
            if n not in o[q]: o[q][n]=(fl(c[ix['oEngNationIndex']]),fl(c[ix['lNationIndex']]))
            for rg,fk in ws:
                if c[ix['region']]==rg and fl(c[ix['nowRule2Share']])>0.01 and rg not in first: first[rg]=(q,c[ix['foreignTopKey']])
    for fk in sorted(set(f for _,f in ws)):
        ser=[(q,o[q][fk][0]/o[q][fk][1]) for q in range(0,80,8) if fk in o[q] and o[q][fk][1]>0]
        print('種',s,fk,'O÷L（8 四半期ごと）',' '.join(f'{q}:{v:.2f}' for q,v in ser))
    print('種',s,'0.01 を超えた最初',first)
