import sys,hashlib,subprocess,os,shutil
R='D:/PoB_work/wt_fx/'
MUT=[
 ('M1 旗1以上でも max を通さない','PriceOfBread.SimCore/SelfPlay.cs','Math.Max(s, InterestBurdenStressOf(w))','s'),
 ('M2 専売の段の旗の条件 >=2 を >=1 に','PriceOfBread.SimCore/SelfPlay.cs','AiInterestBurdenResponse >= 2 ? disciplineStress','AiInterestBurdenResponse >= 1 ? disciplineStress'),
 ('M3a 窃盗の旗を見ない（常に旧）','PriceOfBread.SimCore/Crime.cs','c.CrimeVictimBasis > 0 ?','c.CrimeVictimBasis < 0 ?'),
 ('M3b 窃盗の旗を見ない（常に新）','PriceOfBread.SimCore/Crime.cs','c.CrimeVictimBasis > 0 ?','c.CrimeVictimBasis > -1 ?'),
 ('M4 重さの分母を利払いでなく歳入に','PriceOfBread.SimCore/SelfPlay.cs','excess > 0 ? excess / interest : 0.0','excess > 0 ? excess / (w.LastRevenuePerMonth.Value + 1.0) : 0.0'),
 ('M5 重さの線を係数でなく 0.5 に固定','PriceOfBread.SimCore/SelfPlay.cs','double excess = interest - w.Coef.MaxInterestShareOfRevenue.Value * w.LastRevenuePerMonth.Value;','double excess = interest - 0.5 * w.LastRevenuePerMonth.Value;'),
]
def md5(p): return hashlib.md5(open(p,'rb').read()).hexdigest()
for name,rel,a,b in MUT:
    p=R+rel; orig=open(p,'rb').read(); h0=md5(p)
    s=orig.decode('utf-8'); assert s.count(a)==1,(name,s.count(a))
    open(p,'wb').write(s.replace(a,b).encode('utf-8'))
    try:
        r=subprocess.run(['dotnet','test','PriceOfBread.Tests','-c','Release','--artifacts-path','D:/PoB_work/fx_319/art','--filter','FullyQualifiedName~FxChainTests'],cwd=R,capture_output=True,text=True,env=dict(os.environ,PATH='/c/Program Files/dotnet:'+os.environ['PATH']))
        out=r.stdout
        line=[l for l in out.splitlines() if l.startswith('成功!') or l.startswith('失敗!')]
        failed=[l.strip() for l in out.splitlines() if '[FAIL]' in l]
        print(name,'→',line[-1] if line else 'ビルド失敗か結果なし',' / 赤:',len(failed),flush=True)
        for f in failed[:4]: print('   ',f[:140],flush=True)
    finally:
        open(p,'wb').write(orig)
        print('  復元 md5一致:',md5(p)==h0,flush=True)
