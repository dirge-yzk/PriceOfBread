# 流れG (320)：標準の計器の disc_s<seed>.tsv（発見の表の行の出来事 E と蒸気機関・機関車の軌道 T）を読む（読むだけ）。
# 使い方：PYTHONUTF8=1 python an_disc320.py <ins のフォルダ> [<ins のフォルダ2>...]   （2 つ以上渡すと 1 本目＝基線として比べる）
# 出す物：種ごとの 1783〜84 年の蒸気機関の解禁（英以外）の数・清の最初の年・主要国の蒸気機関と機関車の最初の年の中央値・1950 年までに蒸気機関を解禁する種の数・1783 年の全行の解禁の数
import sys,glob,os,re,statistics,collections
MAJORS=["フランス","プロイセン","オーストリア","ロシア","アメリカ","スペイン","オスマン","ネーデルラント","日本","清"]
def load(d):
    out={}
    for f in sorted(glob.glob(os.path.join(d,'disc_s*.tsv'))):
        sd=int(re.search(r'disc_s(\d+)\.tsv',f).group(1))
        rows=[]
        with open(f,encoding='utf-8') as fh:
            hdr=fh.readline().rstrip('\n').split('\t')
            for l in fh:
                p=l.rstrip('\n').split('\t')
                if len(p)!=len(hdr): continue
                rows.append(dict(zip(hdr,p)))
        out[sd]=rows
    return out
def first_year(rows,nation,row):
    ys=[int(r['year']) for r in rows if r['kind']=='E' and r['nation']==nation and r['row']==row]
    return min(ys) if ys else None
def summarize(d):
    data=load(d); seeds=sorted(data)
    res={'seeds':seeds,'n84':{},'n88':{},'n92':{},'a83':{},'qing':{},'steam':{},'loco':{}}
    for sd in seeds:
        rows=data[sd]
        res['n84'][sd]=len({r['nation'] for r in rows if r['kind']=='E' and r['row']=='蒸気機関' and int(r['year'])<=1784 and r['nation']!='イギリス'})
        for key,yr in (('n88',1788),('n92',1792)):
            res[key][sd]=len({r['nation'] for r in rows if r['kind']=='E' and r['row']=='蒸気機関' and int(r['year'])<=yr and r['nation']!='イギリス'})
        res['a83'][sd]=sum(1 for r in rows if r['kind']=='E' and int(r['year'])==1783)
        res['qing'][sd]=first_year(rows,'清','蒸気機関')
        for nm in MAJORS:
            res['steam'][(nm,sd)]=first_year(rows,nm,'蒸気機関'); res['loco'][(nm,sd)]=first_year(rows,nm,'機関車')
    return res
def med(xs):
    xs=[x for x in xs if x is not None]
    return statistics.median(xs) if xs else None
def show(name,res):
    seeds=res['seeds']
    print(f'## {name}（{len(seeds)}種）')
    print('種 | 蒸気機関 1783-84 の解禁（英以外） | 〜1788 | 〜1792 | 1783 の全行の解禁 | 清の蒸気機関の最初の年')
    for sd in seeds: print(f"{sd} | {res['n84'][sd]} | {res['n88'][sd]} | {res['n92'][sd]} | {res['a83'][sd]} | {res['qing'][sd]}")
    print('国 | 蒸気機関の最初の年（種の中央値） | 〜1950 に解禁した種 | 機関車の最初の年（種の中央値） | 〜1950 に解禁した種 | 機関車≦蒸気機関の種')
    for nm in MAJORS:
        st=[res['steam'][(nm,sd)] for sd in seeds]; lo=[res['loco'][(nm,sd)] for sd in seeds]
        le=sum(1 for a,b in zip(st,lo) if a is not None and b is not None and b<=a)
        print(f"{nm} | {med(st)} | {sum(1 for x in st if x)}/{len(seeds)} | {med(lo)} | {sum(1 for x in lo if x)}/{len(seeds)} | {le}")
if __name__=='__main__':
    ds=sys.argv[1:]
    rs=[summarize(d) for d in ds]
    for d,r in zip(ds,rs): show(d,r)
    if len(rs)>=2:
        b=rs[0]; print('\n## 基線との比（1 本目＝基線）')
        for d,r in zip(ds[1:],rs[1:]):
            ok=sum(1 for sd in b['seeds'] if sd in r['n84'] and r['n84'][sd]*3<=b['n84'][sd])
            q=sum(1 for sd in r['seeds'] if r['qing'][sd] is not None and r['qing'][sd]<=1784)
            ok92=sum(1 for sd in b['seeds'] if sd in r['n92'] and r['n92'][sd]*2<=b['n92'][sd])
            print(f'{d}: L1（1783-84 の解禁が基線の 1/3 以下の種）{ok}/{len(r["seeds"])}・L1b（〜1792 が基線の 1/2 以下の種）{ok92}/{len(r["seeds"])}・L2（清が 1783-84 の種）{q}/{len(r["seeds"])}')
            early=0
            for nm in MAJORS:
                if nm=='清': continue
                for row in ('steam','loco'):
                    mb=med([b[row][(nm,sd)] for sd in b['seeds']]); ma=med([r[row][(nm,sd)] for sd in r['seeds']])
                    if mb is not None and ma is not None and ma<=mb-5: early+=1; print(f'  L3 早まり {nm} {row}: {mb}→{ma}')
            print(f'  L3（5 年以上早まった国×行）{early}')
            bad4 = 0
            for nm in MAJORS:
                if nm in ('清', '日本'): continue
                cb = sum(1 for sd in b['seeds'] if b['steam'][(nm, sd)] is not None); ca = sum(1 for sd in r['seeds'] if r['steam'][(nm, sd)] is not None)
                mb = med([b['steam'][(nm, sd)] for sd in b['seeds']]); ma = med([r['steam'][(nm, sd)] for sd in r['seeds']])
                flag = ''
                if ca < cb - 2: flag += ' 〜1950 の種が基線から −2 を超えて減った'
                if mb is not None and ma is not None and abs(ma - mb) > 15: flag += ' 中央値が基線の ±15 年の外'
                if flag: bad4 += 1
                print(f'  L4 {nm}: 〜1950 に解禁した種 {cb}→{ca}・最初の年の中央値 {mb}→{ma}{flag}')
            print(f'  L4（外れた国）{bad4}')
