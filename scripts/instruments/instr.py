"""標準の計器（SimCore/Instruments.cs・環境変数 POB_INSTRUMENTS=<ディレクトリ>）の表の読み口＝史実比較の桁（NG）の読み手 11 本が共有する。
2026-10-10 (245)：旧の NG の読み手の入力（木へ注入した inj_*.py の A／C／Q／T／G／M／Y／WEV 行）を標準の計器へ畳んだ。
表（1試合＝seed ごとに1本・タブ区切り・1行目が列名・数は G10・nan／inf はそのまま float で読める）：
  nation_s<seed>.tsv   国×四半期（全ての国・死んだ国も）＝quarter（0始まり）・year・nationIndex・nation・人口・兵士・名目GDP・交戦と内戦の月・徴兵法・台帳の累計 ほか
                       ★(260) 末尾の6列＝国営の棟の着工と完成（stateFacilityStartedCount・stateFacilityStartedGateClosedCount〔うち着工の時点で発見の門が閉じていた〕・stateFacilityCompletedCount・stateFacilityCompletedGateClosedCount〔うち完成の月に門が閉じていた〕・stateFacilityCompletedGateClosedAtStartCount〔その完成のうち着工の時点でも閉じていた〕＝四半期の件数）と supplyFillShare（人口加重の供給の充足＝補給崩壊の物差し・0.3 を割る国×四半期のうち人口100千人以上を数える）
  stock_s<seed>.tsv    在庫・即応度・装備・年末の行（time が q<番号> で番号%4==3）に艦隊のゲージ・技術水準・鉄道の容量・工場の蒸気動力の割合・交戦中か
  wars_s<seed>.tsv     戦争1本ごと（開戦の年・内戦か・当事者・月数・戦死）
  casualty_s<seed>.tsv (年, 国)ごとの兵士の月和と戦死
使い方：from instr import rows ／ for sd, r in rows(D, 'nation'): …（r は列名→文字列の辞書・数は num(r['…'])）。"""
import glob
import os
import re


def num(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def seeds(D, kind):
    """D の下の <kind>_s<seed>.tsv の seed を昇順で。"""
    out = []
    for f in glob.glob(os.path.join(D, f'{kind}_s*.tsv')):
        m = re.match(rf'{kind}_s(\d+)\.tsv$', os.path.basename(f))
        if m:
            out.append(int(m.group(1)))
    return sorted(out)


def rows(D, kind, maxsd=None):
    """(seed, 行の辞書) を seed の昇順・ファイルの行の順で。"""
    for sd in seeds(D, kind):
        if maxsd is not None and sd > maxsd:
            continue
        with open(os.path.join(D, f'{kind}_s{sd}.tsv'), encoding='utf-8') as fh:
            head = fh.readline().rstrip('\n').split('\t')
            for ln in fh:
                yield sd, dict(zip(head, ln.rstrip('\n').split('\t')))


def year_end(r):
    """stock 表の年末の行（time が q<番号> で 番号%4==3）か。開幕・助走の断面と、挙げた国の途中の四半期の行を除く。"""
    t = r['time']
    return t.startswith('q') and int(t[1:]) % 4 == 3


_GOODS = None


def goods():
    """財の表 {名前: 番号}（SimCore/goods.json＝財の番号の唯一の実体・BOM つき UTF-8）。
    ★財は番号でなく名前で引く（作者 2026-10-10・00一覧 00-R2）＝財の削減で番号がずれても追いつく。goods.json が BuiltinGoods と一致することは
    BreakDetectionTests.埋め込みgoodsJsonはBuiltinGoodsと内容一致する が守る。道具に財の番号を数字で書かない（GoodIdGuardTests.道具のPythonは財の番号を数字で書かない が見張る）。"""
    global _GOODS
    if _GOODS is None:
        import json
        p = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'PriceOfBread.SimCore', 'goods.json')
        with open(p, encoding='utf-8-sig') as fh:
            _GOODS = {g['name']: g['id'] for g in json.load(fh)}
    return _GOODS


def good_id(name):
    """財の名前（例：'小麦粉'）から番号を引く。名前が無ければ KeyError（黙って別の財を読まない）。"""
    return goods()[name]
