# (490) 突然変異の駆動（記録）：新しい番人（OpeningBakeTests・AgeLedgerTests・PopulationInstrumentTests の列名・接触の内訳の uid）に、1 か所ずつ壊して赤を確かめる。
#   使い方：POB_WORK=D:/PoB_work/<回> python mut_490.py [M1 M2 ...]（作業ツリーの 1 ファイルを書き換えて隔離ビルド→対象のテストを回し、必ず元へ戻す）。（旧の注：パスは D:/PoB_work/ の固定〈当時の作業フォルダ）。結果は measurements/hz4_490_20261021_mutations.txt。
import subprocess, shutil, os, sys, time
# (499) 木と作業フォルダを環境変数で渡す（旧は wt_hz4 の固定）：POB_REPO＝書き換える作業ツリー（既定＝この scripts の 3 つ上）／POB_WORK＝作業フォルダ（必須・D: 側）
R = os.environ.get('POB_REPO', os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))).replace(chr(92), '/').rstrip('/') + '/'
W = os.environ['POB_WORK'].replace(chr(92), '/').rstrip('/') + '/'
ART = W + 'build_m'
BAK = W + 'mut_bak/'
os.makedirs(BAK, exist_ok=True)
BS = chr(92)

# (名前, ファイル, 旧, 新, テストの filter)
M = [
 ('M1 読み込みが貯蓄を書かない', 'PriceOfBread.SimCore/OpeningBake.cs',
  'for (int i = 0; i < r.Pops.Count; i++) r.Pops[i].ApplyBakedSavings(po[i][1].GetDouble());', 'for (int i = 0; i < r.Pops.Count; i++) { }', 'OpeningBakeTests'),
 ('M2 読み込みが在庫を書かない', 'PriceOfBread.SimCore/OpeningBake.cs',
  'r.Markets[g].Stock = mk[2 * g + 1].GetDouble();', '', 'OpeningBakeTests'),
 ('M3 読み込みが検証しない', 'PriceOfBread.SimCore/OpeningBake.cs',
  'var errs = Validate(sim, doc);\n        if (errs.Count > 0)', 'var errs = new List<string>();\n        if (errs.Count > 0)', 'OpeningBakeTests'),
 ('M4 検証が生産者の財を見ない', 'PriceOfBread.SimCore/OpeningBake.cs',
  ' || row[0].GetInt32() != r.Producers[i].GoodId', '', 'OpeningBakeTests'),
 ('M5 検証が階級の名前を見ない', 'PriceOfBread.SimCore/OpeningBake.cs',
  ' || po[i][0].GetString() != r.Pops[i].ClassName', '', 'OpeningBakeTests'),
 ('M6 検証が州の名前を見ない', 'PriceOfBread.SimCore/OpeningBake.cs',
  'foreach (var x in worldR.Except(tableR, StringComparer.Ordinal)) errs.Add($"{nk} の世界の州 {x} が表に無い");', '', 'OpeningBakeTests'),
 ('M7 焼く腕が能力の凍結を外さない', 'PriceOfBread.SimCore/EconomyStep.cs',
  'world.BakeRun?.ThawCapacity != true;', 'true;', 'OpeningBakeTests'),
 ('M8 焼く腕が社会移動を止めない', 'PriceOfBread.SimCore/Simulation.cs',
  'if (!(warm && World.BakeRun?.StopSocialMobility == true)) Meritocracy.Step', 'if (true) Meritocracy.Step', 'OpeningBakeTests'),
 ('M9 焼く腕が階級遷移を止めない', 'PriceOfBread.SimCore/Simulation.cs',
  'if (r.State == RegionState.Active && !(World.WarmupFrozen && World.BakeRun?.StopPopDecision == true))', 'if (r.State == RegionState.Active)', 'OpeningBakeTests'),
 ('M10 台帳の件数が層の数−8でない', 'PriceOfBread.SimCore/EconomyStep.cs',
  '(Pop.Layout.Bands - 8); }   // ★(490)', '(Pop.Layout.Bands); }   // ★(490)', 'AgeLedgerTests'),
 ('M11 pop_s の列の名前が層の番号', 'PriceOfBread.SimCore/InstrumentsPop.cs',
  'internal static string PopBandTag(AgeLayout l, int b) => ((int)l.Lower[b]).ToString(System.Globalization.CultureInfo.InvariantCulture);',
  'internal static string PopBandTag(AgeLayout l, int b) => b.ToString(System.Globalization.CultureInfo.InvariantCulture);', 'PopulationInstrumentTests'),
 ('M12 disc_s の nationUid が 0', 'PriceOfBread.SimCore/Instruments.cs',
  "row.Substring(tab) + '" + BS + "t' + I(st.Uid)); }", "row.Substring(tab) + '" + BS + "t' + I(0)); }", 'FullyQualifiedName~接触の内訳'),
 ('M13 disccontact_s の buyerUid が 0', 'PriceOfBread.SimCore/Instruments.cs',
  'Catalog.Goods[cgood].Name, F(v), I(st.Uid), I(S(sellerW).Uid)));', 'Catalog.Goods[cgood].Name, F(v), I(0), I(S(sellerW).Uid)));', 'FullyQualifiedName~接触の内訳'),
 ('M18 disccontact_s の sellerUid が 0', 'PriceOfBread.SimCore/Instruments.cs',
  'I(st.Uid), I(S(sellerW).Uid)));', 'I(st.Uid), I(0)));', 'FullyQualifiedName~接触の内訳'),
 ('M19 disccontact_s の sellerUid が買い手の個体', 'PriceOfBread.SimCore/Instruments.cs',
  'I(st.Uid), I(S(sellerW).Uid)));', 'I(st.Uid), I(st.Uid)));', 'FullyQualifiedName~接触の内訳'),
 ('M17 検証が入力のハッシュを見ない', 'PriceOfBread.SimCore/OpeningBake.cs',
  'if (!root.TryGetProperty("header", out var hdr) || !hdr.TryGetProperty("inputHash", out var ih) || ih.GetString() != inputNow)', 'if (false)', 'OpeningBakeTests'),

 ('M14 検証が世界の国の欠けを見ない', 'PriceOfBread.SimCore/OpeningBake.cs',
  'foreach (var x in worldN.Except(tableN, StringComparer.Ordinal)) errs.Add($"世界の国 {x} が表に無い");', '', 'OpeningBakeTests'),
 ('M15 検証が表の国の余りを見ない', 'PriceOfBread.SimCore/OpeningBake.cs',
  'foreach (var x in tableN.Except(worldN, StringComparer.Ordinal)) errs.Add($"表の国 {x} が世界に無い");', '', 'OpeningBakeTests'),
 ('M16 検証が表の州の余りを見ない', 'PriceOfBread.SimCore/OpeningBake.cs',
  'foreach (var x in tableR.Except(worldR, StringComparer.Ordinal)) errs.Add($"{nk} の表の州 {x} が世界に無い");', '', 'OpeningBakeTests'),
]

only = sys.argv[1:]
out = open(W + 'mut_out.txt', 'a', encoding='utf-8')
def log(s):
    print(s); out.write(s + '\n'); out.flush()
for name, fn, old, new, flt in M:
    if only and not any(name.startswith(o + ' ') for o in only): continue
    p = R + fn
    orig = open(p, 'rb').read()
    s = orig.decode('utf-8')
    if s.count(old) != 1:
        log(f'{name}: SKIP（旧の文字列が {s.count(old)} 件）'); continue
    bak = BAK + os.path.basename(fn) + '.orig'
    open(bak, 'wb').write(orig)
    try:
        open(p, 'wb').write(s.replace(old, new).encode('utf-8'))
        b = subprocess.run(['dotnet', 'build', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', ART, '-v', 'q'], cwd=R, capture_output=True, text=True, encoding='utf-8', errors='replace')
        if ' error ' in b.stdout or b.returncode != 0:
            log(f'{name}: BUILD-FAIL {b.stdout[-300:]}'); continue
        fl = flt if flt.startswith('FullyQualifiedName') else 'FullyQualifiedName~' + flt
        t = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', ART, '--no-build', '--filter', fl], cwd=R, capture_output=True, text=True, encoding='utf-8', errors='replace')
        tail = [l for l in t.stdout.split('\n') if '失敗!' in l or '成功!' in l]
        verdict = 'RED(OK)' if any('失敗!' in l for l in tail) else 'GREEN(見逃し)'
        log(f'{name}: {verdict} {tail[-1].strip() if tail else t.stdout[-200:]}')
    finally:
        open(p, 'wb').write(orig)
log('done')
