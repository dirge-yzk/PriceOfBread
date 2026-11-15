# (1163) U5 の帰属：四半期 79 の平時の中核の州を、CTL で外国の取り分≧0.01 の州とそれ以外に分け、持ち主の取り分の平均を CTL と M57 で並べる（種 1・2）
import collections, statistics as st
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
A={'CTL':'D:/PoB_work/keep_1157_rej/c113cr_mut/CTL/ins','M57':'D:/PoB_work/keep_1157/base_ins'}
def load(d,s):
    war=set()
    with open(f'{d}/nation_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t');ix={k:j for j,k in enumerate(h)}
        for l in f:
            c=l.rstrip('\n').split('\t')
            if c[0]=='79' and fl(c[ix['atWarMonths']])>0: war.add(c[ix['nation']])
    r={}
    with open(f'{d}/rulepower_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t');ix={k:j for j,k in enumerate(h)}
        for l in f:
            c=l.rstrip('\n').split('\t');q=int(c[0])
            if q>79: break
            if q==79 and c[ix['tier']]=='Core' and c[ix['nation']] not in war: r[c[ix['region']]]=(c[ix['nation']],fl(c[ix['nowRule0Share']]),fl(c[ix['nowRule1Share']]),fl(c[ix['nowRule2Share']]),fl(c[ix['nowRule3Share']]))
    return r
for s in (1,2):
    c=load(A['CTL'],s); m=load(A['M57'],s)
    both=[k for k in c if k in m and c[k][0]==m[k][0]]
    for lab,sel in (('外国≧0.01',[k for k in both if c[k][3]>=0.01]),('外国＜0.01',[k for k in both if c[k][3]<0.01])):
        if not sel: continue
        mean=lambda d,i: st.mean(d[k][i] for k in sel)
        print(f'種 {s} {lab} 州 {len(sel)}：持ち主 CTL {mean(c,1):.3f} M57 {mean(m,1):.3f}・在地 {mean(c,2):.3f}/{mean(m,2):.3f}・外国 {mean(c,3):.3f}/{mean(m,3):.3f}・無統治 {mean(c,4):.3f}/{mean(m,4):.3f}')
