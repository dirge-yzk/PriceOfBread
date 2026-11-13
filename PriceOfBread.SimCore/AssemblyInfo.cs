using System.Runtime.CompilerServices;

// テストから internal メンバ（World の setter 等）へアクセスできるようにする。
[assembly: InternalsVisibleTo("PriceOfBread.Tests")]
// 診断ツール（Runner の lawaudit 等）からも internal の World setter・法律値へアクセスできるようにする（A/B対照で機能検査するため）。
[assembly: InternalsVisibleTo("PriceOfBread.Runner")]
// UI（戦争タブ・国内勢力の強さ比表示）から internal の純関数（Politics.LoyalistMilitia）を読めるようにする（表示のみ・書き込みには使わない・2026-08-16）。
[assembly: InternalsVisibleTo("PriceOfBread.App")]
// 表示の層（PriceOfBread.Display）と画面の試験から SimCore の internal を読む（旧 SimCore/Display は同じアセンブリだった）。
[assembly: InternalsVisibleTo("PriceOfBread.Display")]
[assembly: InternalsVisibleTo("PriceOfBread.Tests.Ui")]
