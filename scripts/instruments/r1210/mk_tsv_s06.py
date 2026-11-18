# 読むだけ (1210) c121rn：11-12 の S06 の 30 行を、今の木 326e284e の錨・写しと読み直しの別で 12 列の TSV に書く（(1203) mk_tsv_s21.py の形を写した）。
#  入力：q3/ledger_S06_2026-11-12.tsv（木 7bb40c77）・q3/s06_cmp.tsv（cmp_rows.py）・q3/s06_anchor_hunks.tsv（anchor_hunks.py）・q3/s06_readers.tsv（dl_readers.py）。
#  出力：q3/ledger_S06_2026-11-18.tsv（12 列）・q3/side_S06_2026-11-18.tsv（写した判定と読み直した判定の列を分ける）・q3/dl_table_S06_2026-11-18.tsv（消す候補）。
#  12 列目の〈作者「全て推奨で」で決定〉は〈消すと決まった〉と読めるので〈AI の推奨（作者「全て推奨で」の下の候補）〉へ直す（(1196)・(1203) と同じ）。
#  使い方: python -X utf8 -I mk_tsv_s06.py <作業フォルダ>
import sys, os, csv
W = sys.argv[1]
NEW = '326e284e'
RAW = '（生 keep_1198/base_ins 種 1〜10・台本 r1210 の s06_recount.py・出力 q3/s06_recount_m1198.tsv）'
def rd(p): return list(csv.reader(open(os.path.join(W, p), encoding='utf-8'), delimiter='\t'))
cmp = {r[0]: r for r in rd('q3/s06_cmp.tsv')[1:]}
near = {}
for r in rd('q3/s06_anchor_hunks.tsv')[2:]:
    if len(r) > 3 and r[3] not in ('0', '—'): near.setdefault(r[0], []).append(f'{r[2]}（{r[3]} 塊：{r[4][:60]}）')
# 錨の近く（±12 行）の差がある行の読み：どれも行の機構の式には触れない（削除の線の別の行）
NEAR_READ = {
 'S06-08': 'MarketFlow.StepDay の TargetStock の行の削除（市場の在庫の目標・物価水準の式ではない）',
 'S06-17': 'EconomyStep.Run の鉱物の自己費用の行の直し（内戦の麻痺の 2 行は字が同じ）',
 'S06-19': 'MonthSettle の記念碑の積み MonumentBuildQueue の削除（scrapKeep の式は字が同じ）',
 'S06-22': 'MonthSettle の記念碑の積みの削除（WarmupFrozen の門は字が同じ）',
 'S06-25': 'MonthSettle の戦争の恐れの張力 WarScareTension の写しの削除（社会保障・収穫・教育の写しは字が同じ）',
}
# 錨の取り直し（cmp が取り直せなかった物・注が履歴へ移った物）
ANCH = {
 'S06-04': 'EconomyStep.cs:2493:p.Satisfaction = p.NeedTotal;Coefficients.p02_borrow_polity_epidemic.cs:18:CrimeSatPenalty',
 'S06-27': 'Coefficients.p08_audit_soldiers.cs.md:536:HoardDishoardRate',   # docs/history の墓標（文書の見出しは ゲーム概要と実装機能.md:1175＝台帳の門は日本語の名のファイルを引けない）
 'S06-28': 'Coefficients.p08_audit_soldiers.cs.md:153:HoardCrystallizeRate',   # 見出しは :1180
 'S06-29': 'Coefficients.p08_audit_soldiers.cs.md:354:CurrentInputFillGates',   # 見出しは :1190
}
# 読み直した行：(判定の上書き or None, 判定の型の上書き or None, 根拠, 12 列目の上書き or None)
RE = {
 'S06-01': (None, '頻度', '〈読み直し〉labor_s の farmUnemploymentShare（農業の労働の売れ残り）の種ごとの平均 0.00000〜0.00023（最大 0.006〜0.225）・plainUnemploymentShare（高度でない労働）の平均 0.089〜0.097（最大 0.80〜1.00）＝11-12 の〈農業の労働の失業は平均 0.0000〜0.0003〉と同じ。高度の労働は計器が書かない＝数えていない' + RAW, None),
 'S06-05': ('働く（国の中の恒等）', None, '〈読み直し・ソース〉基準の取り直し RebaselineInvariants の呼び手は MultiNationSim.cs:440（開幕）・:554（助走の後）・OpeningBake.cs:188（焼き込み）と SpawnNation（MultiNationSim.cs:373 の new Simulation＝途中で生まれた国の構築）＝根拠の文〈基準の取り直しは開始の前だけ〉は〈開始の前と、途中で生まれた国の構築の時〉が正しい（反証 §2-1 の直し・S22-43 へ写す時も）。(1205) の leakhunt は緑', None),
 'S06-09': (None, '頻度', '〈読み直し〉物価の床 0.4 が効いた四半期（pclamp_s の kind=floor・403 行全てで priceLevelAfterFiat＜0.4 かつ after＝0.4＝World.cs:1341 の Math.Max と同じ不等式）は 403／409,752 国×四半期（0.098%・種ごと 0〜101）＝11-12 の〈0〜0.07%〉より僅かに多い・同じ桁。非有限と 0 以下の守りは〈: before〉の枝（World.cs:1341）' + RAW, None),
 'S06-19': (None, '頻度', '〈読み直し〉後退度（World.UpdateGdpCycle の式を nation_s の realGdpPerYear で組み直した・平滑 0.18・ピークの忘却 0.02）が DepressionScrapThreshold 0.20 を超えた国×四半期＝47,660／409,752（11.6%・種ごと 7.8〜14.6%）・開いた中で 1 人あたりの後退度≦0.2＝22.6%（11-12 の種 1〜3 は 13.1%）＝門は小国・縮んだ国で開く型のまま（門の多い国＝サルデーニャ＝ピエモンテ・ソコト・ライン諸邦・マラータ同盟ほか）。削る 3 本のうち OldVintageCapital・StrandedVintage は常に 0・工業化度の在庫は IndustrializationOff=1 で能力に掛からない（ソース・11-12 と同じ）。限界：計器の realGdpPerYear とエンジンの GdpPerMonth の時点の差は確かめていない＝近似' + RAW, None),
 'S06-21': ('働かない（休眠・旗 IndustrializationOff=1 の門の外）', None, '〈読み直し・ソース〉IndustrializationOff＝1（Coefficients.p06_rank_discovery.cs:119）・枝 DevelopmentMemory > 0 && !indOff（EconomyStep.cs:866）は基線で走らない＝11-12（反証 §0-1）と同じ', None),
}
def fixdec(s): return s.replace('〈作者「全て推奨で」で決定（2026-11-12）〉', '〈AI の推奨（作者「全て推奨で」の下の候補）〉').replace('作者「全て推奨で」で決定', 'AI の推奨（作者「全て推奨で」の下の候補）')
RDR = {r[0]: r for r in rd('q3/s06_readers.tsv')[1:]}
def rdn(k):
    r = RDR[k]; return f'読み手 {sum(int(x) for x in r[2:12])}＝エンジン {r[2]}・計器 {r[3]}・係数 {r[5]}・試験 {r[6]}・台本 {r[10]}・データ {r[11]}'
KMAP = {'S06-11': 'K83', 'S06-17': 'K84', 'S06-18': 'K85', 'S06-19': 'K86', 'S06-20': 'K86', 'S06-21': 'K87', 'S06-26': 'K89', 'S06-27': 'K89', 'S06-28': 'K89', 'S06-29': 'K89'}
# 行ごとの処置と理由（1 行）
ACT = {
 'S06-01': ('残す', '失業の数え方は S04-25（労働 3 財）の読む線が持つ＝二度数えない'),
 'S06-02': ('残す', '働く・焼き込みは S12-16 の線'),
 'S06-03': ('残す', '数える計器が無い＝S12-15 を参照'),
 'S06-04': ('直す', '文書だけ：犯罪の減算の例外の文を消す（コードは (384) で削除済み）'),
 'S06-05': ('直す', '根拠の文だけ（基準の取り直しは開始の前と途中の建国）・機構は働く'),
 'S06-06': ('残す', '働く'),
 'S06-07': ('残す', '働く'),
 'S06-08': ('残す', '働く'),
 'S06-09': ('残す', '床は 0.098% の国×四半期だけ・非有限の守りは別の枝・取りまとめ 問い2 の B を変えない（記録の候補 K88）'),
 'S06-10': ('残す', '貿易の置き換え Q1 の決定まで'),
 'S06-11': ('消す', '既にある線 M6・植民地の格の州の多くが床＝錠'),
 'S06-12': ('直す', '文書だけ'),
 'S06-13': ('残す', '働く'),
 'S06-14': ('直す', '支配の力 段 F の設計で書き直す（拿捕の 1 本は名目へ）'),
 'S06-15': ('残す', '働く'),
 'S06-16': ('直す', '計器の線：平滑とピークの出どころを World の後退度へ（記録だけ）'),
 'S06-17': ('消す', '既にある線 M9・思想⑨（戦場の被害と重なる）'),
 'S06-18': ('消す', '既にある線 M9・闘争度の閾値の錠'),
 'S06-19': ('消す', '既にある線 M9・削る相手の 2 本は常に 0・残る在庫は能力に掛からない'),
 'S06-20': ('消す', 'S06-19 と同じ門 scrapKeep の塊'),
 'S06-21': ('消す', '恒等の線・基線で走る読み手 0（旗 IndustrializationOff を焼く線と同じ回）'),
 'S06-22': ('残す', '働く'),
 'S06-23': ('直す', '文書だけ'),
 'S06-24': ('直す', '思想⑩：欄ごとの表を作ってから恒等で写せる欄だけ World を読む形へ'),
 'S06-25': ('直す', 'S06-24 と同じ読む線→恒等の線'),
 'S06-26': ('消す', '文書の見出しを畳む・コードの読み手 0'),
 'S06-27': ('消す', '文書の見出しを畳む・コードの読み手 0（11-12 の錨＝係数の注は (1114) で履歴へ移った）'),
 'S06-28': ('消す', '同上'),
 'S06-29': ('消す', '同上'),
 'S06-30': ('直す', '文書と注だけ（恒等）・関税の選好の読み手は貿易の置き換え T3'),
}
rows = rd('q3/ledger_S06_2026-11-12.tsv')
hdr, body = rows[0], rows[1:]
out, side = [hdr], [['行', '錨の関数の差（cmp_rows.py・anchor_hunks.py）', '写した判定（11-12・木 7bb40c77）', '読み直した判定（326e284e・keep_1198）', '確証', '処置', '理由（1 行）']]
for r in body:
    rid = r[0]; c = cmp.get(rid); old_judge = r[8]
    anchors = ANCH.get(rid) or (c[2] if c and c[2] and '?' not in c[2] else r[4])
    if c and c[1] == '変わった':
        if rid in near: delta = '錨の近くの差＝' + NEAR_READ.get(rid, '未読') + '（行の式に触れない）'
        elif rid in ('S06-27', 'S06-28', 'S06-29'): delta = '係数の注が履歴へ移った（(1114) ほか）＝錨を docs/history の墓標へ取り直した（文書の見出しは ゲーム概要と実装機能.md:1175・:1180・:1190 に残る＝畳む物）'
        elif rid == 'S06-04': delta = '錨 Crime.cs:68 の注が履歴へ移った（(1113)）＝錨を係数の部品の注へ取り直した・満足度の行は字が同じ'
        else: delta = '囲む関数は字が違うが錨の前後 12 行に差なし（大きな関数の別の直し）'
    else:
        delta = '錨の関数の字が同じ'
    j, typ, basis, fix = RE.get(rid, (None, None, None, None))
    new_judge = j or old_judge
    if basis is None: basis = f'〈写し〉11-12 の判定と根拠（木 7bb40c77）を写す。{delta}。11-12 の根拠：' + r[9]
    else: basis = basis + f'。{delta}'
    r2 = r[:]; r2[4] = anchors; r2[7] = NEW; r2[8] = new_judge; r2[9] = basis
    if typ: r2[6] = typ
    act, why = ACT[rid]
    t12 = fixdec(fix if fix else r[11])
    if t12 in ('—', '') or t12.startswith('—'): t12 = f'{act}：{why}' + ('' if t12 in ('—', '') else '（' + t12[1:].strip('（）') + '）')
    if rid in KMAP and t12.startswith('消す'):
        t12 = '消す〔' + KMAP[rid] + '・' + rdn(KMAP[rid]) + '・326e284e の dl_readers.py〕' + t12[2:]
    r2[11] = t12
    out.append(r2)
    reread = rid in RE
    conf = '中（生で数えた・種 1〜10）' if rid in ('S06-01', 'S06-09', 'S06-19') else ('高（ソースの grep）' if reread else '写し（11-12 の生で数えた物・今の生では数えていない）')
    side.append([rid, delta, old_judge, new_judge if reread else '—', conf, act, why])
with open(os.path.join(W, 'q3', 'ledger_S06_2026-11-18.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    for r in out: f.write('\t'.join(x.replace('\t', ' ').replace('\n', ' ').replace('|', '／') for x in r) + '\n')
with open(os.path.join(W, 'q3', 'side_S06_2026-11-18.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    for r in side: f.write('\t'.join(x.replace('\t', ' ').replace('\n', ' ').replace('|', '／') for x in r) + '\n')
# 消す候補の表（dl_table の形の列を写す：id・名・読み手・(b) 重なり・(c) 閾値と錠・消し方・史実で重要か・失う物・大きな機構から導けるか・前提の線）
DL = [
 ('K83', 0, 1, '挙動', '否', '州の 1 人あたり貯蓄の差で州の物価を動かす乗数（床 0.7）', '導ける＝貿易の置き換えの自己費用の段（州の値段は市場から）', 'M6・貿易の置き換え T0 の盤面より前'),
 ('K84', 1, 0, '挙動', '否', '内戦中の州の実効生産性 ×0.8', '一部＝内戦が War として戦われる時は戦場の強度 LastWarIntensity が被害を出す・War にならない内戦は未確かめ', 'M9'),
 ('K85', 1, 1, '挙動', '否', '組合容認・闘争度の閾値を超えた州の産出 −25%', '一部＝罷業の強度（dcont の strikeIntensity）と賃金の交渉が既にある・AI の近代化の信号 ReturnShare も動く', 'M9'),
 ('K86', 1, 1, '挙動', '否（今のコードでは〈縮んだ小国の在庫が削られる〉だけ）', '後退度 0.2 を超えた国の工業化度・旧世代資本・座礁資本の月 6%×超過・休止中の設備の解体', '導ける＝倒産・休止・損耗の実の道（工業化度の在庫は能力に掛からない）', 'M9（K4）'),
 ('K87', 0, 0, '恒等', '否', '到達した最大の開発度の記憶（旗 IndustrializationOff=1 の門の外）', '導く必要なし（基線で走らない）', 'IndustrializationOff の焼き込み（M15）か ind4'),
 ('K88', 0, 1, '挙動', '否', '物価水準の床 0.4（国×四半期の 0.098%）', '記録だけ＝取りまとめ 問い2 の B（残す）を変えない', '—'),
 ('K89', 0, 0, '文書だけ', '否', '文書の見出し 4 本（コードの読み手 0）', '—', '文書の線'),
]
with open(os.path.join(W, 'q3', 'dl_table_S06_2026-11-18.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('id\t名\t読み手（326e284e）\t(b) 他の量と重なる\t(c) 閾値・錠・床\t消し方\t史実で重要か\t失う物\t大きな機構から導けるか\t前提の線\n')
    for k, b, cc, how, hist, lose, derive, pre in DL:
        f.write(f'{k}\t{RDR[k][1]}\t{rdn(k)}\t{b}\t{cc}\t{how}\t{hist}\t{lose}\t{derive}\t{pre}\n')
print('ok', len(out) - 1, 'rows')
