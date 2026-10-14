# 流れG (320)：discprobe の出力（1ファイル＝1種）から、蒸気機関と機関車の解禁の年と、開幕の10年の解禁の数を読む（読むだけ）。
# 使い方：python an_dg.py <プレフィックス> <種の数>   例：python an_dg.py D:/PoB_work/dg/base 10
import re,sys,collections,statistics
pref=sys.argv[1]; n=int(sys.argv[2]); yfrom=int(sys.argv[3]) if len(sys.argv)>3 else 1792
majors=["フランス","プロイセン","オーストリア","ロシア","アメリカ","スペイン","オスマン","ネーデルラント","日本","清"]
rows={}
for sd in range(1,n+1):
    t=open(f'{pref}_s{sd}.txt',encoding='utf-8').read().split('\n')
    ev=[]
    for l in t:
        m=re.match(r'(\d{4}) (\S+) (\S+) ＜(.*)＞',l)
        if m: ev.append((int(m.group(1)),m.group(2),m.group(3),m.group(4)))
    rows[sd]=ev
print(f'## {pref}（{n}種・解禁の出来事は discprobe の section 2）')
print('種 | 蒸気機関 1783-84 の解禁（英以外）| 蒸気機関 1783-92 の解禁（英以外）| 全行 1783-92 の解禁（出来事の数）| 全行 1783 の解禁')
tot=[];
for sd in range(1,n+1):
    ev=rows[sd]
    s84=[e for e in ev if e[2]=='蒸気機関' and e[0]<=1784 and e[1]!='イギリス']
    s92=[e for e in ev if e[2]=='蒸気機関' and e[0]<=1792 and e[1]!='イギリス']
    a92=[e for e in ev if e[0]<=1792]
    a83=[e for e in ev if e[0]==1783]
    tot.append((len(s84),len(s92),len(a92),len(a83)))
    print(f'{sd} | {len(s84)} | {len(s92)} | {len(a92)} | {len(a83)}')
print('中央値 |',' | '.join(str(statistics.median([x[i] for x in tot])) for i in range(4)))
print('合計 |',' | '.join(str(sum(x[i] for x in tot)) for i in range(4)))
for row in ['蒸気機関','機関車']:
    print(f'\n### {row} の最初の年（主要国・種ごと・— は 200 四半期=1832年までに無い）')
    print('国 | '+' | '.join(f's{sd}' for sd in range(1,n+1))+' | 中央値(あるもの) | ある種の数')
    for nm in majors:
        ys=[]
        for sd in range(1,n+1):
            y=[e[0] for e in rows[sd] if e[2]==row and e[1]==nm]
            ys.append(min(y) if y else None)
        have=[y for y in ys if y]
        med=statistics.median(have) if have else '—'
        print(f'{nm} | '+' | '.join(str(y) if y else '—' for y in ys)+f' | {med} | {len(have)}/{n}')
