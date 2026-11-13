using System.Runtime.CompilerServices;

// 画面の試験（PriceOfBread.Tests.Ui）が、エンジンの試験の補助（GoodIdGuardTests.RepoRoot・OpeningFixture ほか）の internal を読む。
[assembly: InternalsVisibleTo("PriceOfBread.Tests.Ui")]
