# (776) c7hl：worldmap.svg の path4682（フィンランドの本土の多角形）の南岸の頂点だけを、頂点ごとに置いた緯度へ動かす。
#   使い方：python fixsvg776.py <元の svg> <出力の svg> [fix|north|whole]
#     fix   ＝表 TABLE のとおり（採る形）
#     north ＝突然変異：表の頂点を、表の動きと同じ大きさだけ北へ（元の緯度から北へ動かす＝S1 の下限の門で落ちるはずの誤り）
#     whole ＝突然変異：path4682 の全ての頂点を、表の動きの平均と同じ大きさだけ南へ（局所でなく多角形の全体＝S2 の画素の上限の門で落ちるはずの誤り）
#   バイトで読み書き（BOM と改行を保つ）。他の頂点の文字列は触らない。
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import path4682 as P
# k：頂点番号（端点は k%3==0・他は制御点）→ 新しい緯度。経度は動かさない。海岸の緯度は AI の概算（地名の海岸の緯度の記憶）＝確証なし。
#   24.737E→60.12・24.939E→60.13＝ヘルシンキ沖の群島〔スオメンリンナ・ラウトサーリ〕を含めた 0.1° 格子の海岸（首都の格子を陸にする）
#   25.419E→60.30・26.826E→60.46・27.165E→60.55＝読み手（実装ログ (765)）の推奨の海岸の緯度
TABLE = {
    141: 60.12,   # 24.737E  (60.287)
    140: 60.13,   # 24.939E  (60.319)
    139: 60.17,   # 25.278E  (60.319)
    138: 60.30,   # 25.419E  (60.474)
    121: 60.45,   # 26.333E  (60.536)
    120: 60.45,   # 26.397E  (60.540)
    119: 60.46,   # 26.551E  (60.540)
    118: 60.46,   # 26.683E  (60.581)
    117: 60.46,   # 26.826E  (60.630)
    116: 60.52,   # 27.013E  (60.695)
    115: 60.50,   # 27.004E  (60.585)
    114: 60.55,   # 27.165E  (60.596)
}
def main():
    src, dst = sys.argv[1], sys.argv[2]; mode = sys.argv[3] if len(sys.argv) > 3 else 'fix'
    raw = open(src, 'rb').read(); s = raw.decode('utf-8')
    st, en, blk, d = P.read_block(s)
    V = P.vertices(d)
    move = {k: V[k] for k in TABLE}
    delta = {k: P.lat_of(V[k][1]) - TABLE[k] for k in TABLE}   # 正＝南へ動く量
    if mode == 'fix': nl = dict(TABLE)
    elif mode == 'north': nl = {k: P.lat_of(V[k][1]) + delta[k] for k in TABLE}
    elif mode == 'whole':
        mean = sum(delta.values()) / len(delta)
        nl = {k: P.lat_of(V[k][1]) - mean for k in range(len(V))}
    else: raise SystemExit('mode?')
    d2 = P.rewrite(d, nl)
    s2 = s[:st] + blk.replace(d, d2) + s[en:]
    open(dst, 'wb').write(s2.encode('utf-8'))
    print('mode', mode, 'vertices moved', len(nl), 'mean south move deg', round(sum(delta.values()) / len(delta), 4))
main()
