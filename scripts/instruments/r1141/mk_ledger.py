# (1141) S25 外交の台帳の行の書き直しの案（12 列）を TSV に書く（読むだけ）。
# 数は out_freq_{M,N1,N2,S11}.txt・out_submits_*.txt・out_v02_*.txt・out_vassal_*.txt・out_grudge_mask.txt の写し。
# 錨：元の行の錨を out_anchors.txt の宣言の行へ付け替え、行ごとに今の木（03d192514）の行を足す。倍率の一掃の印は out_multclass.txt の写し。
import os, re
W = 'D:/PoB_work/r1141_work'
C = '03d192514'
H = ['行', '系統', '機能', '期待する動き（出典）', '錨', '持ち主のファイル', '判定の型', '最後に確かめた', '判定', '根拠', '残りタスク', '直す／消す／残す']
old = {}
for l in open(os.path.join(W, 'ledger_rows.txt'), encoding='utf-8'):
    c = [x.strip() for x in l.strip().strip('|').split('|')]
    old[c[0]] = c
anc = {}
for l in open(os.path.join(W, 'out_anchors.txt'), encoding='utf-8'):
    if l.startswith('#'): continue
    rid, rest = l.rstrip('\n').split('\t')
    out = []
    for a in rest.split(' ; '):
        a = a.strip()
        if '=NG→' in a: out.append(a.split('=NG→')[1])
        elif a.endswith('=OK'): out.append(a[:-3])
    anc[rid] = [x for x in out if x and x != 'None']
mult = {}
for l in open(os.path.join(W, 'out_multclass.txt'), encoding='utf-8'):
    rid, rest = l.rstrip('\n').split('\t')
    if rest == '—': continue
    tags = []
    for x in rest.split(' ; '):
        n, w = x.split('=', 1)
        first = w.split(',')[0]
        m = re.match(r'(問い\d+|個別|1-2|2-2|中|小|大|増幅の輪)[^/]*/(.+)$', first)
        tags.append(f'{n}={m.group(1)}/{m.group(2)}' if m else f'{n}={first[:12]}')
    mult[rid] = '・'.join(tags)
EFG = '段 E・F・G の線が畳む'
READ = {  # 〈消す〉の行の読み手の数（git grep -w・03d192514・readers_S25.txt と readers_b.txt の写し・SimCore／試験）
 'S25-13': '読み手 コード 8（DefeatShockTensionRelief 3・DefeatShockProtectMin 2・DefeatedShock 3）・試験 0',
 'S25-14': '読み手 コード 5（FrozenTensionDecayRate 2・CannotDeclareWar 3）・試験 0',
 'S25-17': '読み手 コード 19（HegemonContainmentTrade 3・HegemonContainmentMax 3・_hegemonContainLost 4・AntiHegemon 3 本 6・HegemonThreatMin 3）・試験 0',
 'S25-40': '読み手 コード 5（VassalHoldScale 3・VassalDefectionMilitancy 2）・試験 0',
 'S25-42': '読み手 コード 5（MaxDependents）・App と Runner 2・試験 0',
 'S25-46': '読み手 コード 5（VassalizeWinScore 3・GovCostVassalThreshold 2）・試験 1',
 'S25-49': '読み手 コード 9（TreatyPortWinnerDistanceKm 5・TreatyPortReparationMult 4）・試験 6',
 'S25-50': '読み手 コード 7（TreatyPortLapseRatio 3・TreatyPortLapse 4）・試験 20',
 'S25-51': '読み手 コード 1 か所（Diplomacy.cs:2802〜2815 の引き継ぎの枝）・試験 0',
 'S25-56': '読み手 注 2 行（Diplomacy.cs:2039・:2157）',
 'S25-59': '読み手 コード 8（UnificationAiPursueChance・CooldownYears・WarGapYears・TryUnificationWars 各 2）・試験 0',
 'S25-69': '読み手 コード 5（ConcertReparationRestraint 3・ConcertRestraintMax 2）・試験 4',
 'S25-70': '読み手 コード 4（ImposeRegimeIdeologyGap 2・ImposedRegimeLegitimacy 2）・試験 0',
 'S25-73': '読み手 コード 9（AISubmitMilRatio＝屈服と共有・離反の枝 Diplomacy.cs:1277）・試験 0',
}
R = []
def r(rid, verdict, basis, task, act, typ=None, extra=None, expect=None):
    o = old[rid]
    if act.startswith('消す') and rid in READ: act = act + '・' + READ[rid]
    a = anc.get(rid, []) + (extra.split(';') if extra else [])
    a = ';'.join(dict.fromkeys(x for x in a if x)) or '—'
    lref = re.search(r'（L\d+）', o[3])
    ex = (expect + (lref.group(0) if lref else '')) if expect else o[3]
    t = task
    if rid in mult: t = (t + '〔倍率の一掃：' + mult[rid] + '〕') if t != '—' else '〔倍率の一掃：' + mult[rid] + '〕'
    R.append([rid, 'S25', o[2], ex, a, o[5], typ or o[6], C, verdict, basis, t, act])

A = 'M＝keep_1136/base_ins・N1／N2＝keep_1122/n1・n2・S11＝keep_1122/a11'
r('S25-01', '働く', 'warband start_main（時計の開戦）M 103〜190／種（N1 85〜177・N2 94〜167）。時計が満ちた所の反対の側＝AI 同士の屈服 M 780〜1,360／種（時計が満ちた回の 87.5%＝10 種の和）。緊張≧140 の月のうち門が閉じた月 M 61.7〜73.1%（N1 57.4〜71.8%・N2 53.7〜74.8%・交戦中の組を含む）', '—', '残す', typ='両側の門')
r('S25-02', '働く', 'ソース：_tension に書く所は AddTensionKeyed（実際に動いた量だけ内訳へ・Diplomacy.cs:557〜566）・CoolTensionTo／DriftTension の CoolWhy（比例で縮める・:570〜584・:3229）・ResetTensionTo（理由 1 本へ・:587）の 3 つだけ（git grep で直の書き込みは :573・:589 だけ）。番人は試験 8 か所が内訳を読む', '—', '残す', typ='不変条件')
r('S25-03', '未確かめ', '理由ごとの内訳の列が無い（warpair は≧100 の組だけ・理由なし）。ソース：Diplomacy.cs:968〜996（交戦中の組は除く・同盟は ThreatReach で 0・従属は ×(1−AllyAttentionDiscount)）', '計器 dipten_s（理由の内訳）', '残す')
r('S25-04', '一部だけ働く', 'ソース：熟練の倍率は脅威の超過の全体（GDP・植民地・軍の 3 項の和−1）に掛かる（Diplomacy.cs:977〜978）＝狙いの軍の項だけでない。熟練は陸の力 LandPower（:36＝Military×ArmySkillMul）に既に入るが、脅威の軍の項は Military（熟練なし・:1034〜1038）を読む。文書の〈陸海の高い方・最大 1.5 倍〉は古い（熟練は 1 本 World.cs:399〜400）', '—', '直す（軍の項を LandPower へ・SkillTensionWeight を消す＝挙動・小）', typ='効き')
r('S25-05', '未確かめ', '理由の内訳の列が無い。ソース：政体の座標の表（Diplomacy.cs:1052〜1059）で毎月 ±（:1062〜1075）', '計器 dipten_s', '残す（S×M の S の項・11-09）')
r('S25-06', '未確かめ', '理由の内訳の列が無い。ソース：文化の群の表（Diplomacy.cs:1094〜1106）と宗教の家族の座標（:1109〜1125）を毎月の組の走査で（:1128〜1196）。文化名をコードの表に書く形（作者の問いの材料）', '計器 dipten_s', '残す（S×M の S の項・11-09）')
r('S25-07', '未確かめ', '理由の内訳の列が無い。ソース：AffinityTensions の保護の枝（Diplomacy.cs:1189〜1194・ProtectorTensionPerMonth 0.02）と介入の動機の掛け算（:2151〜2152）', '計器 dipten_s', '残す')
r('S25-08', '未確かめ', '理由の内訳の列が無い。ソース：閉じた海の出口の持ち主へ毎月（Diplomacy.cs:943〜956）', '計器 dipten_s', '残す')
r('S25-09', '働く', 'opium_s：所持を禁じた国で緊張が積んだ国×四半期 M 1,408〜2,985／種（N1 1,309〜2,850・N2 788〜2,545）。輸出を禁じた国の行 M 11〜3,195／種', '00-H 阿片の作り直し', '残す')
r('S25-10', '働く', 'monai：OpiumExportBan を AI が動かした国 M 2〜47／種（N1 5〜36・N2 2〜60・S11 2〜55）・終局で禁止 0〜41', '—', '残す')
r('S25-11', '働く', 'warband：緊張≧100 の組×四半期 M 1.8〜2.1%（N1 1.6〜2.2%・N2 1.5〜2.4%）。高い帯の枝は全て出る：ハイエナ（衝撃≧0.3）が高い帯の行の 9.3〜10.3%（衝撃の読めた行）・遺恨＞0 が 4.5〜5.9%（M・N1・N2）。高い帯の残りは平時の 0.42 と同じ速さ（Diplomacy.cs:3252〜3256＝遺恨 0 なら低い帯と同じ）', 'S×M（11-09）で〈毎月の定量の冷め方〉を消す', '残す（S×M の線まで）', typ='効き')
r('S25-12', '一部だけ働く', '薄れ方は働く：同じ組の続く四半期の比の中央 0.9905〜0.9915＝半減期 18.2〜20.3 年（M・N1・N2 とも・世代の置き換わり Diplomacy.cs:3259〜3275）。ただし講和の直後（遺恨≧0.9）の高い帯の行の 63.8〜67.8% はハイエナの枝が先に取る（DriftTension の枝の順 :3246→:3249→:3252）＝〈冷めない〉狙いが講和の直後の約 3 分の 2 で消える（out_grudge_mask.txt）', 'S×M の M の m と二重にしない（思想⑨・作者 2026-11-10）', '直す（ハイエナの枝を消すと解ける＝S25-13）', typ='効き')
r('S25-13', '働く', '高い帯の行のうち衝撃≧0.3 の組 M 2,524〜5,422／種（衝撃の読めた行に対する割合 M 10.3%・N1 9.9%・N2 9.3%）。S25-12 と向きが逆で、枝の順で遺恨の側を打ち消す', '大きな機構（厭戦の門 WarWearyThreshold・敗者は開戦しない）から導ける', '消す（候補 D-b・挙動・小）', typ='効き')
r('S25-14', '一部だけ働く', 'ソース：凍結の組の冷め方 0.2／月（FrozenTensionDecayRate）は、(736) で高い帯の止めが消えた後の平時 0.42 より遅い＝狙い〈微小な減衰で解凍〉の向きが逆（従属の組を他より遅く冷ます）。従属の組は整列 −1.2／月（Diplomacy.cs:1254）も受ける', '—', '消す（候補 D-c・挙動・小）', typ='効き')
r('S25-15', '働く', '同じ枝（Diplomacy.cs:2439〜2445）が +70 と遺恨 1 を書く＝遺恨≧0.9 の高い帯の行 M 3,408・N1 2,394・N2 2,092（10 種の和）。+70 の量は列なし', 'S×M の M（出来事の記憶）へ（11-09）', '残す（S×M の線まで）')
r('S25-16', '働く', 'warentry rank：4 段が全ての四半期に定員どおり（Great 7×668・Secondary 5×668・Middle 7×668＝M・N1・N2 とも）', '—', '残す', typ='効き')
r('S25-17', '未確かめ', '覇権国・封じ込めの列が無い。ソース：覇権＝脅威の超過の最大（Diplomacy.cs:1042〜1049）・封じ込め＝交易アクセスを最大 0.55 失う（:695〜704・TradeAccess :679）。脅威の緊張（S25-03）が既に覇権国への緊張を積む＝合従の倍率 3 本と二重（倍率の一掃 問い4 理由4）', '大きな機構（脅威の緊張と同盟の規則）から導ける', '消す（候補 D-a・挙動）')
r('S25-18', '未確かめ', '同盟の締結・数・上限の列が無い。同盟の参戦の戦争 M 168〜255／種＝同盟は張られている。締結の確率 0.4 はコードの数（Diplomacy.cs:1757・係数表の外）', '計器 dipev_s（同盟の成立と破棄）', '残す', extra='Coefficients.p04_trade.cs:97:MaxAlliances;Diplomacy.cs:1756:MaxAlliances')
r('S25-19', '未確かめ', '同盟の列が無い。ソース：陸塊が違えば海盆の重なり＞0 を要る（Diplomacy.cs:1749〜1755）', '計器 dipev_s', '残す', extra='Diplomacy.cs:1750:Landmass.Same')
r('S25-20', '未確かめ', '同盟の破棄の列が無い（Log だけ・Diplomacy.cs:1291〜1303）', '計器 dipev_s', '残す')
r('S25-21', '働く', 'wars entryCode ally M 168〜255／種（N1 147〜265・N2 122〜215・S11 97〜245）＝戦争の約 3 分の 1。見送り（国難・勝ち目なし・緊張が低い）の側は列なし（Log だけ）＝S24-H006 と同じ', '—', '残す')
r('S25-22', '未確かめ', '参戦の段（depth）の列が無い（ally の札は段を分けない）', '計器 wars の warDepth', '残す')
r('S25-23', '未確かめ', '断った回と +60 の列が無い（理由 AllyRefusal の内訳）', '計器 dipten_s', '残す')
r('S25-24', '一部だけ働く', 'ソース：開戦の禁輸は戦争ごとに入る（AddBelligerent Diplomacy.cs:1527）・終戦で外す（:2978）。ただし禁輸の効きは組ごとでなく、その国の売りの全体に掛かる 1 本の倍率（TradeAccess＝失った交易比重の和 :666〜681→World.ExternalTradeAccess→BilateralMarket.cs:298・:750）＝禁輸の相手以外への売りも同じ割合で減る', '組ごとの門へ（11-11 問い4 の軍需の輸出と同じ形）', '直す（候補 L3）', typ='効き')
r('S25-25', '一部だけ働く', 'S24-H002 と同じ門。ソース：同じ陸塊の組は Projection＝1/(1+km/10,000)（Diplomacy.cs:3215）で、海路が引けないときの km は大圏×2（SeaGraph.cs:348）＝0.2 を割るには 4 万 km が要る＝陸続きでは門が閉じない。閉じるのは海越えで制海権が小さいときだけ（:3216〜3217）', 'S24-V02・段 F（投射を州ごとの届きへ）', '直す（' + EFG + '）', typ='片側だけの門')
r('S25-26', '一部だけ働く', 'warpair submits M 780〜1,360／種（N1 804〜1,521・N2 529〜1,826）＝時計が満ちた回のうち屈服 M 87.5%（N1 88.6%・N2 89.6%・10 種の和）。物差し SideMilitary は投射を見ない（Diplomacy.cs:1420〜1432）＝屈服の 54.3〜56.3% が首都の大圏 4,000km 超・19.6〜21.1% が 8,000km 超（観戦 S4・Q12・X9）。同じ組の繰り返し：朝鮮×清が 8/10 種で 52〜64 回（屈服は緊張を 110 へ戻すだけ・正統性 −0.05 が毎回）', '屈服の 1 本化（支配の力 §4-3）', '直す（' + EFG + '）', typ='頻度')
r('S25-27', '未確かめ', '自己対戦にプレイヤーが居ない（start_ultimatum 0）。ソース：プレイヤーは受け入れか開戦を選ぶ・AI は SideMilitary の比で決まる（Diplomacy.cs:1374〜1405）', '—', '残す')
r('S25-28', '未確かめ', '挑発のならし（_tensionRamp）の列が無い', '計器 dipten_s（理由 Provocation・Appeasement）', '残す')
r('S25-29', '未確かめ', '挑発と宥和の回数の列が無い。ソース：格上 2.5 倍で宥和（Diplomacy.cs:1724・:1740）・宿敵で挑発（:1728〜1738）', '計器 dipev_s', '残す')
r('S25-30', '未確かめ', '理由 ProvocationSpread の内訳の列が無い', '計器 dipten_s', '残す')
r('S25-31', '未確かめ', '正統性の内訳の列が無い（Explain だけ）', '—', '残す')
r('S25-32', '一部だけ働く', 'ソース：挑発の禁輸（Diplomacy.cs:1923）は緩和（EaseTension :1927〜1933）では外れない＝外れるのは終戦（:2978）・属国化（:1256）・国の除去（:396）だけ＝片方向の錠（README §1-8）。効きは S25-24 と同じ 1 本の倍率。禁輸の組の数の列が無い', '計器 dipten_s の禁輸の印', '直す（候補 L3・緩和でも外す＋組ごとの門）', typ='片側だけの門')
r('S25-33', '一部だけ働く', 'monai：MilitaryExportBan を AI が動かした国 M 60〜64／種（N1 60〜65・N2 57〜64）＝法は動く。効きは緊張≧60 の相手の交易比重を売りの全体から削る 1 本の倍率（Diplomacy.cs:686〜692→BilateralMarket.cs:270・:750）＝相手を選ばない', '交戦中の国へ売らない形（11-11 問い4）', '直す（候補 L3 と同じ線）')
r('S25-34', '一部だけ働く', 'S24-H035 と同じ：取引ごとの危険料は生きる・国の値 World.LastBlockadeRisk は書き手 0（World.cs:1105・代入の行が無い）', 'S09 R-a', '直す（S09 R-a）')
r('S25-35', '働かない', '原資が読む LastBlockadeRisk の書き手が今も 0（Diplomacy.cs:726）＝賞金は常に 0（(907) と同じ）', 'S09 R-a', '残す（上流待ち）：書き手が入らないと決まれば S25-36 と一緒に消す（恒等）')
r('S25-36', '働かない', 'S25-35 と同じ', 'S09 R-a', '残す（上流待ち・S25-35 と同じ）', extra='Diplomacy.cs:729:raiderNaval')
r('S25-37', '未確かめ', 'S23 の持ち物（観戦 U6）＝二度確かめない', 'S23', '—')
r('S25-38', '一部だけ働く', '宗主と属国の組の開戦（近似・離反は行が無い＝上限）：入口 main 0.15〜0.72%・mediation 0.25〜0.80%（＝読みの雑音）に対し ally 2.73〜3.12%（M 59・N1 50・N2 45／10 種・張って 8 四半期以上が 85〜94%）＝同盟の参戦の入口（JoinAllies Diplomacy.cs:1595〜1598）は従属を見ない。終局の属国（近似）は経済を持つ国の 79〜93%（M 85〜90%）', 'S25-V02', '直す（' + EFG + '・段 G で従属ごと消す）', typ='両側の門')
r('S25-39', '働く', 'fiscal（計器の国）：貢納を受けた月 M 2,437〜3,603／種（N1 2,611〜4,110・N2 2,606〜4,231・S11 2,442〜4,283）', '段 F で外国の取り分の税へ（支配の力 §3-8）', '残す（' + EFG + '）')
r('S25-40', '一部だけ働く', '張りの行から組み立てた終局の張り−最終行の張り＝離反と国の消滅の上限 M 4.3・N1 6.2・N2 4.2／種（入口は 77〜126／種）＝出口は実質ほぼ閉じる（観戦〈属国だらけ〉）', '—', '消す（' + EFG + '・支配の力 §2-1〈離反は消す〉）')
r('S25-41', '働く', 'wars entryCode suzerain M 59〜122／種（N1 61〜119・N2 32〜97・S11 43〜130）。見送り（厭戦・届かない）の側は列なし', '—', '残す（' + EFG + '）')
r('S25-42', '一部だけ働く', '上限 8 は侵略の可否の門だけ（warentry r_maxDep M 1〜171／種）。講和の属国化と保護国（Diplomacy.cs:2755〜2770）は上限を見ない（引き継ぎ :2802 は見る）＝組み立てた従属（離反の行が無い＝上限）で 1 宗主の最大 M 7〜10（N1 6〜9・N2 5〜9）', '—', '消す（' + EFG + '・段 G）')
r('S25-43', '働く', 'cpmed forcedFlag（調停の白紙）M 22〜35／種（N1 16〜35・N2 12〜36）・拒否の側＝S25-45', '—', '残す')
r('S25-44', '未確かめ', '自己対戦にプレイヤーが居ない', '—', '残す')
r('S25-45', '働く', 'wars entryCode mediation（拒否→提案国の参戦）M 29〜56／種（N1 36〜50・N2 25〜50・S11 25〜61）', '—', '残す')
r('S25-46', '働く', 'cpmed（講和）M 471〜656／種。属国化の段＝dep ResolvePeace:2760 M 12〜15／種・保護国 :2769 は 10 種で 1。深く読まない', '支配の力 §4-3（寄せ切り）', '消す（' + EFG + '）')
r('S25-47', '働く', '(907) の判定を写す（割譲の行は S24-H052）。深く読まない', '—', '残す（' + EFG + '）')
r('S25-48', '働く', '(907) の判定を写す（S24-H047）。深く読まない', '—', '残す（' + EFG + '）')
r('S25-49', '働く', 'rule：条約港が効いている州×四半期 M 1,289〜7,697／種（N1 331〜6,379・N2 471〜4,779）。深く読まない', 'この波の c111tp（読み手から植民地格の港を外す）', '消す（' + EFG + '・支配の力 §2-1）')
r('S25-50', '働く', '(907) の判定を写す。深く読まない', 'c111tp', '消す（' + EFG + '）')
r('S25-51', '働く', 'dep ResolvePeace:2813 M 29〜64／種（N1 25〜61・N2 25〜54）。そのうち交戦中の組に張った M 1〜5／種（10 種で 18・N1 12・N2 6）＝V02 の入口 3', 'S25-V02', '消す（' + EFG + '）')
r('S25-52', '未確かめ', 'S24-H041・H042 の持ち物（v の指摘 1＝厭戦の枝はエンジンの線 0.9×RevCut で数え直す）', 'S24', '—')
r('S25-53', '働く', 'S24-H059 の判定を写す（敗戦の衝撃＞0.05 の国×四半期 11.0〜20.8%）。scalars：衝撃≧0.3 の国×四半期 M 5.4〜10.0%', '—', '残す')
r('S25-54', '働く', 'invade（国のある標的）M 64〜94／種・屈服 40〜48（約 55%）＝両側。首都の大圏 km の中央は屈服 1,377・開戦 2,134（投射を掛けた実効戦力＝S25-26 と物差しが違う）', '屈服の 1 本化', '直す（' + EFG + '）', typ='両側の門')
r('S25-55', '働く', 'warentry：可否の評価 M 72,369〜85,110／種・通った r_ok 652〜990・理由の拒否が全て出る。呼び手は 1 本（World.CanInvade）', '—', '残す（' + EFG + '）')
r('S25-56', '働かない', '関数は消えた（SimCore で ExpeditionLift 0 件）。注の残り：ColonizeForce と ColonizeWillCost の注が〈×遠征技術〉と書く（Diplomacy.cs:2039・:2157）', '—', '消す（注・恒等）')
r('S25-57', '一部だけ働く', 'warentry calls M 882〜1,037／種・r_ok 652〜990。AI の標的の門 AiColonizeMinProjection 0.2（Diplomacy.cs:1788）は陸続きで閉じない（S25-25 と同じ算術）＝観戦 Q6', '段 F（州ごとの届き）', '直す（' + EFG + '）')
r('S25-58', '働く', 'ソース：屈服でも開戦でも侵略の要求ごとに必ず通る（Diplomacy.cs:1995）＝発火は invade の行 M 120〜147／種と調停の回（:2076）。量は理由 Colonial の内訳の列が無い', '—', '残す')
r('S25-59', '一部だけ働く', 'wars entryCode declare（統一）M 10 種で 5・N1 2・N2 1・S11 2。S24-H017 と同じ', '作者 2026-10-20「統一戦争は名指ししないという原則に反しているので、廃止寄りです」', '消す（候補 D3＝S24 と同じ）')
r('S25-60', '一部だけ働く', 'ソース：陸続きでは距離の減衰が 0.2 を割らない（S25-25）＝届きの門は海越えでだけ効く', '段 F', '直す（' + EFG + '）')
r('S25-61', '休眠', 'ProjectionRuleMin＝1.1（到達不能＝首都だけ）・ProjectionUsesLand＝0', '段 F', '残す（' + EFG + '・段 F で投射ごと畳む）')
r('S25-62', '働く', '(907) の判定を写す。Landmass.Same は同盟・海越え・割譲が共有', '—', '残す')
r('S25-63', '未確かめ', '偏り TariffRetaliationBias の列が無い（tariffTargetIndex は目標の和）。ソース：Diplomacy.cs:1654〜1675 が全ての国の関税の目標へ ±0.25 まで足す（Government.cs:185）', '作者の問いの材料（関税は国内の利害と議会で決める＝(604)）', '残す')
r('S25-64', '働かない', '(604) で消した後・読み手 0', '—', '—', extra='PriceOfBread.Tests/DormantIdentityTests.cs:321:TradeTreatyEnabled')
r('S25-65', '一部だけ働く', 'ソース：WorthFighting の投射の門は陸続きで閉じない（S25-25）。抽象の標的は植民地 0 なら開戦しない（Diplomacy.cs:1311〜1312）', 'S24-V02', '直す（' + EFG + '）')
r('S25-66', '働く', 'scalars：厭戦＞0.5 の国×四半期 M 6.8〜10.1%（N1 5.6〜9.7%・N2 5.3〜10.1%・S11 4.5〜9.8%）＝門の入力が両側に出る。閉じた月の内訳は列なし', '—', '残す', typ='両側の門')
r('S25-67', '働く', 'dep SeedDependencies 3／種（全ての腕）', '—', '残す')
r('S25-68', '働く', 'warpair secessionResets M 10 種で 4（N1 6・N2 3）＝稀に出る', '—', '残す')
r('S25-69', '働く', '(907)：削りは上限 0.5 に張り付く（天井の錠の型）。深く読まない', '賠償は段 F で消す（言葉 6）', '消す（' + EFG + '）')
r('S25-70', '働く', 'imp M 23〜50／種（N1 18〜50・N2 13〜48）', '言葉 6（矯正講和は消す）', '消す（' + EFG + '）')
r('S25-71', '未確かめ', 'S24 の割譲（H052）の持ち物。深く読まない', '—', '残す（' + EFG + '）')
r('S25-72', '休眠', 'SubmitProbabilistic＝0（作者の温存）', '屈服の 1 本化', '残す（' + EFG + '）')
r('S25-73', '一部だけ働く', 'S25-40 と同じ（離反は上限 4.2〜6.2／種）', '—', '消す（' + EFG + '）')
r('S25-74', '文書が古い', '(271) で消した後・読み手 0', '—', '—', extra='PriceOfBread.Tests/DormantIdentityTests.cs:284:WarStartPoliticsScale')
r('S25-75', '働かない', '2026-10-18 に消した後・読み手 0', '—', '—', extra='PriceOfBread.Tests/ScalarDeletionTests.cs:70:DiploCore')
r('S25-76', '働く', '(907) の判定を写す（計器）', '—', '残す')
r('S25-77', '働く', '(907) の判定を写す（計器・母集合は StartWar と内戦だけ）', '—', '残す')
r('S25-V01', '働く', 'dep：輪の印（cycleFlag）M 10 種で 5・N1 19・N2 3＝切る枝が通る。張った後の輪の数 cyclesAfterCount は 30 種の全てで 0', '—', '残す（段 G まで不変条件の番人の行）', typ='不変条件')
r('S25-V02', '一部だけ働く', '入口 1（宣戦・時計・調停）は閉じる（従属の組の開戦は読みの雑音 0.15〜0.80%）。入口 2（同盟の参戦）は開く：ally の 2.73〜3.12%（M 59／10 種）。入口 3（引き継ぎ）は交戦中に張る：M 18・N1 12・N2 6／10 種（out_v02_*.txt・近似）', '直す線 L4（JoinAllies に InDependency・引き継ぎは交戦中の組を外す）', '直す（' + EFG + '・段 G まで）', typ='両側の門')
for rid in old:
    if rid not in {x[0] for x in R}: raise SystemExit('行が欠けた ' + rid)
L = ['\t'.join(H)] + ['\t'.join(x) for x in R]
open(os.path.join(W, 'ledger_S25_2026-11-14.tsv'), 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))
from collections import Counter
print(len(R), Counter(x[8] for x in R))
print(Counter(x[11].split('（')[0] for x in R))
print('EFG', sum(1 for x in R if EFG in x[11]))
