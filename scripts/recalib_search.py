# -*- coding: utf-8 -*-
# ★2026-09-03（キューD-38）：PYTHONUTF8 なしで起動されても**黙って化けない**ようにする自衛。
#   ⚠実害＝docs 100件が化けた（30%は例外で止まるが70%は黙って通る）。環境変数は作者の設定なのでここでは触らず、
#   スクリプト側で標準入出力を UTF-8 に張り替える（ファイル入出力は各所で encoding を明示しているのでこれで足りる）。
import sys as _sys
try:
    _sys.stdout.reconfigure(encoding='utf-8')
    _sys.stderr.reconfigure(encoding='utf-8')
except Exception:
    pass
"""経済規模の結び目＝同時再較正の無人探索ドライバ（2026-08-08・wf_d460b52d計画のStage1反証を織り込んだ改訂グリッド）。

反証（実測4連続不採用）＝結晶化の読者点火は「蓄積退蔵の結晶化→投資予算の収縮」を資本側の補償なしには支えられない。
本ドライバは**読者×資本側補償（IndInvestmentFrac増）を共役セルとして同時に動かす**。

運用規則（wf_d460b52d設計の運用規則を機械化）：
- 各セル＝Coefficients.cs を編集→canary（較正2スイート・約3.5分）→赤なら評価消費ゼロで棄却→緑のみ RUN_TESTS=1 measure.sh（約15分）
- 硬性ゲート赤（テスト赤 or 破綻>0）＝棄却。軟性裁定＝採用: Δ経済>=+1.5 かつ Δ総合>=-0.5 かつ 全軸>=基線-5
- incumbent（現最良）を保持し、終了時に incumbent 座標を Coefficients.cs へ復元（採用なしなら原状復元）
- 全セル（棄却含む）を docs/20260808/再較正探索_2026-08-08.md と .tsv へ記録

起動: python scripts/recalib_search.py   （リポジトリルートで・逐次実行・約3-5時間）
"""
import io, os, re, subprocess, shutil, sys, time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
COEF = os.path.join(ROOT, "PriceOfBread.SimCore", "Coefficients.cs")
DOCS = os.path.join(ROOT, "docs", "0808")
os.makedirs(DOCS, exist_ok=True)
LOG = os.path.join(DOCS, "再較正探索_2026-08-08.md")
TSV = os.path.join(DOCS, "再較正探索_2026-08-08.tsv")
BACKUP = COEF + ".search_backup"

# 基線（unify_fallback・2026-08-08採用状態）
BASE = {"政体": 27.8, "法": 48.1, "人口": 15.7, "列強": 16.3, "特異度": 88.7, "経済": 27.5, "領土本": 76.7, "領土植": 46.7}
# 基線更新2026-08-09＝colony_promo（植民地州昇格採用後）。前回8セル全滅の後、軌道ピン2本を再導出済み
# （公教育の絶対床1.25→1.10・NY産出財数30→12）＝同じセルがcanaryを通る可能性が開けた。
AXES = list(BASE.keys())

# 探索セル（順序＝感度大→小・各セルは差分のみ指定・未指定は既定値へ戻す）
DEFAULTS = {
    "HoardCrystallizeRate": "0.0", "WealthAnchorYears": "15.0", "WealthAnchorFloorPerCapita": "1000.0",
    "InvestIncomeFloor": "0.0", "InvestReadsLiquid": "0.0", "ConsumeReadsLiquid": "0.0",
    "IndInvestmentFrac": "0.05", "DemandElasticityMaxDaily": "2.5", "DemandElasticityMaxLuxury": "2.0",
    "IndPerConstruction": "0.01", "IndDepreciation": "0.003", "IndCapitalCost": "0.7",
}
READERS = {"HoardCrystallizeRate": "0.003", "InvestReadsLiquid": "1.0", "ConsumeReadsLiquid": "1.0", "InvestIncomeFloor": "0.4"}
CELLS = [
    # 共役グリッド1＝読者×投資比率の補償（液体≈ストックの1/4〜1/6を想定した予算パリティ）
    ("A1_Y15_Frac015", dict(READERS, WealthAnchorYears="15.0", IndInvestmentFrac="0.15")),
    ("A2_Y15_Frac025", dict(READERS, WealthAnchorYears="15.0", IndInvestmentFrac="0.25")),
    ("A3_Y8_Frac025",  dict(READERS, WealthAnchorYears="8.0",  IndInvestmentFrac="0.25")),
    ("A4_Y8_Frac040",  dict(READERS, WealthAnchorYears="8.0",  IndInvestmentFrac="0.40")),
    # 共役グリッド2＝最良Aセル（実行時に決定）＋需要天井（穏やか側）
    ("B1_elas30",      {"DemandElasticityMaxDaily": "3.0", "DemandElasticityMaxLuxury": "2.4"}),   # incumbentへ加算
    ("B2_elas35",      {"DemandElasticityMaxDaily": "3.5", "DemandElasticityMaxLuxury": "2.8"}),
    # 独立ノブ＝建設フロー（I*非接触の工業GDP純ノブ）
    ("C1_P007",        {"IndPerConstruction": "0.007"}),
    ("C2_P005",        {"IndPerConstruction": "0.005"}),
    # 資本補償対＝(IndDepreciation, IndCapitalCost) の I*保存ペア（ΔH≈−(ΔD/v)×I*_ref・v=0.012・I*≈2）
    ("D1_dep045_cap045", {"IndDepreciation": "0.0045", "IndCapitalCost": "0.45"}),
    ("D2_A2xD1",       dict(READERS, WealthAnchorYears="15.0", IndInvestmentFrac="0.25", IndDepreciation="0.0045", IndCapitalCost="0.45")),
]

def set_coef(name, value):
    s = io.open(COEF, encoding="utf-8").read()
    pat = re.compile(r"(public double %s = )[-0-9.]+(\s*;)" % re.escape(name))
    if not pat.search(s):
        raise RuntimeError("係数が見つからない: " + name)
    s = pat.sub(lambda m: m.group(1) + value + m.group(2), s, count=1)
    io.open(COEF, "w", encoding="utf-8").write(s)

def apply_cell(overrides):
    merged = dict(DEFAULTS)
    merged.update(overrides)
    for k, v in merged.items():
        set_coef(k, v)

def run(cmd, timeout):
    return subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=timeout, shell=isinstance(cmd, str))

def canary():
    # ⚠判定は終了コードで（2026-08-09修正＝日本語文字列照合は分離プロセスのコンソール文字化けで常に偽陰性＝第1-2走のC/D系の赤は無効だった）
    r = run('dotnet test PriceOfBread.Tests -c Release --nologo --filter "FullyQualifiedName~MultiNationTests|FullyQualifiedName~TechInstitutionTests"', 900)
    return r.returncode == 0

BASH = r"C:\Program Files\Git\bin\bash.exe"   # ⚠絶対パス必須（2026-08-09）：分離プロセスのPATHでは "bash"=System32のWSL bashに解決され measure.sh が即死＝第2走D2/第3走A1の「バッチ出力なし」の根因
def batch(name):
    env = dict(os.environ); env["RUN_TESTS"] = "1"
    r = subprocess.run([BASH, "measure.sh", name, "selfplay-batch", "10"], cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=3600, env=env)
    out_path = os.path.join(ROOT, name)
    if not os.path.exists(out_path):
        tail = (r.stdout or "")[-500:] + "|" + (r.stderr or "")[-300:]
        return None, "バッチ出力なし rc=%s tail=%s" % (r.returncode, " / ".join(tail.splitlines()))
    out = io.open(out_path, encoding="utf-8", errors="replace").read()
    tests_red = False
    tp = out_path + ".tests"
    if os.path.exists(tp):
        tests_red = "失敗!" in io.open(tp, encoding="utf-8", errors="replace").read()   # measure.shがUTF-8で書くファイル側を読む（コンソール非依存）
    hard_red = tests_red or ("破綻（未完走）: 0/10" not in out) or ("硬性ゲート緑" not in out)
    bands = {}
    for jp, key in [("政体exact", "政体"), ("法", "法"), ("人口", "人口"), ("列強・再現率", "列強"),
                    ("列強・特異度", "特異度"), ("経済規模", "経済"), ("領土・本国", "領土本"), ("領土・植民地", "領土植")]:
        m = re.search(re.escape(jp) + r": ([0-9.]+) %", out)
        if m: bands[key] = float(m.group(1))
    if len(bands) < 8:
        return None, "帯のパース失敗"
    return (bands, "硬性赤" if hard_red else "緑")

def score(bands):
    d_econ = bands["経済"] - BASE["経済"]
    d_total = sum(bands[a] - BASE[a] for a in AXES)
    floor_ok = all(bands[a] >= BASE[a] - 5.0 for a in AXES)
    adopt = d_econ >= 1.5 and d_total >= -0.5 and floor_ok
    return d_econ, d_total, floor_ok, adopt

def log(line):
    with io.open(LOG, "a", encoding="utf-8") as f:
        f.write(line + "\n")
    print(line, flush=True)

def main():
    shutil.copyfile(COEF, BACKUP)
    log(f"# 再較正探索 {time.strftime('%Y-%m-%d %H:%M')} 開始（基線＝unify_fallback）")
    with io.open(TSV, "w", encoding="utf-8") as f:
        f.write("cell\t" + "\t".join(AXES) + "\tΔ経済\tΔ総合\t判定\n")
    incumbent = None          # (name, overrides, bands)
    incumbent_extra = {}      # B/C 段で incumbent に積む差分
    try:
        for name, overrides in CELLS:
            eff = dict(incumbent[1]) if (incumbent and name[0] in "BC") else {}
            eff.update(incumbent_extra if name[0] in "BC" else {})
            eff.update(overrides)
            apply_cell(eff)
            log(f"\n## {name}: {eff}")
            if not canary():
                log(f"{name}: canary赤＝棄却（評価消費ゼロ）")
                with io.open(TSV, "a", encoding="utf-8") as f:
                    f.write(name + "\t" + "\t".join(["-"] * 8) + "\t-\t-\tcanary赤\n")
                continue
            result = batch("rs_" + name)
            if result[0] is None:
                log(f"{name}: {result[1]}＝棄却")
                continue
            bands, hard = result
            d_econ, d_total, floor_ok, adopt = score(bands)
            verdict = "硬性赤棄却" if hard == "硬性赤" else ("採用" if adopt else "棄却")
            log(f"{name}: {bands} Δ経済={d_econ:+.1f} Δ総合={d_total:+.1f} 床={floor_ok} → {verdict}")
            with io.open(TSV, "a", encoding="utf-8") as f:
                f.write(name + "\t" + "\t".join(f"{bands[a]:.1f}" for a in AXES) + f"\t{d_econ:+.1f}\t{d_total:+.1f}\t{verdict}\n")
            if verdict == "採用":
                if name[0] == "A":
                    incumbent = (name, eff, bands)
                else:
                    incumbent_extra.update(overrides)
                    if incumbent: incumbent = (incumbent[0] + "+" + name, dict(incumbent[1], **incumbent_extra), bands)
                    else: incumbent = (name, eff, bands)
        # 復元＝incumbent 採用座標 or 原状
        if incumbent:
            apply_cell(incumbent[1])
            log(f"\n# 終了：incumbent={incumbent[0]} を Coefficients.cs へ適用済み。最終帯={incumbent[2]}")
        else:
            shutil.copyfile(BACKUP, COEF)
            log("\n# 終了：採用セルなし＝Coefficients.cs を原状復元（読者点火は資本コスト側の追加次元が必要という結論を強化）")
    except Exception as e:
        shutil.copyfile(BACKUP, COEF)
        log(f"\n# 例外で中断・原状復元: {e!r}")
        raise
    finally:
        subprocess.run(["dotnet", "build", "PriceOfBread.SimCore", "-c", "Release", "--nologo"], cwd=ROOT, capture_output=True)

if __name__ == "__main__":
    main()
