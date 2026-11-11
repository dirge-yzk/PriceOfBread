# (1039) 旗 0 の生で新しい列を読む（記録・門にしない）：G3 型（実際が −2×(0.5＋期待) に合う行の割合）が旗 0 で落ちることなど
import glob,csv,sys
d=sys.argv[1]
n=0;clerg=0;pos=0;g3=0;g4=0;down_n=0;pref_not_m1=0;dn_pos=0;rho_bad=0;mid_pos=0;gain_pos=0;cut_pos=0
for f in sorted(glob.glob(d+'/lordstat_s*.tsv')):
    for x in csv.DictReader(open(f,encoding='utf-8'),delimiter='\t'):
        n+=1
        up=float(x['relStakeUpExp']);dn=float(x['relStakeDownExp']);pu=float(x['relPrefUpAct']);pd=float(x['relPrefDownAct'])
        if float(x['relClergyIncome0'])>0: clerg+=1
        if up>0: pos+=1
        if dn>0: dn_pos+=1; 
        if abs(pu-(-2*(0.5+up)))<=1e-9: g3+=1
        if abs(pd-(-2*(0.5+dn)))<=1e-9: g4+=1
        if pu!=-1.0 or pd!=-1.0 or x['d2PrefBurUpAct']!=x['d2PrefBurUpAct'] : pref_not_m1+=1
        r=float(x['d2Rho']);
        if not(0<=r<=1): rho_bad+=1
        if float(x['d2SMidBurExp'])>0 or float(x['d2SMidLandExp'])>0: mid_pos+=1
        if float(x['d2SGainBurExp'])>0 or float(x['d2SGainLandExp'])>0: gain_pos+=1
        if float(x['d2CutOverIncome0'])>0: cut_pos+=1
print(dict(rows=n,clergy_rows=clerg,stakeUp_pos=pos,stakeDown_pos=dn_pos,G3_match_frac=round(g3/n,4),G4_match_frac=round(g4/n,4),pref_not_minus1=pref_not_m1,rho_out_of_range=rho_bad,smid_pos=mid_pos,sgain_pos=gain_pos,cut_pos=cut_pos))
