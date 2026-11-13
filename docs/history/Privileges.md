# PriceOfBread.SimCore/Privileges.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-6 W 直後 `public static class Privileges`
````
/// <summary>★特権の台帳（作者設計 2026-09-06「特権システム＝制度疲労の置換」§9 段1＝観測のみ・既定 <c>PrivilegeLedgerEnabled=0</c> で1命令も走らない）。
/// 特権＝(受益クラス, 取り分, 歪み, 法の軸, 強度ストック)。段1は既存の3破片（P1 十分の一税・P2 身分制免税・P3 身分任用＝汚職の取り分）の
/// **取り分を記帳**し、**強度ストック**（保有年数÷DominanceMatureYears＝既得権化・DominanceIsStock と同じ作法＝タイマーでなく物理）を積む。
/// 段6の制度疲労の導出は (373) で削除した。段3の剥離の怒り（<c>PrivilegeStripMilitancy</c>）は作者 2026-10-11「残す」で StepMonth に残る（台帳が立たないと動かない）。新しい数は作らない。</summary>
````

## L9-9 W 直後 `public const int Tithe = 0;          // 聖職者：十分の一税（TitheRate＋国教法）`
````
// 第1段＝実体のある4本（作者設計「法と特権の仕分け」§8・2026-09-06）
````

## L10 T `public const int Tithe = 0;`
````
// 聖職者：十分の一税（TitheRate＋国教法）
````

## L11 T `public const int TaxExemption = 1;`
````
// 貴族・聖職：身分制免税（PrivilegeTaxDiscount×王権の取り分）
````

## L12 T `public const int VestedOffice = 2;`
````
// 官僚：既得の官職（BureaucracyLaw の身分度×汚職で官僚へ渡った分）
````

## L13 T `public const int Seigneurial = 3;`
````
// 貴族：領主権（農奴解放法の未解放度×RGOの地代）
````

## L14 T `public const int Monopoly = 4;`
````
// 資本家：特許会社（茶の独占レント＝東インド会社。塩の王室のgabelleは国庫収入＝階級の特権でないので数えない）★段5前半（2026-09-07）
````

## L15 T `public const int Guild = 5;`
````
// 職人：同業組合（参入制限のレント＝自営レシピの製品への上乗せ・GuildLaw）★段4（2026-09-07）
````

## L18 T `public static readonly int[] Beneficiary = [PopClass.Clergy, PopClass.Noble, PopClass.Bure`
````
// ClassId（PopClass は int 定数の静的クラス）
````

## L20-20 W 直後 `public static double EstateDegree(World w) => EstateDegreeFor(w.Bureaucracy);`
````
/// <summary>身分の度合い（猟官1／資格任用0.5／専門0）＝PowerShares.PrivilegeIntact と同じ物差し。</summary>
````

## L22-22 W 直後 `public static double EstateDegreeFor(BureaucracyLaw b) => b switch { BureaucracyLaw.Spoils`
````
/// <summary>(1039) 段を受ける形（身分の度合いの 1 本の定義・上の <see cref="EstateDegree"/> はこれを呼ぶ）。</summary>
````

## L24-24 W 直後 `public static double LandReformDegreeFor(ReformLevel lr) => lr switch { ReformLevel.None =`
````
/// <summary>(1039) 段を受ける形（土地改革の項＝なし1／限定0.5／全面0）。</summary>
````

## L26-27 W 直後 `public static double SeigneurialDegree(World w) => w.SerfEmancipation switch { ReformLevel`
````
/// <summary>領主権の残高＝農奴解放の未解放度（なし1／限定0.5／全面0）＝PowerShares.PrivilegeIntact と同じ物差し。
/// ★農民の移動の自由（入植の送り元・社会移動の農村の池・農村→都市の流れ）は 1−これ（2026-09-28 処理の一元化 段1＝旧は同じ switch の写しが6箇所）。</summary>
````

## L29-31 W 直後 `public static double IntermediaryDegree(World w) => IntermediaryDegreeFor(w.Bureaucracy, w`
````
/// <summary>★(399) 仲介者（徴税請負・藩・ザミーンダール・土司・領主の裁判権）が握る権利の残高∈[0,1]＝在地の区分の目標を決める量（<c>Politics.CheckSecession</c>）。
/// 官職と徴税の私有〔<see cref="EstateDegree"/>＝官僚制度法 猟官1／資格任用0.5／専門0＝台帳の 既得の官職〕と地代を取る権利〔土地改革の法 なし1／限定0.5／全面0〕の大きい方＝どちらかが残る限り仲介者の権利は残る。
/// 農奴解放の法は読まない（人の身分の法であって州の支配の法ではない・作者 2026-10-16）。開幕は全ての国で土地改革＝なし＝1.0。純関数・新しい状態なし。</summary>
````

## L33-33 W 直後 `public static double IntermediaryDegreeFor(BureaucracyLaw b, ReformLevel lr) => System.Mat`
````
/// <summary>(1039) 段を受ける形（官僚制度と土地改革の段を引数で受ける＝一段ずらした仲介者の度合いを同じ 1 本で読む・D-2 の受け口）。</summary>
````

## L35-35 W 直後 `public static double GuildDegree(World w) => w.Guild switch { GuildLaw.Full => 1.0, GuildL`
````
/// <summary>同業組合の残高＝法の度合（保護1／制限0.5／廃止0）＝上の2つと同じ物差し（★段4・GuildLawGate=0 でも読めるが上乗せ係数0で効かない）。</summary>
````

## L38-39 W 直後 `public static double TitheDegree(World w, Coefficients c) => TitheDegreeFor(w.StateReligio`
````
/// <summary>十分の一税の残高＝国教法で変調した実効率÷基準率（国教のみ1／内心の自由・信仰の自由は係数／国家無神論0）。
/// ★十分の一税の徴収（Fiscal.Step の税の走査）と同じ1本の定義（同じ式を2箇所に書かない・2026-09-07）。TitheRate=0 の世界は0＝特権そのものが無い。</summary>
````

## L41-41 W 直後 `public static double TitheDegreeFor(StateReligionLaw s, Coefficients c)`
````
/// <summary>(1039) 段を受ける形（国教の段を引数で受ける＝一段ずらした十分の一税の度合いを同じ 1 本で読む・D-1 の受け口）。</summary>
````

## L46-47 W 直後 `public static double TaxExemptionDegree(World w, Coefficients c, double crownShare)`
````
/// <summary>身分制免税の割引率＝閉鎖階級（貴族・聖職）の課税ベースから抜ける割合。
/// ★<see cref="TaxAssessment"/> と同じ1本の定義（2026-09-07に重複を畳んだ・1.0−これ＝旧式とビット同一）。</summary>
````

## L54-55 W 直後 `public static double MonopolyDegree(World w, Coefficients c)`
````
/// <summary>特許会社の残高＝独占レントがまだどこかに立つか。州の度合は物理側の唯一定義 <see cref="EconomyStep.MonopolyDegree"/>（premium=1＝度合[0,1]）を呼ぶ＝
/// 独禁法で縮む（MonopolyRentFactor）。州ごとに違うので**最大**＝「その特権がまだ在るか」の答え（表示のみ・力学の読み手なし）。</summary>
````

## L67-68 W 直後 `public static double Degree(World w, Coefficients c, int kind) => kind switch`
````
/// <summary>特権 k の残高∈[0,1]＝法・政体・工業化だけから決まる観測量（新しい状態を作らない・2026-09-07 POPタブの行が読む）。
/// 1783年の初期値は各国の初期法（world.json の polity／initialSerfEmancipation／bureaucracy）がそのまま決める＝国名分岐はゼロ。</summary>
````

## L72 T `TaxExemption => TaxExemptionDegree(w, c, Fiscal.CrownShareForPrivilege(w, c)),`
````
// Fiscal.Step・予算表の予想と同じ 1 本（既定は PowerAffinity を呼ばない）
````

## L80-80 W 直後 `public static bool Enabled(Coefficients c, int kind) => kind switch`
````
/// <summary>その特権の機構が今の世界で1銭でも動くか（＝表に出す資格）。休眠ゲートで止まっている特権は「在る」と見せない。</summary>
````

## L85 T `VestedOffice => true,`
````
// 身分任用の取り分＝汚職の分配経路（BureaucracyLaw）にゲートは無い
````

## L86 T `Seigneurial => true,`
````
// 領主権＝RGOの地代（農奴解放法）にゲートは無い
````

## L88 T `Guild => c.GuildLawGate > 0 && c.GuildRentPremium > 0,`
````
// EconomyStep の guildRent の点火条件と同じ
````

## L92-93 W 直後 `public static bool HeldBy(int kind, int classId)`
````
/// <summary>特権 k を実際に受ける階級か。<see cref="Beneficiary"/> は台帳の**代表1クラス**（段6の発言権集計）で、
/// 身分制免税だけは閉鎖階級すべて（貴族と聖職者）が受ける＝<see cref="TaxAssessment"/> の p.Closed と同じ条件。</summary>
````

## L97-99 W 直後 `public static double TaxAssessment(World w, Coefficients c, Pop p, double crownShare)`
````
/// <summary>身分制免税＝課税査定の係数（1.0＝免税なし）。★Fiscal から**移設**（2026-09-06・式はビット同一・旧経路は削除）。
/// PrivilegeTaxDiscount=0 で 1.0（乗算恒等）。PrivilegeFromCrownShare>0 で王権の取り分に比例する連続量、0 で政体ラベルの集合判定（旧）。
/// ★2026-09-07：割引率を <see cref="TaxExemptionDegree"/> へ畳んだ（同じ式を2箇所に書かない・1.0−0.0＝1.0 でビット同一）。</summary>
````

## L103-104 W 直後 `public static void Record(World w, int kind, double take)`
````
// 【墓標2026-09-30・支払いの一元化 段3-4（B6）】CollectTithe（十分の一税の徴収の走査・税の後の残りの貯蓄から）を削除した。
//   十分の一税の請求は Fiscal.Step の税の走査で税・領主の取り分と一緒に集め、POP ごとに1本の率 r_p で払う（国家が教会に優先しない）＝率は TitheDegree の1本。
````

## L106-106 W 直後 `public static void Record(World w, int kind, double take)`
````
/// <summary>今月の取り分を記帳（pop→pop の移転額・貨幣は既に移動済み＝ここは記帳のみ）。</summary>
````

## L109 T `if (kind >= w.PrivilegeTakeMonth.Length) return;`
````
// 旧セーブ（短い配列）を守る＝落とさない（その特権は0のまま）
````

## L110 T `if (!(take > 0)) return;`
````
// 否定形＝NaN も捨てる
````

## L114-114 W 直後 `public static void StepMonth(World w, Coefficients c)`
````
/// <summary>月次＝取り分を年換算の指数平均へ畳み、強度ストック（保有年数）を積む/減らす。活性＝今月の取り分>0。</summary>
````

## L121 T `w.PrivilegeTakeYear[k] = w.PrivilegeTakeYear[k] * (1.0 - dt) + take;`
````
// ≈直近1年の取り分（指数平均・単位＝貨幣/年）
````

## L123-130 W 直後 `if (c.PrivilegeStripMilitancy > 0 && !active && w.PrivilegeHeldYears[k] > 0 && w.Privilege`
````
// ★段3 剥離の怒り（作者設計 §5-2・二量分離の設計メモ §7-6）：活性→不活性の落ちた月に、受益層の闘争度 += 強度（保有年数÷世代長）× PrivilegeStripMilitancy。
//   長く持っていたものほど荒れる（1789・1861・1876・1905）。0 で加算なし（恒等）。⚠取り分が立たない月の揺れを剥離と誤認しないよう、強度が正の特権だけ。
//   ★**インパルス**であって積分ではない（史実16件はすべて日付のある法令が引き金・中央値2か月＝飢餓型6か月の3倍速い）。
//     `w.PrivilegeLastActive[k]`（前月の活性）との連言で**落ちた月に1回だけ**書く＝翌月からは積まない。タイマーではなく状態の遷移。
//   ★なぜ闘争度（ストック）側か＝動員される層は生活水準が平均より**上**なので不満度（生活の失望＋法の隔たり）からは構造的に出ない
//     ＝段3の参加の述語（ParticipationDiscontent）は max(不満度, 闘争度) を読むので、この上書きが無いと貴族・聖職は運動に一度も加わらない。
//   ⚠受益層の判定は <see cref="HeldBy"/> **1本**（★2026-09-09 是正）：旧は Beneficiary[k]（台帳の代表1クラス）と直接比べており、
//     身分制免税だけは**貴族と聖職の両方**から取り分を記帳しながら怒るのは貴族だけ＝取り分の分子と怒りの分母が違う人口だった。
````

## L138-139 W 直後 `if (w.PrivilegeHeldYears[k] < 0) w.PrivilegeHeldYears[k] = active ? c.DominanceMatureYears`
````
// ★初期化（最初の月）：1783年に活きている特権は**既に世代長ぶん既得権化している**（旧体制は何世紀も前から）＝保有年数を DominanceMatureYears から始める。
//   活きていない特権は0から。（開幕の全州＝中核=1.0 の Dominance と同じ作法）。未初期化の印＝負値。
````

## L146-146 W 直後 `public static double Strength(World w, Coefficients c, int kind)`
````
/// <summary>強度∈[0,1]＝保有年数÷世代長（DominanceMatureYears）。剥離の怒り（段3）はこれに比例する。</summary>
````

