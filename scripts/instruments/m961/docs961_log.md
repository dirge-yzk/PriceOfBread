## 2026-11-08 (955) 恒等の線 c85sl SecessionLaborLawInstrumentTests.cs:87 の対照を開幕の支配度に依らない盤面へ（波85・試験だけ・ident なし・完了〔★(961) で main へ・c85kp 経由〕）
- 事前登録 `measurements/m955_20261108_prereg_sl.txt`（d21a859d）。直し 88f1501a＝`Run(36)`→`Run(60)`・最後の四半期の指定 35→59 の 2 行だけ（★(743) の区分 (p)＝盤面〔期間〕の取り直し・(q) は採らない）。Assert の文・閾値 20・Near の式・クランプ除外は変えていない。
- プローブ（種 1789・植民地格の checkedPairs）：旗 0＝36/48/60 四半期で 1123/1334/1442・旗 1（c84kp の点火）＝7/19/31。本国格は両方の旗で 0。突然変異 M1（speed×1.01）・M2（クランプ除外を外す）・M3（Run(36) に戻す）は全て赤。
- 反証役（中・記録）：旗 1 の 31 組は四半期 29〜59 に 1 件ずつ＝1 州（推測：英領オーストラリア）の 1 系列だけ＝〈開幕の支配度に依らない〉形ではなく〈両方の旗でかろうじて通る〉形。**この指摘は (961) の合わせた木（kp＋lt）で実現した＝組 13 で赤**。突然変異の台本とログはコミットされていない（D:/PoB_work/c85sl_work）。

## 2026-11-08 (956) 恒等の線 c85hs H0＝HeldSinceFromData・CoreHeldYears と heldSince のデータの削除（波85・ident 1 本・完了〔★(961) で main へ〕）
- 事前登録 `measurements/m956_20261108_prereg_hs.txt`（db979486）・本体 82ff1811（Coefficients.cs の 2 係数・ProvinceSpec の位置引数 HeldSince・world.json の heldSince 29 行・Simulation.cs の BuildProvince を `Dominance = 1.0`・`Tier = RegionTier.Core` の直書き・注 3 か所・DormantIdentityTests の行を RetiredFields へ・新しい試験 OpeningProvinceHeldTests.cs）。
- ident c85hs〈判定: 恒等〉（測った種で一致）：最初は cpop_s:outOtherTop1Route が経路の行番号 2416→2413 だけ違って〈不一致〉＝宣言して check をやり直した。NG 93 新しい 0 外れた 0・推定 210.2 秒・テスト 2426。
- **予言の外れ**：world.json のバイトが変わると opening_bake.json の頭書き inputHash が合わない＝フルスイートで OpeningBakeTests 2 本が赤（ident は旗 OpeningBakeEnabled 0 で表を読まないので拾えない）→ 0d1009fa で inputHash を 1 行直した（持ち主の外）。
- 反証役（低・記録）：〈行番号だけ〉の正規化の台本が残っていない・台帳 S26-H005 の錨（CoreHeldYears）が漏れていた（(961) が写し直した）・NationConfig.cs:174 の注に HeldSince が残る（次の小物の束へ）・番人の走りは器のコミットの前の作業木。

## 2026-11-08 (957) 挙動の線 c85kp K＝開幕の一括昇格の植民地だけ支配度 1.0 から（波85・旗 OpeningColonyDominanceFull 既定 1・1 回目・**採った〔★(961)〕**）
- 枝 origin/c84kp（器 4ea56d76・点火 70caed7c）に main を merge・事前登録 fdb52653・番人 T3′（接収の AcquireColony は旗 1 でも 0 から）26143cd8・c85sl の 6defceee を merge（da16a760）。
- 腕 A（旗 0・使い捨ての木 46303e1c）〈判定: 恒等〉（素の 5 種）・突然変異 M1〜M6 は予言どおり（M6〈接収の後に 1.0〉は T3′ だけが赤）・旗 1 のフルスイート 2430＋3 失敗 0・savetest・leakhunt 合格。
- 腕 B：G1 1.0000（下限 0.9・旗 0 は 0.0595）・G2 0（上限 17・旗 0 は 45）・G3 0.0257（上限 0.05・旗 0 は 0.0273）・止める線 0・推定 211.8 秒。S11：1.0000・0・0.0253・止める線 0・213.6 秒。記録：exact 48・near 84・英の国債の不履行 4/10（S11 5/10）・英の後の分離 3→9。
- 1.0 という値は AI の推奨・確証なし（根拠の作者の言葉は「領土の所有権と実効支配を別々に扱うだけでなく、獲得後の統治能力が時間をかけて変化する仕組みが欲しいところです」だけ）〈作者「全て推奨で」で推奨を採った〉。
- 反証役：hs とは Coefficients.cs の隣り合う行で文字の上の衝突（合流で両方の行を残した）・M6 の最初の走りは CRLF で rc=9（台本を直して M6 だけ回し直した＝エンジンの変更ではない）・注の行番号は合わせた木でずれる・番人はコミットの 5 秒前の作業木で回った。

## 2026-11-08 (958) 挙動の線 c85lt A-2〈定額の土地税〉の腕つきの 2 回目（波85・旗 LandTaxFixedBasis 既定 1・**合流で外した〔★(961)〕**）
- 枝 origin/c84lb の 039541a0 から・fix942 の逆適用・main の merge・旗 1 の cherry-pick。ident c85ltz（旗 0）〈判定: 恒等〉（宣言 landfix_s）。
- **事前登録 [5] の字義から外れた**：旗 1 のフルスイートで GoodIdGuardTests が予言の外の赤（LandfixInstrumentTests.cs:127・:135 の `foreach (var q in new[] { 1, 2 })`＝四半期の添字を財の番号と誤検出・旗に依らない）→ 13af9704 で `for (int q = 1; q <= 2; q++)` に変えて腕 B へ進んだ（★(743) の区分の外と名乗った）。
- 腕 B：G7 20・G4 0・G8a 0・G8b 0・清の G8c 0.5097（窓 0.2314〜1.2594）・止める線 0・209.7 秒。S11：清の G8c 0.5616・止める線の〈それ以外〉1 件は pair で記録・212.4 秒。
- (961) の合わせた木（kp と重ねる）でも M・S11 の門は全て通った（清の G8c 0.4368／0.5301）が、フルスイートで SecessionLaborLawInstrumentTests.cs:87 が赤（組 13）＝lt を抜くと緑＝〈後から入れた側を外す〉で外した。**★(912) の〈腕の前の赤〉ではない**（線の門と止める線は全て通った）。

## 2026-11-08 (959) 挙動の線 c85mw MilitaryWarMult を接触の割合から導く（案 A・2 回目・**止めた**・3 回の規則に数えない〔★(912)〕）
- 器 757daf81（旗 MilitaryWarMultFromContact 既定 0・κ WarArmsDemandPerContact 42・Diplomacy.Engagement・Simulation.cs:2069 の 1 行・cwmult_s の末尾 5 列）・ident c85mwz〈判定: 恒等〉（cpop_s の経路の行番号 +2 を宣言）・突然変異 M0〜M6 は全て赤。
- 旗 1 のフルスイート 2431 で失敗 10＝予言の内 7（CivilWarArmsDemandTests の 6・MilitaryTests.cs:2030＝旧の規則を字で固定した番人・持ち主の外）＋**予言の外 3**（WriterRatchetTests の 2 行・TransportInstrumentTests.cs:91＝〈組が少ない〉型の 4 本目）→ 腕 B の前で止めた。
- 反証役（低）：Engagement の doc が FrontPush の doc を外した・T6 が字の 42 を固定・cpop の集計の台本が残っていない。
- 作者に聞く物（担当の推奨）：⑰（CivilWarArmsDemandUnified の旧の枝の焼き込み）と同じ線で旧の規則の番人を消し、双子の試験は κ を替える形へ。

## 2026-11-08 (960) 設計の材料の線 c85tx 土地税の A-2b′〈施行は Move の経路・代価の物差し〉（盤面 m947・腕 0 本・コミット 0〔★(961) が写した〕）
- 写した先：`docs/202611/20261108/20261108_土地税の算定の基準A1の段A-2b′_施行の経路と代価_盤面m947_下書き.md`・`…_材料.md`（題に〈設計の材料と下書き・AI の案・作者の確認待ち〉・本文は書き換えず反証役の訂正を下書きの末尾に足した）・`scripts/instruments/r960/r960read.py`・`measurements/m960_20261108/r960read_m947.txt`。
- 推奨 P1（Move を見積もりと払いに分ける・目標 T だけを状態に）・規則 C（財政の危機の四半期だけ目標を動かす）。反証役（高 2）：P1 には RGO ごとの地代の状態か凍結の予約が要る／規則 C は今の生で数えられ、日本は 10 種とも危機 0 件＝目標が 1 度も動かない。

## 2026-11-08 (961) 合流（波85・挙動の線 c85kp〔(957)〕を採った新しい基線 m961・恒等の線 c85hs〔(956)〕・試験だけの c85sl〔(955)〕を入れた・c85lt〔(958)〕は合わせた木のフルスイートの赤で外した・c85mw〔(959)〕は記録だけ・設計の材料 c85tx〔(960)〕を写した）
- 作ったフォルダ：D:/PoB_work/m961_work・m961_work2・wt_m961・wt_m961z・wt_m961k（帰属の確かめ）・ident_m961z（2 回）・keep_961・keep_961_rej。予言 `measurements/m961_20261108_prereg_merge.txt`（最初のコミット 5c59ab2d・追記 b3f0dc02 は〈結果の後の変更〉と名乗った）。
- **1 回目の合わせた木（hs＋kp＋sl＋lt・測った木 e2dea72d）**：衝突は Coefficients.cs（hs の注と kp の旗の行が隣り合う＝両方を残した）と opening_bake.json の頭書き（合わせた木で焼き直した＝本文は前の表とバイト一致）。予言は当たった＝A-lt0（素の 10 種）は c85kp の腕 B と、A-kp0 は c85lt の腕 B と名乗り行を除いて全行一致・腕 Z〈判定: 恒等〉（m947 と全行一致・宣言 landfix_s 10 本と cpop_s:outOtherTop1Route・テスト 2450）・M（exact 45・near 85・211.3 秒）と S11（45・86・211.6 秒）で kp の門（G1 1.0000・G2 0・G3 0.0250／S11 0.0253）と lt の門（清の G8c 0.4368／0.5301・G7 20・G4・G8a・G8b 0）は全て通過・止める線 0（〈100 倍〉の〈それ以外〉0・空洞化 1/10 未満 0・名目 GDP 0 の続き 0）・WriterRatchetTests の 3 行（予言の内）を取り直した。
- **予言の外れ（記録の先頭）**：そのフルスイート（2450）で SecessionLaborLawInstrumentTests.cs:87 が赤〈組が少ない 13（対照）〉。lt を抜いた木 55b910e1 では 5/5 緑＝組み合わせで落ちる＝(955) の反証役の指摘が実現した。合流は試験を直さない（★(940)・RULE47）＝指示の規則〈後から入れた側を外す〉で lt を外し、枝 m961 を 55b910e1 へ戻した（push の前）。代案〈脆さの根は kp の側＝kp を外す〉は規則の外の読み替え＝採らなかった。1 回目の記録は `measurements/m961_20261108/combo_kp_lt/`、腕の生は keep_961_rej（門と止める線の表へ縮めた物）。
- **2 回目の合わせた木（hs＋kp＋sl・測った木 {H}・SOURCE-HASH {SH}）**：{RESULT2}
- テスト：{SUITE}
- 書き換えたテスト：合流は 0 本（1 回目の木の WriterRatchetTests の 3 行は lt と一緒に外した）。線の書き換え＝c85sl の SecessionLaborLawInstrumentTests.cs:52・:89（★(743) (p)）・c85hs の DormantIdentityTests の表の移し（★(743) ①④）。
- 減った行と増えた行（SimCore・Tests・Runner・3fbb5f8b からの差）：{LINES}
- D: の空き（片付けの後・shutil）：{FREE}GB。
