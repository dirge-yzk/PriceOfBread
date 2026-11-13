using System.Runtime.CompilerServices;

// 表示の層（旧 SimCore/Display）の internal を、読む側（App・Runner・画面の試験）へ見せる。
[assembly: InternalsVisibleTo("PriceOfBread.App")]
[assembly: InternalsVisibleTo("PriceOfBread.Runner")]
[assembly: InternalsVisibleTo("PriceOfBread.Tests.Ui")]
