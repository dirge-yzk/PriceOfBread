namespace PriceOfBread.App.ViewModels;

/// <summary>ポインタが実際に動いたかの門。開始・読み込みの直後は、描画の変化で窓がポインタの動きの知らせを送ることがある
/// （ポインタは止まったまま）。その知らせでホバーを上書きすると、窓「州」に自国の首都でなく、たまたまポインタの下にあった他国の州が出る。
/// 最初に見た位置から <see cref="Slop"/> 画素以上離れるまでは「動いていない」とみなす。</summary>
public sealed class PointerMoveGate
{
    public const double Slop = 3.0;
    private double _x = double.NaN, _y;
    private bool _moved;

    /// <summary>この位置の知らせを実際の動きとして扱ってよいか。いちど動いたら以後は常に true。</summary>
    public bool Moved(double x, double y)
    {
        if (_moved) return true;
        if (double.IsNaN(_x)) { _x = x; _y = y; return false; }
        if (System.Math.Abs(x - _x) >= Slop || System.Math.Abs(y - _y) >= Slop) _moved = true;
        return _moved;
    }

    public void Reset() { _x = double.NaN; _moved = false; }
}
