#!/usr/bin/env bash
# 隔離測定ハーネス（2026-07-28）
# ── なぜ要るか ────────────────────────────────────────────────────────────
# 作業ツリーを複数セッションが同時に編集する運用のため、「ビルド→測定」の間に他人の編集が
# 混入し、決定論の検証が成立しなくなる事故が起きた（2026-07-28・詳細は docs/2026-07-28_測定汚染.md）。
# 症状は「挙動不変のはずの変更で軌道が全面的に動く」で、原因の切り分けに約1時間を要した。
#
# ── 何をするか ────────────────────────────────────────────────────────────
# ソース一式（bin/obj/docs/dist を除く実体 約8MB）を隔離ディレクトリへ複製し、**そこで**
# ビルドと測定を行う。以後どれだけ作業ツリーが編集されても測定結果は汚染されない。
# 併せてソースの SHA-256 マニフェストを出し、「どのソースを測ったか」を後から検証可能にする。
#
# ── 使い方 ────────────────────────────────────────────────────────────────
#   bash measure.sh <出力パス> [Runnerの引数...]
#   例: bash measure.sh /tmp/batch.txt selfplay-batch 10
#       RUN_TESTS=1 bash measure.sh /tmp/batch.txt selfplay-batch 10   ← テストも隔離側で回す（作業ツリーをロックしない）
#       bash measure.sh /tmp/chron.txt chronicle 1
#       SEEDS=5 bash measure.sh /tmp/b5.txt selfplay-batch     ← 探索の腕（5種）。採否を決める1本は種の数を書かない＝既定の10種
# 出力パスの隣に <出力パス>.manifest（ソースのハッシュ一覧と要約）も書く。
# 2つの測定が同じソースかは manifest 冒頭の SOURCE-HASH 行を比べれば分かる。

set -u
# ★2026-09-03（キューD-38）：文字化けの自衛。⚠測定結果の日本語が化けると「数字は合っているのに読めない」事故になる。
export PYTHONUTF8=1                 # このハーネスから呼ぶ補助スクリプト（将来含む）が黙って化けないように
export DOTNET_CLI_UI_LANGUAGE=ja    # dotnet の診断メッセージを固定（環境差でログの語が変わらない）
SRC_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT="${1:?出力パスを指定してください}"; shift
# ★2026-09-30（高速化の段 S4）：探索の腕は5種・採否を決める1本は10種。selfplay-batch の種の数は Runner の引数か SEEDS（既定10）で渡す
#   （旧＝種の数を書かないと Runner の既定の30種）。⚠ 引数と SEEDS が食い違えば止める＝10種と5種の帯を取り違えない。
#   種の数は報告の名乗り行（先頭行の seeds 1..N・史実忠実度の「完走N試合」）とマニフェストの RUN 行に出る。
RUN_NOTE=""
if [ "${1:-}" = "selfplay-batch" ]; then
  if [ -z "${2:-}" ]; then set -- selfplay-batch "${SEEDS:-10}"
  elif [ -n "${SEEDS:-}" ] && [ "$2" != "$SEEDS" ]; then echo "!! 種の数が食い違う（引数 $2・SEEDS=$SEEDS）＝測定を中止"; exit 1
  fi
  case "$2" in 10) RUN_NOTE="（10種＝採否）" ;; 5) RUN_NOTE="（5種＝探索）" ;; *) RUN_NOTE="（${2}種）" ;; esac
fi
# ★(248) 標準の計器を出した走行はマニフェストの RUN 行に名乗る（帯の本体は計器の有無で1バイトも変わらない＝本体からは区別できない）。
if [ -n "${POB_INSTRUMENTS:-}" ]; then RUN_NOTE="$RUN_NOTE（計器 POB_INSTRUMENTS=$POB_INSTRUMENTS${POB_INSTRUMENTS_NATIONS:+ NATIONS=$POB_INSTRUMENTS_NATIONS}）"; fi
# dotnet と隔離先は環境ごとに違う（機体を跨ぐと旧既定は存在しない）。明示指定＞既定パス＞PATH の順で解決する。
DOTNET="${DOTNET:-}"
if [ -z "$DOTNET" ] || [ ! -x "$DOTNET" ]; then
  for cand in /c/Users/takuto.motoki/.dotnet/dotnet.exe "$HOME/.dotnet/dotnet.exe" "$(command -v dotnet || true)"; do
    [ -n "$cand" ] && [ -x "$cand" ] && DOTNET="$cand" && break
  done
fi
[ -n "$DOTNET" ] && [ -x "$DOTNET" ] || { echo "!! dotnet が見つからない（DOTNET=... で明示指定してください）"; exit 1; }
# ★2026-09-26（fis-23）：既定の隔離先を**この実行だけのもの**にする（旧＝固定の pob-measure-snap を全実行で共有＝SNAP_DIR を渡し忘れた
#   2本目の measure.sh が1本目の走行中のスナップショットを rm -rf していた）。既定を使ったときだけ終わりに片付ける。
SNAP_OWNED=0
if [ -z "${SNAP_DIR:-}" ]; then SNAP_OWNED=1; fi
SNAP="${SNAP_DIR:-${TMPDIR:-/c/Users/$USERNAME/AppData/Local/Temp}/claude/pob-measure-snap-$$}"

echo "[1/4] ソースを隔離ディレクトリへ複製（bin/obj/docs/dist/measurements/uishot を除く）"
rm -rf "$SNAP" && mkdir -p "$SNAP" || exit 1
# ★2026-09-29（高速化の段 S1）：measurements（帯と計器の表 46MB）と uishot（撮影 18MB）も外す＝ビルドにも測定にも読まれない
#   （マニフェストが数える .cs/.json/.axaml/.csproj/.slnx はどちらにも無い＝SOURCE-HASH は変わらない）。
tar -cf - -C "$SRC_DIR" --exclude=bin --exclude=obj --exclude=docs --exclude=dist --exclude='.vs' --exclude='.git' --exclude=measurements --exclude=uishot . \
  | tar -xf - -C "$SNAP" || exit 1

echo "[2/4] ソースのマニフェストを作成"
( cd "$SNAP" && find . -type f \( -name '*.cs' -o -name '*.json' -o -name '*.axaml' -o -name '*.csproj' -o -name '*.slnx' \) \
    -not -path './obj/*' -not -path './bin/*' | sort | xargs sha256sum ) > "$OUT.manifest.tmp" || exit 1
HASH=$(sha256sum < "$OUT.manifest.tmp" | cut -c1-16)
{ echo "SOURCE-HASH: $HASH"; echo "FILES: $(wc -l < "$OUT.manifest.tmp")"; echo "RUN: $*$RUN_NOTE"; echo "---"; cat "$OUT.manifest.tmp"; } > "$OUT.manifest"
rm -f "$OUT.manifest.tmp"
echo "      SOURCE-HASH=$HASH"

echo "[3/4] 隔離ディレクトリでビルド"
# ★2026-09-29（高速化の段 S1）：テストを回さない測定は Runner（と SimCore）だけをビルドする＝App（Avalonia）と Tests を外す（測る Runner は同じ）。
BUILD_TARGET="$SNAP/PriceOfBread.slnx"
[ "${RUN_TESTS:-0}" = "1" ] || BUILD_TARGET="$SNAP/PriceOfBread.Runner/PriceOfBread.Runner.csproj"
"$DOTNET" build "$BUILD_TARGET" -c Release 2>&1 | grep -E "error|エラー|警告|成功" | head -5
[ "${PIPESTATUS[0]}" = "0" ] || { echo "!! ビルド失敗＝測定を中止"; exit 1; }

# テストも隔離側で回す（--test を付けたとき）。作業ツリーで dotnet test を叩くと testhost が
# PriceOfBread.SimCore.dll をロックし、並行してビルドできなくなる（2026-07-28 に実際に踏んだ）。
TEST_RC=0
if [ "${RUN_TESTS:-0}" = "1" ]; then
  echo "[3.5] 隔離ディレクトリでテスト"
  # ★2026-08-01：**テストの終了コードを捨てない**。旧は `| tail -1` でパイプ末尾の RC になり、
  #   テストが赤でも measure.sh は 0 を返していた＝Runner のゲート0が「全ゲート緑」と印字した裏でテストが落ちていた（実際に踏んだ）。
  # ★2026-09-27（作者裁定 2026-09-26）：全長の保存則の番人（約7分）は普段外す＝印 Category=FullLength。保存則に触る回は別に
  #   `dotnet test ... --filter "Category=FullLength"` で回す（README §1-2）。
  "$DOTNET" test "$SNAP/PriceOfBread.slnx" -c Release --no-build --filter "Category!=FullLength" > "$OUT.tests" 2>&1
  TEST_RC=$?
  tail -1 "$OUT.tests"
  [ "$TEST_RC" = "0" ] || echo "  ★赤：テストが失敗している（詳細＝$OUT.tests）"
fi

echo "[4/4] 測定: $*$RUN_NOTE"
# ★忠実度の基線（裁定①の条件2＝前回値との差分を必ず併記）は**作業ツリー直下**に置く。
# 隔離スナップショットは毎回作り直されるので、そこに置くと「前回」が常に存在しない。
export POB_FIDELITY_BASELINE="${POB_FIDELITY_BASELINE:-$SRC_DIR/.pob-fidelity-baseline.tsv}"
# ★2026-09-26（fis-23）：Runner は基線の1行目に走行条件（シード・長さ・係数の上書き・このソースの SOURCE-HASH）を書き、次の走行で突き合わせる
#   ＝長さや上書きの違う腕の値と黙って差を取らない（条件が違えば「前回値なし（条件違い）」）。
export POB_SOURCE_HASH="$HASH"
# ★2026-09-30（作者裁定「計算量の予算」）：selfplay-batch は計算量の測り（1試合あたりの CPU 秒・全長の件数の1試合の平均・推定の計算量・覆う割合・
#   予算の上限と目標・「予算の内／超過」の1行）を帯の報告とは**別のファイル** <出力>.cost へ書く（報告は恒等の検証で全行バイト一致を見る＝揺れる値を入れない）。
if [ "${1:-}" = "selfplay-batch" ]; then export POB_COST_OUT="$OUT.cost"; rm -f "$POB_COST_OUT"; fi
"$DOTNET" "$SNAP/PriceOfBread.Runner/bin/Release/net10.0/PriceOfBread.Runner.dll" "$@" > "$OUT" 2>&1
RC=$?
[ "$SNAP_OWNED" = "1" ] && rm -rf "$SNAP"   # 既定の隔離先だけ片付ける（SNAP_DIR を渡した腕は呼び手のもの＝残す）
echo "      終了コード=$RC　テスト=$TEST_RC　出力=$OUT$RUN_NOTE　マニフェスト=$OUT.manifest（SOURCE-HASH=$HASH）"
[ -n "${POB_COST_OUT:-}" ] && [ -f "$POB_COST_OUT" ] && echo "      計算量＝$POB_COST_OUT　$(grep -E '^判定:' "$POB_COST_OUT")"
# ★2026-09-30（止める規則＝作者裁定「計算量の予算」の6・README §1-2）：種ごとの exact／near の平均と標準誤差（<出力>.cost の「揺れ:」の1行）。
[ -n "${POB_COST_OUT:-}" ] && [ -f "$POB_COST_OUT" ] && grep -E '^揺れ:' "$POB_COST_OUT" | sed 's/^/      /'
# ★★終了コードは3値（裁定①・2026-08-01）。裁定順序「破綻しない ＞ 正しい挙動 ＞ バランス」を大小へ写す。
#   0 ＝ 硬性ゲート全緑・テスト緑・6次元も到達
#   1 ＝ 硬性ゲートが赤 **または テストが赤** ← 差し戻しの理由になる唯一の値
#   2 ＝ 硬性ゲート緑・テスト緑だが6次元が未達 ← 記録して進む（差し戻しの理由にならない）
# ⚠テストの赤は必ず1へ畳む（6次元の未達より重い＝「壊れていない」の側の判定だから）。
if [ "$TEST_RC" != "0" ] || [ "$RC" = "1" ]; then exit 1; fi
exit "$RC"
