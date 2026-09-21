using PriceOfBread.SimCore;

namespace PriceOfBread.App;

/// <summary>ゲーム内enumの日本語表示（VMの法律表示テキスト・転機通知の文言用）。
/// XAML参照ゼロにつき IValueConverter 実装は撤去し静的クラス化（バッチ3・2026-07-16）。</summary>
public static class JaEnumConverter
{
    public static string Ja(object? value) => value switch
    {
        EducationLaw e => e switch { EducationLaw.None => "教育なし", EducationLaw.Religious => "宗教学校", EducationLaw.Public => "公教育", _ => "義務教育" },
        CensorshipLaw c => c switch { CensorshipLaw.StatePressOnly => "国営報道のみ", CensorshipLaw.Censorship => "検閲", _ => "自由報道" },
        LaborUnionLaw l => l switch { LaborUnionLaw.Illegal => "非合法", LaborUnionLaw.Controlled => "官許", _ => "容認" },
        WorkHours w => w switch { WorkHours.Eight => "8時間", WorkHours.Ten => "10時間", WorkHours.Twelve => "12時間", _ => "無制限" },
        Suffrage s => s switch { Suffrage.None => "参政権なし", Suffrage.Census => "制限選挙", Suffrage.AdultMale => "男性普通選挙", _ => "男女普通選挙" },
        ReformLevel r => r switch { ReformLevel.None => "なし", ReformLevel.Limited => "限定的", _ => "全面" },
        SocialSecurityLaw ss => ss switch { SocialSecurityLaw.PoorLaw => "救貧法", SocialSecurityLaw.WorkersComp => "労災", SocialSecurityLaw.Unemployment => "失業保険", _ => "年金" },   // 弧3-3（2026-07-24）：段上げニュース用
        WomenRightsLaw wr => wr switch { WomenRightsLaw.None => "なし", WomenRightsLaw.Property => "財産権", WomenRightsLaw.Education => "教育権", _ => "選挙権" },   // ★女性の権利の梯子（統合法・2026-08-10）＝法律タブの段ラベルと同じ語彙
        ColonialSuffrageLaw cs => cs switch { ColonialSuffrageLaw.None => "なし", ColonialSuffrageLaw.OwnNationals => "自国民のみ", _ => "全面" },   // 参政権の範囲2法（2026-08-16）＝法律タブの段ラベルと同じ語彙
        _ => value?.ToString() ?? "",
    };
}
