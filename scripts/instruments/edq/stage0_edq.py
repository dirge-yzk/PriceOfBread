# (680) 線 edq 段0 の読み手（読むだけ・判定ではない）。
# 問い：姿勢（Modernize／Industrialize／Militarize）が毎四半期 `p.EducationLaw = Public` を書く形で、
#       〈現行法が Public より上（Compulsory）なのに目標を Public へ書き戻す〉国×四半期（＝撤回の願いの置き直し）は世界の中に何件あるか。
# 入力：計器つき 10 種の ins フォルダ（reform_s<種>.tsv の educationLawCode＝四半期末の現行法 0〜3、modsig_s<種>.tsv の stance＝Apply が選んだ姿勢）。
# 出力：種ごとと合計の ①行数 ②現行法の分布 ③姿勢の分布 ④書き手の姿勢の割合 ⑤置き直しの件数（現行法 3 かつ書き手の姿勢）⑥現行法の変化（国ごとの隣の四半期との差）の件数。
# 書き手の姿勢＝Modernize(0)・Industrialize(1)・Militarize(4。MilitarizeEducationDrive=1.0 の既定で書く＝(772) c6sp で焼き込み済み・欄は墓標)。Welfare(2)・Fiscal(3)・Apply が走らない(-1)は書かない。
# 使い方：PYTHONUTF8=1 python stage0_edq.py <ins フォルダ> [種の数=10] [書き手に Militarize を含める=1]
import csv,sys,collections
sys.stdout.reconfigure(encoding='utf-8')
D=(sys.argv[1] if len(sys.argv)>1 else 'D:/PoB_work/keep_675/m675_ins').rstrip('/')+'/'
NS=int(sys.argv[2]) if len(sys.argv)>2 else 10
MIL=int(sys.argv[3]) if len(sys.argv)>3 else 1
S0=int(sys.argv[4]) if len(sys.argv)>4 else 1
WRITERS={0,1}|({4} if MIL else set())
SN={0:'Modernize',1:'Industrialize',2:'Welfare',3:'Fiscal',4:'Militarize',-1:'noApply'}
tot=collections.Counter(); law=collections.Counter(); st=collections.Counter(); rewrite=0; writer_rows=0; rows=0; changes=0; join_miss=0; w3=0
for s in range(S0,S0+NS):
    L={}
    with open(D+f'reform_s{s}.tsv',encoding='utf-8') as f:
        rd=csv.reader(f,delimiter='\t'); h=next(rd); iq=h.index('quarter'); inn=h.index('nation'); ic=h.index('educationLawCode')
        for r in rd: L[(int(r[iq]),r[inn])]=int(r[ic])
    S={}
    with open(D+f'modsig_s{s}.tsv',encoding='utf-8') as f:
        rd=csv.reader(f,delimiter='\t'); h=next(rd); iq=h.index('quarter'); inn=h.index('nation'); i=h.index('stance')
        for r in rd: S[(int(r[iq]),r[inn])]=int(r[i])
    nat=set(n for (_,n) in L)
    for (q,n),c in L.items():
        rows+=1; law[c]+=1
        if (q,n) not in S: join_miss+=1; continue
        k=S[(q,n)]; st[k]+=1
        if k in WRITERS:
            writer_rows+=1
            if c>=3: rewrite+=1
        if c>=3: w3+=1
        if (q-1,n) in L and L[(q-1,n)]!=c: changes+=1
print('入力',D,'種',NS,'書き手',sorted(SN[k] for k in WRITERS))
print('行数(国×四半期)',rows,'  姿勢の欠け(結合できず)',join_miss)
print('現行法の分布(0=なし 1=宗教 2=公教育 3=義務)',dict(sorted(law.items())))
print('姿勢の分布',{SN[k]:v for k,v in sorted(st.items())})
tt=sum(st.values()); print('書き手の姿勢の行',writer_rows,'割合 %.4f'%(writer_rows/tt if tt else float('nan')))
print('現行法が義務(3)の行',w3)
print('★置き直しの件数(現行法 3 かつ書き手の姿勢＝毎四半期 Public を書く行)',rewrite)
print('現行法の変化(隣の四半期との差のある国×四半期)',changes)
