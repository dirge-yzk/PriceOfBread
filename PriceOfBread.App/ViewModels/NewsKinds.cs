using System;
using System.Collections.Generic;

namespace PriceOfBread.App.ViewModels;

/// <summary>年表へ流す出来事の種類の表（作者の指示 2026-11-22「年表は不要な情報が多すぎる」）。
/// 外交ログ（世界の出来事の唯一の出どころ）の 1 行を、種類と取り扱いに分ける＝上から順に最初に当たった行が勝つ（消す物を先に並べる）。
/// 残す物＝戦争の始まりと終わり・国境が変わる事・体制が変わる事・自国の法と政権と発見。消す物は年表に流さない（作るのをやめる）。
/// 当たる行が無い本文は残す側へ倒す（迷う物は残す）＝種類は「未分類」。</summary>
public static class NewsKinds
{
    public enum Scope { Drop, World, Mine }

    private static readonly (string Needle, string Kind, Scope Scope)[] Table =
    [
        // ---- 消す物（年表に流さない）----
        ("（革命の成就）", "内戦の決着（四半期の末の再統合の行と重なる）", Scope.Drop),
        ("内戦を鎮め、", "内戦の決着（四半期の末の再統合の行と重なる）", Scope.Drop),
        ("要請を見送り", "参戦要請の見送り", Scope.Drop),
        ("参戦要請を拒否", "参戦要請の拒否", Scope.Drop),
        ("調停を拒んだ", "調停の拒否", Scope.Drop),
        ("同盟が破棄", "同盟の破棄", Scope.Drop),
        ("防衛同盟を締結", "同盟の締結", Scope.Drop),
        ("外国区分）", "条約港の持分", Scope.Drop),
        ("住民は州へ", "植民地の州への吸収", Scope.Drop),
        ("へ遷都", "遷都", Scope.Drop),
        ("が開通", "要衝の開通", Scope.Drop),
        ("は立ち行かず", "反乱国の自壊", Scope.Drop),
        ("政府は", "内戦の細部", Scope.Drop),
        ("が結党した", "結党", Scope.Drop),
        ("公約を果たした", "公約", Scope.Drop),
        ("要求を掲げた", "勢力の要求", Scope.Drop),
        ("要求を法で受け入れた", "勢力の要求", Scope.Drop),
        ("別の軸で譲歩", "勢力の要求", Scope.Drop),
        ("要求をのんだ", "勢力の要求", Scope.Drop),
        ("一揆", "一揆", Scope.Drop),
        ("剥離＝", "特権の剥離", Scope.Drop),
        ("旗を掲げた", "勢力の旗揚げ", Scope.Drop),
        ("に合流した", "勢力の合流", Scope.Drop),
        ("通貨改革", "通貨改革", Scope.Drop),
        ("兌換停止", "兌換停止", Scope.Drop),
        ("旧設備の座礁", "州の技術の採用", Scope.Drop),
        ("の職人が没落", "州の技術の採用", Scope.Drop),
        ("が凶作に見舞われた", "凶作", Scope.Drop),
        ("病虫害", "病虫害", Scope.Drop),
        ("で疫病が流行", "疫病", Scope.Drop),
        ("大鉱脈", "資源の発見", Scope.Drop),
        ("植林が始まった", "資源の発見", Scope.Drop),
        ("landmass", "内部の注意", Scope.Drop),
        ("陸戦で", "個々の戦闘", Scope.Drop),
        ("海戦で", "個々の戦闘", Scope.Drop),
        ("陸戦と海戦で", "個々の戦闘", Scope.Drop),
        // ---- 残す物：戦争の始まりと終わり ----
        ("開戦", "戦争の始まり", Scope.World),
        ("に宣戦", "戦争の始まり", Scope.World),
        ("戦争に加わった", "戦争の始まり", Scope.World),
        ("矛を収めた", "戦争の終わり", Scope.World),
        ("白紙で講和", "戦争の終わり", Scope.World),
        ("に勝利し、", "戦争の終わり", Scope.World),
        ("膠着のまま終わり", "戦争の終わり", Scope.World),
        // ---- 残す物：国境が変わる事 ----
        ("成立を宣言", "国の成立と改名", Scope.World),
        ("独立を宣言", "独立", Scope.World),
        ("独立を求めて立った", "独立", Scope.World),
        ("独立が確定", "独立", Scope.World),
        ("離反", "独立", Scope.World),
        ("侵略", "併合と従属", Scope.World),
        ("属国に", "併合と従属", Scope.World),
        ("保護国にした", "併合と従属", Scope.World),
        ("に屈服", "併合と従属", Scope.World),
        ("宗主権を引き継ぐ", "併合と従属", Scope.World),
        ("を割譲させた", "割譲", Scope.World),
        ("条約港を開かせた", "条約港", Scope.World),
        ("再併合", "再統合", Scope.World),
        ("再統一", "再統合", Scope.World),
        ("平和的に復帰", "再統合", Scope.World),
        ("への統一を掲げ", "統一", Scope.World),
        // ---- 残す物：体制が変わる事 ----
        ("内戦", "内戦の始まりと終わり", Scope.World),
        ("蜂起", "内戦の始まりと終わり", Scope.World),
        ("政体を改めさせた", "政体の変更", Scope.World),
        ("王冠を廃した", "政体の変更", Scope.World),
        ("幕府が倒れ", "政体の変更", Scope.World),
        ("統治が変わった", "政体の変更", Scope.World),
        // ---- 残す物：自国だけ ----
        ("を発見", "発見", Scope.Mine),
        ("繰り上げ総選挙", "政権の交代", Scope.Mine),
    ];

    /// <summary>外交ログ 1 行（年の印を除いた本文）の種類と取り扱い。当たる行が無ければ「未分類」・残す側。</summary>
    public static (string Kind, Scope Scope) Classify(string body)
    {
        foreach (var (needle, kind, scope) in Table)
            if (body.Contains(needle, StringComparison.Ordinal)) return (kind, scope);
        return ("未分類", Scope.World);
    }

    /// <summary>この行を年表へ流すか。自国だけの種類は自国が当事者のときだけ。</summary>
    public static bool Passes(Scope scope, bool mentionsMe) => scope == Scope.World || (scope == Scope.Mine && mentionsMe);
}
