# (762) 波55 c5lg の紙の台本：事前登録 m762_20261102_prereg_lg.txt の §2 の静的な数を、写しの木で再現する。
# 使い方：python measurements/m762_20261102_paper_lg.py [--repo <旧の木>] [--base 191ca9c8] [--work D:/PoB_work/c5lg_paper] [--phase prep|build|run|all] [--keep]
#   旧＝<repo> の <base> のコミットから git archive で写した SimCore（何も足さない）。
#   新＝同じ写しに §0 の行（barriers.json）と規則（PathGraph.cs の ③④）を**旗なしで常時**当てた物。
#   探針＝SimCore を参照する小さなコンソール。ノード・辺・成分・壁・全対の陸路距離（places の全ての名前）と、開幕の内陸州の港の脚を TSV に吐く。
# 台本で再現した静的な数が事前登録の値と 1 つでも違ったら、腕を回さずに止めて報告する（事前登録の値を直さない）。違いは最後に NG として出す（終了コード 1）。
# 写しの木は終わったら消す（--keep を付けたときだけ残す）。D: の下で作業する（C: に作らない）。
import argparse, os, subprocess, sys, shutil, tarfile, io, json, math

ap = argparse.ArgumentParser()
ap.add_argument('--repo', default='C:/Users/dirge/Documents/PriceOfBread')
ap.add_argument('--base', default='191ca9c8')
ap.add_argument('--work', default='D:/PoB_work/c5lg_paper')
ap.add_argument('--phase', default='all')
ap.add_argument('--keep', action='store_true')
ap.add_argument('--out', default='')
a = ap.parse_args()
W = a.work.replace('\\', '/')


def rb(p):
    with open(p, 'rb') as f:
        return f.read()


def wb(p, b):
    os.makedirs(os.path.dirname(p), exist_ok=True)
    with open(p, 'wb') as f:
        f.write(b)


def rep1(b, old, new, label):
    old = old.encode('utf-8'); new = new.encode('utf-8')
    assert b.count(old) == 1, f'{label}: 置換の元が {b.count(old)} 回（1 回でなければ止める）'
    return b.replace(old, new)


# ---------------------------------------------------------------- 準備：旧と新の写し
def prep():
    if os.path.isdir(W): shutil.rmtree(W)
    data = subprocess.run(['git', '-C', a.repo, 'archive', a.base, 'PriceOfBread.SimCore'], check=True, capture_output=True).stdout
    for tree in ('old', 'new'):
        with tarfile.open(fileobj=io.BytesIO(data)) as t:
            t.extractall(f'{W}/{tree}')
    # --- 新：§0 の行（barriers.json）
    bj = f'{W}/new/PriceOfBread.SimCore/barriers.json'
    b = rb(bj)
    b = rep1(b, ' {\n  "name": "アルプス（ドロミテ）",\n  "lat": 46.4,\n  "lon": 11.9,\n  "kind": "barrier"\n },',
             ' {\n  "name": "アルプス（ドロミテ）",\n  "lat": 46.4,\n  "lon": 11.9,\n  "kind": "barrier",\n  "fix": -1\n },', 'barriers ①')
    tail = b'\n }\n]\n'
    assert b.endswith(tail), 'barriers.json の末尾の形が違う'
    add = [('アルプス（ドロミテ）', 46.6, 11.9, 'barrier'), ('ル・ペルテュス峠', 42.46, 2.86, 'pass'), ('ダリヤル峠', 42.95, 44.64, 'pass'),
           ('赤い塔峠', 45.57, 24.27, 'pass'), ('シプカ峠', 42.75, 25.32, 'pass')]
    rows = ''.join(' {\n  "name": "%s",\n  "lat": %s,\n  "lon": %s,\n  "kind": "%s",\n  "fix": 1\n },\n' % r for r in add)
    rows = rows[:-len(' },\n')] + ' }\n'
    nb = b[:len(b) - len(tail)] + b'\n },\n' + rows.encode('utf-8') + b']\n'
    wb(bj, nb)
    # --- 新：規則（PathGraph.cs）。旗なし＝常時
    pg = f'{W}/new/PriceOfBread.SimCore/PathGraph.cs'
    b = rb(pg)
    b = rep1(b, '    public static readonly Node[] Nodes = LoadNodes();\n',
             '    private static bool[] Added = [];\n    public static readonly Node[] Nodes = LoadNodes();\n', 'PathGraph Added')
    b = rep1(b, '        foreach (var r in rows) list.Add(new Node(r.Name, r.Kind, r.Lat, r.Lon));\n        return [.. list];\n',
             '        var addedIdx = new List<int>();\n'
             '        foreach (var r in rows)\n'
             '        {\n'
             '            if (r.Fix < 0) continue;\n'
             '            if (r.Fix > 0 && r.Kind != "barrier") addedIdx.Add(list.Count);\n'
             '            list.Add(new Node(r.Name, r.Kind, r.Lat, r.Lon));\n'
             '        }\n'
             '        Added = new bool[list.Count];\n'
             '        foreach (var k in addedIdx) Added[k] = true;\n'
             '        return [.. list];\n', 'PathGraph LoadNodes')
    b = rep1(b, '        public string Kind { get; set; } = "barrier";\n    }\n',
             '        public string Kind { get; set; } = "barrier";\n        public int Fix { get; set; }\n    }\n', 'PathGraph BarrierRow')
    b = rep1(b, '                    if (Nodes[j2].Kind == "barrier") continue;   // バリアは隣人として数えない（辺を張らないので）\n',
             '                    if (Nodes[j2].Kind == "barrier") continue;   // バリアは隣人として数えない（辺を張らないので）\n'
             '                    if (Added[j2] && !Added[i]) continue;\n', 'PathGraph radius')
    b = rep1(b, '                taken++;\n                accepted[i].Add(j);\n',
             '                if (!(Added[j] && !Added[i])) taken++;\n                accepted[i].Add(j);\n', 'PathGraph taken')
    wb(pg, b)
    # --- 探針
    for tree in ('old', 'new'):
        pd = f'{W}/{tree}/probe'
        wb(f'{pd}/probe.csproj', ('<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net10.0</TargetFramework>\n'
             '    <ImplicitUsings>enable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n  </PropertyGroup>\n  <ItemGroup>\n'
             '    <ProjectReference Include="../PriceOfBread.SimCore/PriceOfBread.SimCore.csproj" />\n  </ItemGroup>\n</Project>\n').encode('utf-8'))
        wb(f'{pd}/Program.cs', PROBE.encode('utf-8'))
    print('prep done')


PROBE = r'''
using System.Globalization;
using PriceOfBread.SimCore;
var inv = CultureInfo.InvariantCulture;
string F(double v) => v.ToString("R", inv);
var o = new List<string>();
var nodes = PathGraph.Nodes; var edges = PathGraph.Edges;
var (comps, largest) = PathGraph.Connectivity();
var wf = typeof(PathGraph).GetField("Walls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
int walls = wf?.GetValue(null) is Array wa ? wa.Length : -1;
o.Add($"H\t{nodes.Length}\t{edges.Length}\t{comps}\t{largest}\t{walls}");
foreach (var e in edges) o.Add($"E\t{nodes[e.A].Name}\t{nodes[e.B].Name}\t{F(e.Km)}");
var names = Places.All.Where(p => p.Lat != 0 || p.Lon != 0).Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).ToList();
o.Add($"N\t{names.Count}");
for (int i = 0; i < names.Count; i++)
    for (int j = i + 1; j < names.Count; j++)
    {
        var ca = Places.Coord(names[i]); var cb = Places.Coord(names[j]);
        o.Add($"P\t{names[i]}\t{names[j]}\t{F(Geo.DistanceKm(ca.Lat, ca.Lon, cb.Lat, cb.Lon))}\t{F(PathGraph.LandDistanceKm(ca.Lat, ca.Lon, cb.Lat, cb.Lon))}");
    }
var sim = new MultiNationSim(WorldData.Nations(), seed: 1);
// 開幕＝構築直後に昇格した植民地の最寄り港を毎四半期の張り直し（RewirePorts）と同じ規則で張る（構築直後は昇格した州の最寄り港が空）
typeof(MultiNationSim).GetMethod("RewirePorts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(sim, null);
foreach (var n in sim.Nations)
    foreach (var r in n.World.Regions)
        if (!r.IsPort && r.NearestPortName.Length > 0)
        {
            var cc = Climate.CityCoord(r.Name); var cp = Climate.CityCoord(r.NearestPortName);
            o.Add($"L\t{Instruments.NameOf(n.World)}\t{r.Name}\t{r.NearestPortName}\t{F(Geo.DistanceKm(cc.Lat, cc.Lon, cp.Lat, cp.Lon))}\t{F(r.PortLandKm)}");
        }
File.WriteAllLines(args[0], o, new System.Text.UTF8Encoding(false));
'''


def sh(cmd, cwd=None):
    r = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, encoding='utf-8', errors='replace')
    if r.returncode != 0:
        print(r.stdout[-3000:]); print(r.stderr[-3000:]); raise SystemExit(f'失敗: {cmd}')
    return r.stdout


def build():
    for tree in ('old', 'new'):
        sh(['dotnet', 'build', f'{W}/{tree}/probe/probe.csproj', '-c', 'Release', '--disable-build-servers', '-nologo', '-v', 'q'])
    print('build done')


def run():
    for tree in ('old', 'new'):
        sh(['dotnet', f'{W}/{tree}/probe/bin/Release/net10.0/probe.dll', f'{W}/{tree}.tsv'])
    print('run done')


# ---------------------------------------------------------------- 突き合わせ
def load(p):
    h = None; E = {}; P = {}; L = {}
    for ln in open(p, encoding='utf-8'):
        f = ln.rstrip('\n').split('\t')
        if f[0] == 'H': h = tuple(int(x) for x in f[1:])
        elif f[0] == 'E':
            k = tuple(sorted((f[1], f[2]))); assert k not in E, f'辺の名前が重なる {k}'; E[k] = float(f[3])
        elif f[0] == 'P': P[(f[1], f[2])] = (float(f[3]), float(f[4]))
        elif f[0] == 'L': L[(f[1], f[2])] = (f[3], float(f[4]), float(f[5]))
    return h, E, P, L


def analyze():
    ho, Eo, Po, Lo = load(f'{W}/old.tsv'); hn, En, Pn, Ln = load(f'{W}/new.tsv')
    out = []
    def say(s=''):
        print(s); out.append(s)
    bad = []
    def chk(label, got, want):
        ok = (got == want)
        say(f'{"OK " if ok else "NG "} {label}: 台本={got} 事前登録={want}')
        if not ok: bad.append(label)
    say('# 台本の出力（旧 vs 新）')
    chk('ノード', (ho[0], hn[0]), (419, 423)); chk('辺', (ho[1], hn[1]), (1070, 1084)); chk('成分', (ho[2], hn[2]), (54, 54))
    chk('最大成分', (ho[3], hn[3]), (242, 246)); chk('壁の線分', (ho[4], hn[4]), (62, 62))
    gone = sorted(set(Eo) - set(En)); new = sorted(set(En) - set(Eo))
    chk('消える辺の数', len(gone), 4); chk('増える辺の数', len(new), 18)
    say('消える辺: ' + ' / '.join(f'{a}–{b} {Eo[(a,b)]:.0f}' for a, b in gone))
    say('増える辺: ' + ' / '.join(f'{a}–{b} {En[(a,b)]:.0f}' for a, b in new))
    want_gone = {('ザグレブ', 'ヴェネツィア'), ('ジェノヴァ', 'パドヴァ'), ('トリエステ', 'プレスブルク'), ('トリノ', 'パドヴァ')}
    chk('消える辺の名前', {tuple(sorted(x)) for x in gone}, {tuple(sorted(x)) for x in want_gone})
    want_new = [('パドヴァ', 'ヴェネツィア', 36), ('トリエステ', 'パドヴァ', 212), ('トゥールーズ', 'ル・ペルテュス峠', 257), ('バルセロナ', 'ル・ペルテュス峠', 221),
                ('ボルドー', 'ル・ペルテュス峠', 574), ('ダリヤル峠', 'トビリシ', 206), ('ダリヤル峠', 'ロストフ', 921), ('ダリヤル峠', 'デルベント', 473),
                ('エルズルム', 'ダリヤル峠', 661), ('クラウゼンブルク', '赤い塔峠', 215), ('ブカレスト', '赤い塔峠', 288), ('ベオグラード', '赤い塔峠', 468),
                ('ソフィア', '赤い塔峠', 492), ('シプカ峠', 'ソフィア', 245), ('シプカ峠', 'ブカレスト', 296), ('イスタンブール', 'シプカ峠', 539),
                ('サロニカ', 'シプカ峠', 460), ('シプカ峠', '赤い塔峠', 487)]
    got_new = {tuple(sorted((x, y))): round(En[tuple(sorted((x, y)))]) if tuple(sorted((x, y))) in En else None for x, y, _ in want_new}
    chk('増える辺の名前と長さ', {k: v for k, v in got_new.items()}, {tuple(sorted((x, y))): z for x, y, z in want_new})
    comm = set(Eo) & set(En)
    chk('共通の辺の重みのビット一致', sum(1 for k in comm if Eo[k] != En[k]), 0)
    # 全対
    assert set(Po) == set(Pn)
    chg = [(k, Po[k][1], Pn[k][1]) for k in Po if Po[k][1] != Pn[k][1] and not (math.isinf(Po[k][1]) and math.isinf(Pn[k][1]))]
    shorter = [x for x in chg if x[2] < x[1]]; longer = [x for x in chg if x[2] > x[1]]
    chk('陸路の値が変わる対', (len(chg), len(shorter), len(longer)), (3934, 3896, 38))
    def gt3(P):
        return sum(1 for k, (gc, ld) in P.items() if gc > 0 and math.isfinite(ld) and ld / gc > 3)
    chk('陸路÷大圏＞3 の対', (gt3(Po), gt3(Pn)), (616, 603))
    mx = max(longer, key=lambda x: x[2] / x[1]) if longer else None
    say(f'長くなる対の最大: {mx[0]} {mx[1]:.0f}→{mx[2]:.0f} ×{mx[2]/mx[1]:.3f}')
    chk('最大 ×1.288（整数に丸めた 425/330 で）', round(round(mx[2]) / round(mx[1]), 3), 1.288)
    say(f'  参考＝丸めない比 {mx[2] / mx[1]:.6f}（事前登録の 1.288 は丸めた 425÷330＝門 S3 の ≦1.29 はどちらでも通る）')
    ends = {}
    for (k, o_, n_) in longer:
        for e in k: ends[e] = ends.get(e, 0) + 1
    say('長くなる 38 対の端点の度数（1 対に 2 端点）: ' + str(sorted(ends.items(), key=lambda x: -x[1])[:6]))
    say('長くなる 38 対（台本の出力から・門 S3 の部分集合の元）:')
    for (k, o_, n_) in sorted(longer):
        say(f'  LONGER\t{k[0]}\t{k[1]}\t{o_!r}\t{n_!r}\t{n_/o_:.4f}')
    def named(a, b):
        k = (a, b) if (a, b) in Po else (b, a)
        return k, Po[k], Pn[k]
    say('名指しの 6 対（旧→新の陸路 km・陸路÷大圏）:')
    want6 = {('パドヴァ', 'ヴェネツィア'): (242, 36), ('トビリシ', 'ロストフ'): (3286, 1127), ('トゥールーズ', 'バルセロナ'): (1386, 478),
             ('クラウゼンブルク', 'ブカレスト'): (1615, 503), ('ソフィア', 'ブカレスト'): (1301, 541), ('バクー', 'ロストフ'): (3657, 1777)}
    for (x, y), (wo, wn) in want6.items():
        k, po, pn = named(x, y)
        say(f'  {x}–{y}: {po[1]:.0f}→{pn[1]:.0f}（{po[1]/po[0]:.2f}→{pn[1]/pn[0]:.2f}）')
        chk(f'名指し {x}–{y}', (round(po[1]), round(pn[1])), (wo, wn))
    for (x, y), (wo, wn) in {('リヨン', 'バルセロナ'): (1956, 1112), ('トビリシ', 'グリエフ'): (3570, 2156), ('エルズルム', 'グリエフ'): (3624, 2515), ('バルセロナ', 'マルセイユ'): (1679, 835)}.items():
        k, po, pn = named(x, y)
        chk(f'記録 {x}–{y}', (round(po[1]), round(pn[1])), (wo, wn))
    same13 = [('ジブラルタル', 'タンジェ', 10033), ('ワルシャワ', 'ヨーテボリ', 3623), ('ドレスデン', 'ストックホルム', 3928), ('ボローニャ', 'オラン', 7171),
              ('シンゲッティ', 'ジブラルタル', 10409), ('バローダ', 'マスカット', 6231), ('ゴンダール', 'モカ', 4967), ('ムルズク', 'バルセロナ', 6593),
              ('ヘルシングフォシュ', 'リガ', 2912), ('ラパス', 'ペルー', 3557), ('チリ', 'プンタアレナス', 9267), ('ウジジ', 'キサンガニ', 4529), ('オムスク', 'グリエフ', 2054)]
    for x, y, v in same13:
        k, po, pn = named(x, y)
        chk(f'変わらない {x}–{y}', (round(po[1]), po[1] == pn[1]), (v, True))
    # 開幕の内陸州の港の脚
    assert set(Lo) == set(Ln)
    chkeys = [k for k in Lo if Lo[k][0] != Ln[k][0] or Lo[k][2] != Ln[k][2]]
    say(f'開幕の内陸州（港州でなく最寄り港のある州）: {len(Lo)} 州・変わる {len(chkeys)} 州')
    for k in chkeys:
        say(f'  {k[0]} {k[1]}: {Lo[k][0]}→{Ln[k][0]}  {Lo[k][2]:.0f}→{Ln[k][2]:.0f}  新÷旧 {Ln[k][2]/Lo[k][2]:.3f}')
    chk('内陸州の数', len(Lo), 119); chk('変わる州', len(chkeys), 3)
    chk('港の脚の陸路÷大圏＞3 の州', (sum(1 for v in Lo.values() if v[1] > 0 and v[2] / v[1] > 3), sum(1 for v in Ln.values() if v[1] > 0 and v[2] / v[1] > 3)), (3, 1))
    sg = sum(1 - Ln[k][2] / Lo[k][2] for k in Lo if Lo[k][2] > 0)
    chk('Σ(1−新÷旧)（小数 3 位）', round(sg, 3), 1.507)
    say(); say('台本の判定: ' + ('全て一致' if not bad else f'不一致 {len(bad)} 件＝腕を回さずに止める: ' + ' / '.join(bad)))
    if a.out:
        with open(a.out, 'wb') as f: f.write(('\n'.join(out) + '\n').encode('utf-8'))
    return 0 if not bad else 1


if a.phase in ('prep', 'all'): prep()
if a.phase in ('build', 'all'): build()
if a.phase in ('run', 'all'): run()
rc = 0
if a.phase in ('all', 'analyze', 'run'): rc = analyze()
if a.phase == 'all' and not a.keep: shutil.rmtree(W, ignore_errors=True)
sys.exit(rc)
