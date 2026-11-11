# (1039) 突然変異の台本：mut.py <名前>  ＝ 壊す → 狙いの試験だけ走らせる → 出力を measurements/ へ → 元へ戻す（git checkout）
import subprocess, sys, pathlib
R = pathlib.Path('D:/PoB_work/wt_c98da')
SC = 'PriceOfBread.SimCore/'
M = {
 'a_step_shift': (SC+'Privileges.cs', ': s switch {', ': (StateReligionLaw)((int)s + 1) switch {', 'FullyQualifiedName~LordstatD1aTests.D1a_1|FullyQualifiedName~LordstatInstrumentTests.GL_'),
 'b_copy_forgets': (SC+'PolicyPreferences.cs', 'Tl = Tl, ClergyInc0 = ClergyInc0,', 'Tl = Tl,', 'FullyQualifiedName~LordstatD1aTests.D1a_3'),
 'b2_sum_forgets': (SC+'PolicyPreferences.cs', 'if (b == 0 && p.ClassId == PopClass.Clergy) o.ClergyInc0 += inc;', '', 'FullyQualifiedName~LordstatD1aTests.D1a_3|FullyQualifiedName~LordstatD1aTests.D1a_4'),
 'c_engine_call': (SC+'InstrumentsLordstat.cs', 'double cutShare = inc0 > 1e-9 ? w.LastRuleCut / inc0 : 0.0,', 'double cutShare = inc0 > 1e-9 ? PolicyPreferences.LordIncomeLoss(w, PolicyAxis.LandReform, 1.0) * 0 + w.LastRuleCut / inc0 : 0.0,', 'FullyQualifiedName~LordstatInstrumentTests.L10'),
 'd_flag0_path': (SC+'PolicyPreferences.cs', 'if (trad && w.Coef.LordReligionStake > 0) return -1.0;', 'if (trad) return -1.0001;', 'FullyQualifiedName~LordstatD1aTests.D1a_2'),
 'e_rho_no_f': (SC+'InstrumentsLordstat.cs', 'rhoNum += pop * r.Rule[1] * fl;', 'rhoNum += pop * r.Rule[1];', 'FullyQualifiedName~LordstatD1aTests.D1a_5'),
}
name = sys.argv[1]; f, a, b, flt = M[name]
p = R/f; s = p.read_bytes().decode('utf-8'); assert s.count(a) == 1, (name, s.count(a))
p.write_bytes(s.replace(a, b).encode('utf-8'))
try:
    out = subprocess.run(['dotnet','test','PriceOfBread.Tests','-c','Release','--artifacts-path','D:/PoB_work/c98da_art','--filter',flt], cwd=R, capture_output=True, text=True, encoding='utf-8', errors='replace')
    txt = out.stdout + out.stderr
finally:
    subprocess.run(['git','checkout','--',f], cwd=R)
keep = [l for l in txt.splitlines() if ('失敗' in l or '成功' in l or 'error' in l.lower().split(':')[0:1] or ' error ' in l or 'Assert' in l or 'Expected' in l or 'Actual' in l)]
(R/'measurements'/f'm1039_20261111_mut_{name}.txt').write_text(f'変異 {name}: {f} の {a!r} を {b!r} へ\nフィルタ: {flt}\n終了コード: {out.returncode}\n--- 出力（失敗・成功・エラーの行）---\n' + '\n'.join(keep) + '\n--- 出力の全文 ---\n' + txt, encoding='utf-8')
print(name, 'rc', out.returncode, [l for l in txt.splitlines() if '失敗!' in l or '成功!' in l])
