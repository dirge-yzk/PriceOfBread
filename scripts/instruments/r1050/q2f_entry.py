import csv,collections,sys
K='D:/PoB_work/keep_1044'
arm=sys.argv[1]; N=sys.argv[2] if len(sys.argv)>2 else 'イギリス'
seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
def rows(p):
    with open(p,encoding='utf-8') as f: return list(csv.DictReader(f,delimiter='\t'))
ent=collections.Counter(); mon=[]; mainopp=collections.Counter()
for s in seeds:
    wars=[r for r in rows(f'{K}/{d}/wars_s{s}.tsv') if r['civilWarFlag']=='0' and N in (r['sideA'],r['sideB'])]
    for r in wars:
        if r['sideA']==N and r['entryCode']=='main': mainopp[r['sideB']]+=1
    for r in rows(f'{K}/{d}/imp_s{s}.tsv'):
        if r['loser']!=N: continue
        y=int(r['year']); m=int(r['month']); Wn=r['winner']
        # 講和の月 m（通しの月）で終わった戦争：開始の月＝m−months。startYear だけなので年で緩く合わせる
        cand=[w for w in wars if Wn in (w['sideA'],w['sideB']) and int(w['startYear'])<=y and int(w['startYear'])+int(w['months'])/12.0>=y-1]
        if not cand: ent['見つからない']+=1; continue
        w=min(cand,key=lambda w:abs(int(w['startYear'])+int(w['months'])/12.0-y))
        side='英がA' if w['sideA']==N else '英がB'
        ent[(side,w['entryCode'])]+=1; mon.append(int(w['months']))
print(arm,N,'矯正講和の戦争の入口（英の側・入口）:',ent.most_common())
mon.sort(); print(' 月数の中央',mon[len(mon)//2] if mon else None,' 最小・最大',mon[:1],mon[-1:])
print(' 英が main で攻めた相手（上位12）:',mainopp.most_common(12))
