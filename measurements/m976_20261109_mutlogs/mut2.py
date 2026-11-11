import subprocess,shutil,sys
T='D:/PoB_work/wt_c88ts/PriceOfBread.Tests/'; S='D:/PoB_work/c88ts_work/save2/'
files=['InstrumentsTests.cs','RouteKmTests.cs','CearnInstrumentTests.cs']
def restore():
    for f in files: shutil.copy(S+f,T+f)
def apply(edits):
    for f,o,n in edits:
        b=open(T+f,'rb').read().decode(); assert o in b,(f,o[:50]); open(T+f,'wb').write(b.replace(o,n,1).encode())
mk="""        NationConfig[] cfgs = [NationConfig.France(), NationConfig.Prussia(), uk, NationConfig.Austria()];"""
B12off='            Assert.True(sim.Nations.Count >= 4,'
muts=[
 ('M5b B5：Make を 2 国に・B12 の Assert を外す・計器は全国',[('InstrumentsTests.cs',mk,"        NationConfig[] cfgs = [NationConfig.France()];"),('InstrumentsTests.cs',B12off,'            if (false) Assert.True(sim.Nations.Count >= 4,')],'FullyQualifiedName~軍事費の施行の一歩'),
 ('M6b B8：Make を 1 国に・B12 の Assert を外す',[('InstrumentsTests.cs',mk,"        NationConfig[] cfgs = [NationConfig.France()];"),('InstrumentsTests.cs',B12off,'            if (false) Assert.True(sim.Nations.Count >= 4,')],'FullyQualifiedName~幽霊戦線の列'),
]
out=open('D:/PoB_work/c88ts_work/mut_ts2.log','w',encoding='utf-8')
for name,edits,flt in muts:
    restore(); apply(edits)
    r=subprocess.run(['dotnet','test','D:/PoB_work/wt_c88ts/PriceOfBread.Tests','-c','Release','--artifacts-path','D:/PoB_work/c88ts_art_main','--filter',flt],capture_output=True,text=True,encoding='utf-8',errors='replace',cwd='D:/PoB_work/wt_c88ts')
    lines=[l.strip()[:230] for l in r.stdout.splitlines() if ('失敗 ' in l or '対照' in l or '前提が崩れた' in l or 'error' in l or '成功!' in l or '失敗!' in l)]
    out.write('== '+name+' rc=%d\n'%r.returncode+'\n'.join(lines[:8])+'\n'); out.flush()
restore()
out.write('restored\n'); out.close()
