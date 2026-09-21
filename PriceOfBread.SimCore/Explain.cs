namespace PriceOfBread.SimCore;

/// <summary>
/// 説明台帳（Explanation API・タスク#63）。各数値が「なぜこの値なのか」を因果分解して保持する。
/// たとえば 正統性=0.42 ← 信任の満額+1.00 ／ 制度ギャップ−0.31 ／ 腐敗−0.15 ／ マンデート行使−0.12。
///
/// **設計の要（挙動非依存の保証）**：計算現場は必ず null 条件演算子 <c>w.Explain?.Add(...)</c> で記帳する。
/// C# では対象が null のとき引数式そのものが評価されないため、<see cref="World.Explain"/> が null（＝説明モードオフ・既定）
/// なら記帳呼び出しは丸ごと短絡し、寄与量の算術も走らない＝**ゼロコストかつ本計算とビット恒等**。
/// 台帳は本体計算を一切書き換えず、計算した実寄与量を横で観察して溜めるだけ。装着しても数値は1ビットも動かない。
/// </summary>
public sealed class ExplainLedger
{
    /// <summary>内訳の一項＝ある理由が対象数値に加えた実寄与量（符号つき）。</summary>
    public readonly record struct Term(string Reason, double Delta);

    private readonly Dictionary<string, List<Term>> _quantities = new();

    /// <summary>ある数値の内訳を作り直す前に前期の記録を消す（毎四半期の再計算に対応）。</summary>
    public void Reset(string quantity)
    {
        if (_quantities.TryGetValue(quantity, out var list)) list.Clear();
    }

    /// <summary><paramref name="quantity"/> に理由 <paramref name="reason"/> が寄与量 <paramref name="delta"/> を加えた、と記帳する。</summary>
    public void Add(string quantity, string reason, double delta)
    {
        if (!_quantities.TryGetValue(quantity, out var list))
        {
            list = new List<Term>();
            _quantities[quantity] = list;
        }
        list.Add(new Term(reason, delta));
    }

    /// <summary><paramref name="quantity"/> の内訳（理由と寄与量の並び）。未記録なら空。</summary>
    public IReadOnlyList<Term> Get(string quantity)
        => _quantities.TryGetValue(quantity, out var list) ? list : System.Array.Empty<Term>();
}
