# (1025) c96rd 段0″a 資源のデータ：足す行の確定（紙・腕の前）
#   入力：hole.tsv（(1015) の D の行）・hist.tsv（史実の行）・world.json・places.json
#   規則（事前登録 §2 に固定）：
#     R0  hole.tsv の class=D・conf=中 の 15 行から始める
#     R1  外す 3 行：RGO の無い 2 行（日本の茶＝京都・オスマンのアヘン＝スミルナ）・L の露の石炭（ハリコフ）＝verifier_corrections ①②
#     R2  州の対応の確度の列：hist.tsv の note に〈対応の州は〈推〉〉か〈推〉の州の名がある行、または verifier ③ が名指した行を〈推〉・他は〈確〉
#     R3  〈推〉の行は、史実の主な産地（hist.tsv の histRegion の最初に書かれた産地・座標は AI の記憶＝下の SITES）に最も近い places.json の点
#         （state・colony・target の全 344 点＝地図の全ての州と植民地と抽象の標的）が、対応させた州そのものなら〈確かめた〉・違えば外す
#         （距離の閾値は置かない＝最も近い点の州＝その産地を含む州の代わり〔ボロノイ〕）
#     R4  〈確〉の行は R3 を記録だけで計算する（外さない）
#   出力：rows.tsv（15 行・列＝nation good province mapConf site siteLat siteLon nearest nearestKm mappedKm rule keep）
import sys, json, math, os

HOLE, WORLD, PLACES, OUT = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]

# 史実の主な産地の代表点（AI の記憶・確証なし・度）＝hist.tsv の histRegion の最初の産地
SITES = {
    ('イギリス', '銅鉱'): ('レッドルース（コーンウォール）', 50.23, -5.23),
    ('イギリス', '塩'): ('ノースウィッチ（チェシャー）', 53.26, -2.52),
    ('イギリス', '漁獲'): ('グリムズビー（北海）', 53.57, -0.08),
    ('プロイセン', '鉄鉱石'): ('タルノヴィッツ（上シュレージエン）', 50.44, 18.86),
    ('ロシア', '石炭'): ('リシチャンスク（ドンバス）', 48.92, 38.43),
    ('ロシア', '塩'): ('ソリカムスク（ペルミ）', 59.63, 56.77),
    ('アメリカ', '銅鉱'): ('キーウィーノー（カルメット）', 47.25, -88.45),
    ('アメリカ', '鉛鉱'): ('ガリーナ', 42.42, -90.43),
    ('清', '鉄鉱石'): ('仏山（広東）', 23.02, 113.12),
    ('オーストリア', '塩'): ('ハルシュタット（ザルツカンマーグート）', 47.56, 13.65),
    ('スペイン', '鉄鉱石'): ('ビルバオ（ビスカヤ）', 43.26, -2.93),
    ('スペイン', '銅鉱'): ('リオ・ティント（ウエルバ）', 37.70, -6.59),
    ('スペイン', '鉛鉱'): ('リナーレス', 38.10, -3.64),
    ('日本', '茶'): ('宇治', 34.88, 135.80),
    ('オスマン', 'アヘン'): ('アフィヨン', 38.76, 30.54),
}
DROP = {('日本', '茶'): 'R1 RGO が無い', ('オスマン', 'アヘン'): 'R1 RGO が無い・〈主な〉に当たらない（verifier ②）',
        ('ロシア', '石炭'): 'R1 L（主になったのは 1870 年代以後・verifier ①）'}
VERIFIER3 = {('ロシア', '石炭'), ('ロシア', '塩'), ('アメリカ', '銅鉱'), ('アメリカ', '鉛鉱'), ('清', '鉄鉱石'), ('オーストリア', '塩')}

def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        while h[0].startswith('#'): h = f.readline().rstrip('\n').split('\t')
        for l in f:
            if l.startswith('#') or not l.strip(): continue
            yield dict(zip(h, l.rstrip('\n').split('\t')))

def km(a, b, c, d):
    p1, p2 = math.radians(a), math.radians(c)
    dl = math.radians(d - b)
    x = math.sin((p2 - p1) / 2) ** 2 + math.cos(p1) * math.cos(p2) * math.sin(dl / 2) ** 2
    return 2 * 6371.0 * math.asin(math.sqrt(x))

hist_dir = os.path.dirname(HOLE)
notes = {}
for r in rows(os.path.join(hist_dir, 'hist.tsv')):
    notes.setdefault((r['nation'], r['good'], r['simProvince']), r.get('note', ''))
pl = json.load(open(PLACES, encoding='utf-8-sig'))

out = ['# (1025) c96rd 行の確定（rows.py）・規則は事前登録 §2・産地の座標は AI の記憶',
       'nation\tgood\tprovince\tmapConf\tsite\tsiteLat\tsiteLon\tnearest\tnearestKm\tmappedKm\trule\tkeep']
for r in rows(HOLE):
    if not (r.get('class') == 'D' and r.get('conf', '').startswith('中')): continue
    k = (r['nation'], r['good']); prov = r['simProvince']
    note = notes.get((r['nation'], r['good'], prov), '')
    conf = '推' if (k in VERIFIER3 or '〈推〉' in note) else '確'
    name, la, lo = SITES[k]
    best = min(pl, key=lambda p: km(la, lo, p['lat'], p['lon']))
    bkm = km(la, lo, best['lat'], best['lon'])
    mp = next((p for p in pl if p['name'] == prov), None)
    mkm = km(la, lo, mp['lat'], mp['lon']) if mp else float('nan')
    r3 = best['name'] == prov
    if k in DROP: rule, keep = DROP[k], 0
    elif conf == '推': rule, keep = ('R3 確かめた' if r3 else 'R3 外す（最も近い点が別の州）'), int(r3)
    else: rule, keep = ('R4 記録（一致）' if r3 else 'R4 記録（不一致・外さない）'), 1
    out.append('\t'.join([r['nation'], r['good'], prov, conf, name, '%.2f' % la, '%.2f' % lo, best['name'], '%.0f' % bkm, '%.0f' % mkm, rule, str(keep)]))
out.append('# keep=1 の行：%d' % sum(1 for l in out[2:] if l.endswith('\t1')))
open(OUT, 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('\n'.join(out))
