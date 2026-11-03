import sys,subprocess,shutil,os,io
SRC='D:/PoB_work/wt_c11h16'; MT='D:/PoB_work/c11h16/mut_tree'
def rd(p):
    b=open(p,'rb').read(); return b.decode('utf-8-sig'), b.startswith(b'\xef\xbb\xbf')
def wr(p,s,bom): open(p,'wb').write((b'\xef\xbb\xbf' if bom else b'')+s.encode('utf-8'))
MUT={
 'M1_threshold_div12_removed':('Fiscal.cs','Calendar.PerYearToPerMonth(distrustThresholdPerMonthGdp)','distrustThresholdPerMonthGdp'),
 'M2_month_year_reversed':('Fiscal.cs','Calendar.PerMonthToPerYear(new MoneyPerMonth(nominalGdpPerMonth))','new MoneyPerYear(nominalGdpPerMonth / Calendar.MonthsPerYear)'),
 'M3_threshold_times12':('Fiscal.cs','Calendar.PerYearToPerMonth(distrustThresholdPerMonthGdp)','distrustThresholdPerMonthGdp * Calendar.MonthsPerYear'),
 'M4_gt_to_ge':('Fiscal.cs','              > Calendar.PerYearToPerMonth(distrustThresholdPerMonthGdp);','              >= Calendar.PerYearToPerMonth(distrustThresholdPerMonthGdp);'),
 'M5_guard_removed':('Fiscal.cs','=> nominalGdpPerMonth > 1e-9\n           && Calendar','=> Calendar'),
 'M6_selfplay_old_direct_form':('SelfPlay.cs','Fiscal.CurrencyMemoryBroken(w.CumulativeDevaluation, w.NominalGdpPerMonth.Value, w.Coef.CapitalFlightDistrustThreshold)','(w.NominalGdpPerMonth.Value > 1e-9 && w.CumulativeDevaluation / w.NominalGdpPerMonth.Value > w.Coef.CapitalFlightDistrustThreshold)'),
}
names=sys.argv[1:] or list(MUT)
out=io.open('D:/PoB_work/c11h16/mutation_out.txt','w',encoding='utf-8')
if not os.path.isdir(MT):
    subprocess.run(['tar','-cf','D:/PoB_work/c11h16/mt.tar','-C',SRC,'--exclude=bin','--exclude=obj','--exclude=.git','--exclude=docs','--exclude=measurements','--exclude=uishot','--exclude=.vs','.'],check=True)
    os.makedirs(MT); subprocess.run(['tar','-xf','D:/PoB_work/c11h16/mt.tar','-C',MT],check=True)
for n in names:
    for f in ('Fiscal.cs','SelfPlay.cs'): shutil.copyfile(f'{SRC}/PriceOfBread.SimCore/{f}',f'{MT}/PriceOfBread.SimCore/{f}')
    f,old,new=MUT[n]; p=f'{MT}/PriceOfBread.SimCore/{f}'; s,bom=rd(p)
    assert s.count(old)==1,(n,s.count(old)); wr(p,s.replace(old,new),bom)
    r=subprocess.run(['dotnet','test',f'{MT}/PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path','D:/PoB_work/c11h16/art_mut','--disable-build-servers','--filter','FullyQualifiedName~CurrencyMemoryBrokenTests'],capture_output=True,text=True,encoding='utf-8',errors='replace')
    tail=[l for l in r.stdout.splitlines() if ('失敗!' in l or '成功!' in l or 'error' in l.lower()[:200] and 'CS' in l)]
    failed=[l.strip() for l in r.stdout.splitlines() if l.strip().startswith('失敗 ') or 'Failed ' in l[:12]]
    out.write(f'{n}\trc={r.returncode}\t'+' | '.join(tail[:3])+'\n'); 
    for l in failed[:8]: out.write('    '+l+'\n')
    out.flush()
out.close()
