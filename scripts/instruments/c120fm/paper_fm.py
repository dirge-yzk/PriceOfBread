# (1201) c120fm の紙の台本：r1196/q1_form.py の写しに MODE を足した（old＝今の規則・a＝民族主義の取り分の門・b＝直接の州だけ）。
#  a の取り分は ideology_s の ideoNationalNationalismShare（その四半期の行＝Formations.Check の月の値ではない＝近似）。閾値は引数（UnificationNatioShare＝0.25）。
#  使い方: python -X utf8 -I paper_fm.py <ins（D:/…）> <種の起点> <種の終わり> <world.json> <出力の頭> <MODE old|a|b> <閾値>
# 元の注：(1196) c119ra 問い1：統一国家の成立の経路を、計器の表だけから組み直す（エンジンの関数を呼ばない・読むだけ）。
#  なぜ組み直すか：計器の表の国の名は NameOf＝BaseName（Instruments.cs:285）。成立は Diplomacy.RenameNation（Diplomacy.cs:471）が
#   Name だけを書き換える＝BaseName は変わらない＝成立そのものはどの表にも出ない。表に出るのは、成立した国の内戦の反乱側の名
#   （SecessionNames.CivilWarName が親の Name から作る＝〈立憲王政ドイツ〉＝親が〈ドイツ帝国〉の後）だけ。
#  規則（Formations.cs:274 Check・:302 Qualifies・CultureRealms.cs:51 Build・:67 HasGained）を表の上で数える：
#   持つ＝自分の州＋直接の従属国の州（Formations.Owns）。四半期ごとの全ての国の州の行は rule_s の年の標本（全ての国が出る四半期）だけ＝年の分解能。
#   従属のリンクは dep_s の張った行（kind 1）を積む・宗主か従属国が表から消えたら外す・離反（Diplomacy.cs:1278）は表に出ない＝近似。
#   国の文化＝州の文化の札（world.json の provinces）の人口の重みの多数（Diplomacy.Power.Culture の定義に近い・植民地の札は数えない＝近似）。
#  使い方: python -X utf8 -I q1_form.py <ins（D:/…）> <種の起点> <種の終わり> <world.json> <出力の頭>
import sys, csv, json, os, collections
sys.stdout.reconfigure(newline='\n')
INS, SB, SE, WJ, OUT = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
MODE = sys.argv[6]; THR = float(sys.argv[7])
assert MODE in ('old', 'a', 'b')
wd = json.load(open(WJ, encoding='utf-8'))
CUL = {}; INIT = collections.Counter(); INITOWN = {}
for n in wd['nations']:
    for p in n['provinces']:
        CUL[p['city']] = p.get('culture') or ''
        INITOWN[p['city']] = n['name']
        if p.get('culture'): INIT[n['name'] + p['culture']] += 1
BYC = collections.defaultdict(list)
for city, c in CUL.items():
    if c: BYC[c].append(city)
G = ["ベルリン", "シュテッティン", "ケーニヒスベルク", "ブレスラウ", "ケルン", "ドレスデン", "ハノーファー", "ミュンヘン", "シュトゥットガルト"]
# (名, 段, 資格の BaseNames, 資格の文化, 除く BaseNames, 全て要る州, 何個以上の集合, 最小, 文化の起点)
RULES = [
    # ローマ（段 4）：宗教の資格（ReligionAnyOf＝カトリック・プロテスタント・正教・イスラム・シーア派）は表に無い＝資格ありと置く（近似）
    ("ローマ", 4, None, None, None, ["ローマ", "イスタンブール", "カイロ"],
     ["ナポリ", "パレルモ", "カリャリ", "ミラノ", "ヴェネツィア", "パドヴァ", "フィレンツェ", "ボローニャ", "ジェノヴァ", "トリノ", "ノヴァーラ", "マルセイユ", "リヨン", "ボルドー",
      "マドリード", "バルセロナ", "セビーリャ", "バレンシア", "リスボン", "ポルト", "アテネ", "サロニカ", "スミルナ", "アンカラ", "エルズルム", "ダマスカス", "エルサレム",
      "ベオグラード", "ソフィア", "ブカレスト"], 8, None),
    ("神聖ローマ", 3, ["オーストリア", "プロイセン"], "ドイツ", None,
     ["ウィーン", "プラハ", "ベルリン", "ミュンヘン", "ケルン", "ドレスデン", "ハノーファー", "シュトゥットガルト", "ミラノ"], None, 0, None),
    ("ドイツ", 2, ["プロイセン", "バイエルン", "ザクセン", "ハノーファー", "ヴュルテンベルク", "ライン諸邦"], "ドイツ", ["オーストリア"], G, None, 0, None),
    ("イタリア", 2, ["サルデーニャ＝ピエモンテ", "両シチリア", "教皇領", "トスカーナ", "ヴェネツィア", "ジェノヴァ"], "イタリア", ["オーストリア"],
     ["トリノ", "ジェノヴァ", "ミラノ", "ヴェネツィア", "フィレンツェ", "ボローニャ", "ローマ", "ナポリ"], None, 0, None),
    ("北ドイツ連邦", 1, ["プロイセン"], None, None, ["ベルリン", "シュテッティン", "ケーニヒスベルク", "ブレスラウ", "ケルン", "ドレスデン", "ハノーファー"], None, 0, None),
]
for cul, nm, great in (("イタリア", "イタリア王国", "ローマ帝国"), ("ドイツ", "ドイツ帝国", "神聖ローマ帝国")):
    cities = BYC[cul]; n = len(cities)
    RULES.append((great, 3, None, cul, None, list(cities), None, 0, cul))
    RULES.append((nm, 2, None, cul, None, [], list(cities), n // 2 + 1, cul))
STEMS = ["北ドイツ連邦", "神聖ローマ", "ドイツ", "イタリア", "ローマ"]


def rd(path):
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for c in r:
            if len(c) >= len(h): yield ix, c


def seed(s):
    rp = os.path.join(INS, f'rule_s{s}.tsv')
    if not os.path.exists(rp): return None
    # 年の標本（全ての国が出る四半期）
    byq = collections.defaultdict(list); qn = collections.defaultdict(set)
    for ix, c in rd(rp):
        q = int(c[ix['quarter']]); byq[q].append((c[ix['nation']], c[ix['region']], float(c[ix['popK']] or 0), int(c[ix['year']])))
        qn[q].add(c[ix['nation']])
    full = sorted(q for q in byq if len(qn[q]) > 20)
    deps = []
    dpp = os.path.join(INS, f'dep_s{s}.tsv')
    depMissing = not os.path.exists(dpp)
    for ix, c in (rd(dpp) if not depMissing else []):
        if c[ix['kindCode']] != '1': continue
        deps.append((int(c[ix['month']]), c[ix['suzerain']], c[ix['dependent']], c[ix['callerMember']], int(c[ix['year']])))
    deps.sort(key=lambda x: x[0])
    cess = collections.defaultdict(list)
    cp = os.path.join(INS, f'cess_s{s}.tsv')
    if os.path.exists(cp):
        for ix, c in rd(cp): cess[(c[ix['winner']], c[ix['region']])].append(int(c[ix['year']]))
    wars = list(rd(os.path.join(INS, f'wars_s{s}.tsv')))
    decl = [(int(c[ix['startYear']]), c[ix['sideA']], c[ix['sideB']]) for ix, c in wars if c[ix['entryCode']] == 'declare']
    ev = []
    for ix, c in wars:
        if c[ix['civilWarFlag']] == '0': continue
        for side, other in ((c[ix['sideB']], c[ix['sideA']]), (c[ix['sideA']], c[ix['sideB']])):
            st = next((t for t in STEMS if t in side), None)
            if st and st not in other:
                ev.append(dict(y=int(c[ix['startYear']]), rebel=side, parent=other, stem=st, code=c[ix['entryCode']]))
    nshare = {}
    for ix, c in rd(os.path.join(INS, f'ideology_s{s}.tsv')):
        nshare[(int(c[ix['quarter']]), c[ix['nation']])] = float(c[ix['ideoNationalNationalismShare']])
    gateOut = []
    link = {}; di = 0; tier = collections.Counter(); taken = {}; forms = []
    prevown = {}; ownhist = collections.defaultdict(list)
    for q in full:
        rows = byq[q]; year = rows[0][3]
        while di < len(deps) and deps[di][0] <= q * 3 + 2:
            m, sz, dp, how, y = deps[di]; link[dp] = (sz, how, y); di += 1
        alive = qn[q]
        for dp in list(link):
            if dp not in alive or link[dp][0] not in alive: del link[dp]
        own = collections.defaultdict(set); cw = collections.defaultdict(collections.Counter)
        for nat, reg, pop, _ in rows:
            own[nat].add(reg)
            if CUL.get(reg): cw[nat][CUL[reg]] += pop
            if prevown.get(reg) != nat: ownhist[reg].append((year, nat))
            prevown[reg] = nat
        culture = {n: (cw[n].most_common(1)[0][0] if cw[n] else '') for n in own}
        dependents = collections.defaultdict(list)
        for dp, (sz, how, y) in link.items(): dependents[sz].append(dp)
        for nat in sorted(own):
            if nat in taken.values() and False: pass
            def owns(city):
                if city in own[nat]: return 'direct'
                if MODE == 'b': return None
                for dp in dependents.get(nat, []):
                    if city in own.get(dp, ()): return dp
                return None
            best = None
            for (nm, t, bases, cul, excl, req, anyset, amin, corig) in RULES:
                if t <= tier[nat]: continue
                if best is not None and t <= best[1]: continue
                if excl and nat in excl: continue
                elig = (bases is None and cul is None) or (bases and nat in bases) or (cul and culture[nat] == cul)
                if not elig: continue
                if corig is not None:
                    now = sum(1 for x in BYC[corig] if owns(x))
                    if not now > INIT.get(nat + corig, 0): continue
                if any(owns(x) is None for x in req): continue
                if anyset is not None and sum(1 for x in anyset if owns(x)) < amin: continue
                if nm in taken and taken[nm] != nat and taken[nm] in alive: continue
                best = (nm, t, bases, cul, excl, req, anyset, amin, corig)
            if best is None: continue
            sh = nshare.get((q, nat))
            if MODE == 'a' and not (sh is not None and sh >= THR):
                gateOut.append(dict(year=year, q=q, nation=nat, rule=best[0], share=sh)); continue
            nm, t = best[0], best[1]
            tier[nat] = t; taken[nm] = nat
            cities = list(best[5]) + [x for x in (best[6] or []) if owns(x)]
            det = []
            cnt = collections.Counter()
            for x in cities:
                o = owns(x)
                if o is None: continue
                if o == 'direct':
                    if INITOWN.get(x) == nat: kind = '開幕から'; cnt['開幕から'] += 1
                    elif any(y <= year for y in cess.get((nat, x), [])): kind = '講和の割譲'; cnt['講和の割譲'] += 1
                    else: kind = 'その他の移転'; cnt['その他の移転'] += 1
                    det.append(f'{x}:{kind}')
                else:
                    sz, how, y = link[o]; cnt['従属国:' + how] += 1
                    det.append(f'{x}:従属国 {o}（{how} {y}）')
            dbefore = [d for d in decl if d[1] == nat and d[0] <= year]
            route = '統一の宣戦あり' if dbefore else ('従属国の州を数えた' if any(k.startswith('従属国') for k in cnt) else ('講和の割譲を含む' if cnt['講和の割譲'] else '自分の州だけ'))
            forms.append(dict(share=sh, year=year, q=q, nation=nat, culture=culture[nat], rule=nm, tier=t, route=route, counts=dict(cnt), detail=det,
                              declareBefore=len(dbefore)))
    return dict(forms=forms, evidence=ev, declare=[f'{y} {a}->{b}' for y, a, b in decl], fullSamples=len(full), depMissing=depMissing, gateOut=gateOut)


res = {}
for s in range(SB, SE + 1):
    r = seed(s)
    res[str(s)] = r
with open(OUT + '.json', 'w', encoding='utf-8', newline='\n') as f:
    json.dump(res, f, ensure_ascii=False, indent=1)
tot = collections.Counter(); tseed = collections.Counter()
with open(OUT + '.tsv', 'w', encoding='utf-8', newline='\n') as f:
    f.write('seed\tyear\tnation\tculture\trule\ttier\troute\tdeclareBefore\tnatShare\tcounts\tevidenceCivilWars\n')
    for s, r in res.items():
        if r is None:
            f.write(f'{s}\t欠け\n'); continue
        for x in r['forms']:
            evs = [e for e in r['evidence'] if e['parent'] == x['nation'] and e['y'] >= x['year']]
            f.write('\t'.join([s, str(x['year']), x['nation'], x['culture'], x['rule'], str(x['tier']), x['route'], str(x['declareBefore']), '' if x['share'] is None else f"{x['share']:.4f}",
                               ' '.join(f'{k}={v}' for k, v in sorted(x['counts'].items())),
                               '; '.join(f"{e['y']} {e['rebel']}" for e in evs[:4])]) + '\n')
            tot[x['route']] += 1; tot['rule:' + x['rule']] += 1
for s, r in res.items():
    if r is None: print(f's{s} 欠け'); continue
    print(f"s{s} 年の標本 {r['fullSamples']}{'（dep_s 無し＝従属国を数えない下限）' if r['depMissing'] else ''} 統一の宣戦 {len(r['declare'])} " + ' | '.join(f"{x['year']} {x['nation']}→{x['rule']}（{x['route']}）" for x in r['forms']))
    ev = r['evidence']
    if ev: print('   内戦の名の証拠: ' + '; '.join(f"{e['y']} {e['rebel']}（親 {e['parent']}・{e['code']}）" for e in ev[:6]))
    # 証拠と組み直しの突き合わせ：証拠の親が、証拠の年までに成立を組み直した国か
    for e in ev:
        ok = any(x['nation'] == e['parent'] and x['year'] <= e['y'] + 1 for x in r['forms'])
        if not ok: print(f"   ⚠ 証拠と食い違い: {e['y']} {e['rebel']} の親 {e['parent']} の成立を組み直せていない")
print('合計', dict(tot))
for s, r in res.items():
    if r and r['gateOut']:
        g = r['gateOut']; mx = max((x['share'] or 0) for x in g)
        print(f"s{s} 門の外で止めた四半期 {len(g)}（最初 {g[0]['year']} {g[0]['nation']}→{g[0]['rule']} 取り分 {g[0]['share']}・止めた中の取り分の最大 {mx:.4f}）")
