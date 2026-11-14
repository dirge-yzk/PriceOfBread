# (1130) c110lo の紙の台本：旗 0 の盤面（基線の計器つき 10 種の生）で、機関車の発見より前の機関車の払いが正か（G1 が旗 0 で落ちるか）を
# 既にある表だけで見積もる。nationgood_s の機関車の行の (demandQty−shortageQty)×priceMeanGold を国×年の〈買えた額〉とし、
# disc_s の E 行（kind=E・row=機関車）の最初の年より前を〈発見の前〉に数える（E 行の無い国は別の列＝親の写しで E 行が書かれない場合を含む）。
# 線路も同じ物差しで並べる（G3 の旗 0 の値の目安）。読み手はエンジンを呼ばない。年の束の物差し（旗 0 の年末の表）＝近似と名乗る。
import sys, os, collections
def run(d, seeds):
    out = []
    for s in seeds:
        disc = {}
        p = os.path.join(d, f"disc_s{s}.tsv")
        with open(p, encoding="utf-8") as f:
            next(f)
            for ln in f:
                c = ln.rstrip("\n").split("\t")
                if c[0] == "E" and c[4] == "機関車":
                    y = int(c[2]); disc[c[3]] = min(disc.get(c[3], 9999), y)
        acc = collections.defaultdict(float)
        prodPre = 0
        with open(os.path.join(d, f"nationgood_s{s}.tsv"), encoding="utf-8") as f:
            h = next(f).rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for ln in f:
                c = ln.rstrip("\n").split("\t")
                g = c[ix["good"]]
                if g not in ("機関車", "線路"): continue
                y = int(c[ix["year"]]); n = c[ix["nation"]]
                try:
                    got = float(c[ix["demandQty"]]) - float(c[ix["shortageQty"]]); px = float(c[ix["priceMeanGold"]])
                except ValueError: continue
                v = got * px if got > 0 and px == px else 0.0
                k = "pre" if n in disc and y < disc[n] else ("noE" if n not in disc else "post")
                acc[(g, k)] += v
                if g == "機関車" and k == "pre" and float(c[ix["producedQty"]]) > 0: prodPre += 1
        def sh(g, k):
            t = acc[(g, "pre")] + acc[(g, "noE")] + acc[(g, "post")]
            return acc[(g, k)] / t if t > 0 else float("nan")
        out.append((s, len(disc), acc[("機関車", "pre")], sh("機関車", "pre"), sh("機関車", "noE"), acc[("線路", "pre")], sh("線路", "pre"), sh("線路", "noE"), prodPre))
    print("seed\tnDiscE\tlocoPreGold\tlocoPreShare\tlocoNoEShare\trailPreGold\trailPreShare\trailNoEShare\tlocoProducedPreRows")
    for r in out: print("\t".join(str(x) if isinstance(x, int) else f"{x:.6g}" for x in r))
if __name__ == "__main__":
    d = sys.argv[1]; seeds = [int(x) for x in sys.argv[2].split(",")] if len(sys.argv) > 2 else list(range(1, 11))
    run(d, seeds)
