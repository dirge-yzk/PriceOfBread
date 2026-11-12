# (1082) 門の台本：計器つきの腕の secession_s（州ごとの離脱の圧力の表）の最初の四半期の行を読む（エンジンを呼ばない・州は台本が自分で選ぶ）。
#   使い方: python -X utf8 gate1082.py <ins フォルダ> [種の起点=1] [種の数=10]
#   G-W1（働いた・両側）：計器の国の 19 州（下の T19）のうち、最初の四半期の coreShareMeanShare が 1 未満の州の集合が、全ての種で EXPECT とちょうど同じ。
#       下側＝EXPECT の 3 州が全て入る／上側＝EXPECT の外の州が 0（文化の札の誤り〔多数と違う名を置いた〕に固有の量）。
#   G-W2（働いた・大きさ・両側）：EXPECT の 3 州の最初の四半期の ethnicTermIndex が全ての種で正、かつ同じ種のカシュガル（今あるウイグルの州）の値の 2 倍以下。
#   対照：全ての種で T19 の 19 州とカシュガルの行が最初の四半期にある（行が無ければ門は赤）。
#   記録（門ではない）：清の州の離脱の発火（goFlag）の件数・region_s から新疆と甘粛と黒竜江の州の持ち主（1850・1900・1950 年の最初の行）。
import sys, os, csv, collections
INS = sys.argv[1]; SB = int(sys.argv[2]) if len(sys.argv) > 2 else 1; N = int(sys.argv[3]) if len(sys.argv) > 3 else 10
T19 = {("日本", "蝦夷地"), ("アメリカ", "西部"), ("アメリカ", "セントポール"),
       ("ロシア", "ウラル"), ("ロシア", "シベリア"), ("ロシア", "ウラジオストク"), ("ロシア", "ヤクーツク"), ("ロシア", "オホーツク"),
       ("ロシア", "ペトロパヴロフスク"), ("ロシア", "スレドネコリムスク"), ("ロシア", "アナディリ"), ("ロシア", "トゥルハンスク"),
       ("ロシア", "オブドルスク"), ("ロシア", "バルナウル"), ("ロシア", "グリエフ"),
       ("清", "アイグン"), ("清", "ハミ"), ("清", "ホータン"), ("清", "蘭州")}
EXPECT = {("清", "アイグン"), ("清", "ハミ"), ("清", "ホータン")}
REF = ("清", "カシュガル")
WATCH = ["カシュガル", "ハミ", "ホータン", "アイグン", "蘭州"]
csv.field_size_limit(1 << 30)
ok1 = ok2 = okc = True
fires = collections.Counter(); owners = collections.defaultdict(collections.Counter)
for s in range(SB, SB + N):
    p = os.path.join(INS, f"secession_s{s}.tsv")
    if not os.path.exists(p): print(f"種 {s}：secession_s が無い＝赤"); ok1 = ok2 = okc = False; continue
    q0 = None; rows = {}
    with open(p, encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for r in rd:
            q = int(r[ix['quarter']])
            if q0 is None: q0 = q
            key = (r[ix['nation']], r[ix['region']])
            if q == q0 and (key in T19 or key == REF): rows[key] = (float(r[ix['coreShareMeanShare']]), float(r[ix['ethnicTermIndex']]))
            if r[ix['nation']] == "清" and r[ix['goFlag']] == "1": fires[s] += 1
    miss = sorted(k[1] for k in (T19 | {REF}) if k not in rows)
    if miss: okc = False
    below = {k for k in T19 if k in rows and rows[k][0] < 1 - 1e-9}
    g1 = below == EXPECT
    ref = rows.get(REF, (float('nan'), float('nan')))[1]
    e = {k[1]: rows[k][1] for k in EXPECT if k in rows}
    g2 = len(e) == 3 and all(v > 0 and v <= 2 * ref for v in e.values())
    ok1 &= g1; ok2 &= g2
    print(f"種 {s}：q0={q0}・行 {len(rows)}/20・欠け {miss or 'なし'}・中核<1 {len(below)} 州 {sorted(k[1] for k in below)}・G-W1 {'緑' if g1 else '赤'}・"
          f"ethnicTerm " + "・".join(f"{k} {v:.4g}" for k, v in sorted(e.items())) + f"・カシュガル {ref:.4g}・G-W2 {'緑' if g2 else '赤'}")
    pr = os.path.join(INS, f"region_s{s}.tsv")
    if os.path.exists(pr):
        seen = set()
        with open(pr, encoding='utf-8') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
            for r in rd:
                y = int(r[ix['year']]); reg = r[ix['region']]
                if y in (1850, 1900, 1950) and reg in WATCH and (y, reg) not in seen:
                    seen.add((y, reg)); owners[(y, reg)][r[ix['nation']]] += 1
print(f"対照（行がそろう）{'緑' if okc else '赤'}・G-W1 {'緑' if ok1 else '赤'}・G-W2 {'緑' if ok2 else '赤'}")
print("記録：清の州の離脱の発火（goFlag=1 の行）種ごと " + str([fires[s] for s in range(SB, SB + N)]))
for (y, reg) in sorted(owners):
    print(f"記録：{y} 年 {reg} の持ち主（計器の国の行のある種）{dict(owners[(y, reg)])}")
