R='C:/Users/dirge/Documents/PriceOfBread/measurements/'
def ap(p,s):
    b=open(R+p,'rb').read()
    if not b.endswith(b'\n'): b+=b'\n'
    open(R+p,'wb').write(b+('\n'+s.strip()+'\n').encode())
ap('m1058_20261112/q5_questions.txt','''【合流 (1060) の訂正】この表は out_q23.txt（大きさの集計）より前に書かれた。Q-pq2・Q-pq3 の推奨は担当の報告（report.txt）で逆になった＝最終の推奨は次のとおり。
- Q-pq2：独立の線（案 A の 2 回目・案 B と並べてよい）。調達は U1b の分子の 0.05% 以下（近似の上界 0.24%）で U1 の採否に効かない。
- Q-pq3：U1b の分子から除かずに〈U1b は国家の調達を含む〉と名乗るだけ（除いても新÷旧は 4 桁目までしか動かない）。''')
ap('m1058_20261112/q1_readers.txt','''【合流 (1060) が足した（反証役）】
- Runner の LastExport の読み手（診断の出力・シミュの挙動には触れない）：Program.cs:1243〜1246（輸出特化の指標 LastExport÷LastProduced）・:2566〜2567・:5848〜5849・:5909・PlayHarness.cs:374。形 Y にすると首都州の調達がこれらの〈輸出〉から抜ける＝形 Y の事前登録に 1 行書く。
- 表の行〈EconomyStep.cs:963 sellR 旗 1：LastDemand＋PrevExport−PrevImport〉の和の式は main ではなく origin/c99ov EconomyStep.cs:967。
- 〈上界 0.24%〉は近似の上界：procF は flow_s の giveIncome（POP の階級の行）だけで、国営の持分が国庫へ行く分（stateCut）は入らない。首都＝sellr の最初の州の置き方も種により外れる。形 Y の計器 procx_s は pay の全額を書く。''')
ap('m1057_20261112/prereg_next_draft.txt','''【合流 (1060) が足した（反証役）】
- W1 は根（uMod から techGap が消えたこと）の迂回で、根の直しではない。
- 全ての AI の国に及ぶ副作用を記録に置く：GateLaw（SelfPlay.cs:319〜:331）は支持が足りないと目標を次の 1 段のまま残す＝WomenRights の戦線が開いたままになり CountOpenFronts（Government.cs:513〜:529）の N が増え、政治押し予算 (1/N)^0.5 が他の連続の軸を薄める。CrownParliament.cs:573 の Enum2(w.WomenRights, p.WomenRights) は王権と議会の距離に入る。→ 同じ盤面の旧と新で数える記録：開いた戦線の数 N と PoliticalPushBudget・王権と議会の距離。6 国以外の段 1 以上の件数と最初の年を上の門の候補に置く。
- lawgate2_s の WomenRights 行は 0 行（2505 行中 2504 行が SocialSecurity）＝計器が書かない（計器の限界）＝計器 C が要る理由。''')
print('ok')
