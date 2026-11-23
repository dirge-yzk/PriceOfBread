import subprocess, sys, os, shutil
R='D:/PoB_work/wt_c138sm'
OUT='D:/PoB_work/c138sm_work/mut_out3.txt'
def rd(p): return open(os.path.join(R,p),'rb').read().decode('utf-8')
def wr(p,s): open(os.path.join(R,p),'wb').write(s.encode('utf-8'))
BS=chr(92)
FIL_HOOK='FullyQualifiedName~InstrumentHookClosureTests.UnselectedTablesAccumulateNothing|FullyQualifiedName~InstrumentHookClosureTests.OpenStoresAreExactlyTheNamedOnes|FullyQualifiedName~InstrumentHookClosureTests.NationgoodGroupWithOutputIsByteIdenticalToFullRun'
M=[
 ('M0 無変更（基準）', None, None, None, 'FullyQualifiedName~InstrumentHookClosureTests.UnselectedTablesAccumulateNothing|FullyQualifiedName~InstrumentHookClosureTests.OpenStoresAreExactlyTheNamedOnes|FullyQualifiedName~InstrumentHookClosureTests.NationgoodGroup|FullyQualifiedName~InstrumentColumnTests|FullyQualifiedName~ArrearsUnderLimitPrevMonthTests|FullyQualifiedName~CapSkipSellerPotentialTests|FullyQualifiedName~国債台帳'),
 ('M1 BondHoldings を保存から外す（NonSerialized）','PriceOfBread.SimCore/Model.cs','    public MoneyStock BondHoldings;','    [System.NonSerialized] public MoneyStock BondHoldings;','FullyQualifiedName~国債台帳_同じ版'),
 ('M2 dedupBase に dedup を入れる','PriceOfBread.SimCore/Instruments/InstrumentsCapskip.cs','a[g, CkDedupBase] += baseW;','a[g, CkDedupBase] += dedupW;','FullyQualifiedName~CapSkipSellerPotentialTests.T7_計器'),
 ('M3a CpIter の閉じを外す','PriceOfBread.SimCore/Instruments/InstrumentsCpeace.cs','internal static void CpIter(War war, bool collapsed) { if (!Tg.Cede) return; ','internal static void CpIter(War war, bool collapsed) { ',FIL_HOOK),
 ('M3b CpMonth の閉じを外す','PriceOfBread.SimCore/Instruments/InstrumentsCpeace.cs','int year) { if (!Tg.Cede && !Tg.War) return; ','int year) { ',FIL_HOOK),
 ('M3c HabitLgStep の閉じを外す','PriceOfBread.SimCore/Instruments/InstrumentsHabitlg.cs','        if (!Tg.Habitlg) return;\n        var a = HabitlgAccs.GetValue','        var a = HabitlgAccs.GetValue','FullyQualifiedName~DirectEntriesLeaveNoKey'),
 ('M3d NgRegionMoved の閉じを外す','PriceOfBread.SimCore/Instruments/InstrumentsNationgood.cs','Region r) { if (!Tg.BuyShadow) return; if (from','Region r) { if (from','FullyQualifiedName~DirectEntriesLeaveNoKey'),
 ('M3e PxstepTable の閉じを外す','PriceOfBread.SimCore/Instruments/InstrumentsPxstep.cs','int q) { if (!Tg.Pxstep) return; PxSinks','int q) { PxSinks',FIL_HOOK),
 ('M4 soldierK を人口全体にする','PriceOfBread.SimCore/Instruments/InstrumentsRegionProd.cs','if (p.IsSoldier) soldiers += p.Size;','soldiers += p.Size;','FullyQualifiedName~InstrumentColumnTests'),
 ('M5 前の月の比を今の月の比にする','PriceOfBread.SimCore/Instruments/InstrumentsArrears.cs','double prevRatio = s.PrevRev > 0 ? s.PrevInt / s.PrevRev : double.NaN;','double prevRatio = rev > 0 ? intr / rev : double.NaN;','FullyQualifiedName~ArrearsUnderLimitPrevMonthTests.T6'),
]
only=sys.argv[1:]
env=dict(os.environ); env['PATH']='/usr/bin:/c/Program Files/dotnet:'+env['PATH']; env['TEMP']=env['TMP']='D:/PoB_work/c138sm_work/tmp'
with open(OUT,'a',encoding='utf-8') as o:
    for name,f,old,new,fil in M:
        if only and name.split()[0] not in only: continue
        orig=None
        if f:
            orig=rd(f)
            assert orig.count(old)==1,(name,orig.count(old))
            wr(f,orig.replace(old,new))
        try:
            r=subprocess.run(['dotnet','test',R+'/PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path','D:/PoB_work/c138sm_art','--disable-build-servers','--filter',fil],capture_output=True,text=True,encoding='utf-8',errors='replace',env=env)
            txt=r.stdout
            res=[l.strip() for l in txt.splitlines() if ('失敗!' in l or '成功!' in l or 'エラー CS' in l)]
            fails=sorted(set(l.split('PriceOfBread.')[-1].split(' ')[0] for l in txt.splitlines() if l.startswith('  失敗 ')))
            o.write(f'{name}\trc={r.returncode}\t{res[:1]}\tFAIL={fails}\n'); o.flush()
        finally:
            if f: wr(f,orig)
o=open(OUT,'a'); o.write('MUTDONE\n'); o.close()
