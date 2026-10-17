"""(262) 貯蓄の書き手の台帳の経路名（呼び出しの位置→名前）。位置は (262) の木＝r261 の木に計器を足した物の `World.Transfer`／`Pop.Blend` の呼び出しの行。
ソースが動いたら位置もずれる（読み手は ファイル:行 を併記する）。名前はその行を読んで付けた（相手の側は t2 の列）。
★(264) 反証役 rv 中#1：旧の (InternationalMarket.cs, 124／125)〈清算の代金の配分〉は誤った名前だった＝PaySales の中の Transfer で、呼び手8か所（と Procure の呼び手3か所）が1つの名前に潰れていた。
  (264) から PaySales と Procure も [CallerFilePath]／[CallerLineNumber] を Transfer へ渡す＝経路は呼び手の位置で出る（下の10行）。⚠(262) の表（measurements/flow262_20261011）は旧の名前のまま＝(264) の表（measurements/m264_20261011/flow_r264）を読む。
  国営持分の取り分（PaySales の中の1本目の Transfer）は受け手が国庫＝貯蓄の書き手の台帳には払い手の take として呼び手の位置で載る（相手の側 T）。
  ★(272) 合流の担当：(271) で Credit.cs に1行・Politics.cs から削除が入り、Fiscal.cs／Simulation.cs／World.cs の鍵は 66b25c6 の時点でずれていた＝(272) の木（main）で鍵を取り直した（16 本・(264) の木の行の本文を今の木で一意に探した）。
  ⚠鍵は今の木の行＝古い木の flow_s（r264・r268 ほか）を読むときは、その回のコミットの route_names.py を git show で取り出して使う。番人＝InstrumentsTests の「経路名の鍵の行は移し方の呼び出しを指す」。
"""
# ★2026-10-17（開発の高速化・挙動は変えない）：鍵の表は route_anchors.tsv（ファイル・呼び出しの行のコードの文字列・名前・注）へ移した。
#   行番号は、読むときに今の木（または POB_SRC_ROOT が指す木）のソースから引く＝ソースの行がずれても鍵の表を取り直さなくてよい。
#   ⚠古い木の flow_s（行番号はその木のソースの行）を読むときは、その回のコミットのソースを取り出して POB_SRC_ROOT に指す。
#   呼び出しの行のコードそのものを書き換えたときだけ、表の anchor を直す（番人＝InstrumentsTests.経路名の鍵の行は移し方の呼び出しを指す）。
import os as _os

_HERE = _os.path.dirname(_os.path.abspath(__file__))
ANCHORS_TSV = _os.path.join(_HERE, 'route_anchors.tsv')


def _code(x):
    k = x.find('//')
    return (x[:k] if k >= 0 else x).strip()


def load_anchors(path=ANCHORS_TSV):
    rows = []
    for line in open(path, encoding='utf-8'):
        line = line.rstrip('\r\n')
        if not line or line.startswith('#'):
            continue
        f, a, name, note = (line.split('\t') + ['', '', '', ''])[:4]
        rows.append((f, a, name, note))
    return rows


def resolve(src_root=None, path=ANCHORS_TSV):
    """鍵の表を (ファイル, 今の木の行) → 名前 へ引く。anchor が見つからない・2本以上あるときは止める（黙って落ちない）。"""
    src_root = src_root or _os.environ.get('POB_SRC_ROOT') or _os.path.normpath(_os.path.join(_HERE, '..', '..', '..'))
    out, bad, cache = {}, [], {}
    for f, a, name, _note in load_anchors(path):
        if not f:
            out[('', 0)] = name
            continue
        if f not in cache:
            cache[f] = [_code(x) for x in open(_os.path.join(src_root, 'PriceOfBread.SimCore', f), encoding='utf-8-sig').read().split('\n')]
        hits = [i + 1 for i, c in enumerate(cache[f]) if c == a]
        if len(hits) != 1:
            bad.append(f'{f}: {len(hits)} 本「{a[:80]}」')
            continue
        out[(f, hits[0])] = name
    if bad:
        raise RuntimeError('経路名の鍵がソースに1本だけ見つからない（route_anchors.tsv の anchor を直す）：' + ' ／ '.join(bad))
    return out


ROUTE_NAMES = resolve()

