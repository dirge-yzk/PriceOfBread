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
 ('M3 Make を 3 国に（B12 を赤にする）',[('InstrumentsTests.cs',mk,"        NationConfig[] cfgs = [NationConfig.France(), NationConfig.Prussia(), uk];")],'FullyQualifiedName~軍事費の施行の一歩'),
 ('M4 Make を 2 国に・B12 の Assert を外す（B11 を赤にする）',[('InstrumentsTests.cs',mk,"        NationConfig[] cfgs = [NationConfig.France(), NationConfig.Prussia()];"),('InstrumentsTests.cs',B12off,'            if (false) Assert.True(sim.Nations.Count >= 4,')],'FullyQualifiedName~富裕層のお金の行き先'),
 ('M5 B5 の計器を 1 国だけに挙げる',[('InstrumentsTests.cs','''            var sim = Make();
            Instruments.Configure(dir, "*");
            try { RunMatch(sim, WarQ, instr: true); }''','''            var sim = Make();
            Instruments.Configure(dir, "フランス");
            try { RunMatch(sim, WarQ, instr: true); }''')],'FullyQualifiedName~軍事費の施行の一歩'),
 ('M6 B8 の計器を 1 国だけに挙げる',[('InstrumentsTests.cs','''            Instruments.Configure(dir, "*");
            try { RunMatch(sim, 40, instr: true); }''','''            Instruments.Configure(dir, "フランス");
            try { RunMatch(sim, 40, instr: true); }''')],'FullyQualifiedName~幽霊戦線の列'),
 ('M7 B9 の英を添字 2 から外す（Make の Prussia と uk の順を入れ替える）',[('InstrumentsTests.cs',mk,"        NationConfig[] cfgs = [NationConfig.France(), uk, NationConfig.Prussia(), NationConfig.Austria()];")],'FullyQualifiedName~借入の門の周りの列'),
 ('M8 B13 のイギリスの行を飛ばす',[('CearnInstrumentTests.cs','''            foreach (var n in sim.Nations)
                foreach (var r in n.World.Regions)
                {
                    if (r.Tier != RegionTier.Colonial) continue;''','''            foreach (var n in sim.Nations)
                foreach (var r in n.World.Regions)
                {
                    if (r.Tier != RegionTier.Colonial || Instruments.NameOf(n.World) == "イギリス") continue;''')],'FullyQualifiedName~群の表は州の台帳の和に合い'),
 ('M9 B2 の四半期を回す間、越境を起こす国をフランスとプロイセンだけにする',[('RouteKmTests.cs','new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()]','new MultiNationSim([NationConfig.France(), NationConfig.Prussia()]')],'FullyQualifiedName~陸路は貿易の経路の表と一致し'),
]
out=open('D:/PoB_work/c88ts_work/mut_ts.log','w',encoding='utf-8')
for name,edits,flt in muts:
    restore(); apply(edits)
    r=subprocess.run(['dotnet','test','D:/PoB_work/wt_c88ts/PriceOfBread.Tests','-c','Release','--artifacts-path','D:/PoB_work/c88ts_art_main','--filter',flt],capture_output=True,text=True,encoding='utf-8',errors='replace',cwd='D:/PoB_work/wt_c88ts')
    lines=[l.strip()[:230] for l in r.stdout.splitlines() if ('失敗 ' in l or '対照' in l or '前提が崩れた' in l or 'error' in l or '成功!' in l or '失敗!' in l)]
    out.write('== '+name+' rc=%d\n'%r.returncode+'\n'.join(lines[:8])+'\n'); out.flush()
restore()
out.write('restored\n'); out.close()
