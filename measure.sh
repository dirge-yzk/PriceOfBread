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
# 出力パスの隣に <出力パス>.manifest（ソースのハッシュ一覧と要約）も書く。
# 2つの測定が同じソースかは manifest 冒頭の SOURCE-HASH 行を比べれば分かる。

set -u
# ★2026-09-03（キューD-38）：文字化けの自衛。⚠測定結果の日本語が化けると「数字は合っているのに読めない」事故になる。
export PYTHONUTF8=1                 # このハーネスから呼ぶ補助スクリプト（将来含む）が黙って化けないように
export DOTNET_CLI_UI_LANGUAGE=ja    # dotnet の診断メッセージを固定（環境差でログの語が変わらない）
SRC_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT="${1:?出力パスを指定してください}"; shift
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

echo "[1/4] ソースを隔離ディレクトリへ複製（bin/obj/docs/dist を除く）"
rm -rf "$SNAP" && mkdir -p "$SNAP" || exit 1
tar -cf - -C "$SRC_DIR" --exclude=bin --exclude=obj --exclude=docs --exclude=dist --exclude='.vs' --exclude='.git' . \
  | tar -xf - -C "$SNAP" || exit 1

echo "[2/4] ソースのマニフェストを作成"
( cd "$SNAP" && find . -type f \( -name '*.cs' -o -name '*.json' -o -name '*.axaml' -o -name '*.csproj' -o -name '*.slnx' \) \
    -not -path './obj/*' -not -path './bin/*' | sort | xargs sha256sum ) > "$OUT.manifest.tmp" || exit 1
HASH=$(sha256sum < "$OUT.manifest.tmp" | cut -c1-16)
{ echo "SOURCE-HASH: $HASH"; echo "FILES: $(wc -l < "$OUT.manifest.tmp")"; echo "---"; cat "$OUT.manifest.tmp"; } > "$OUT.manifest"
rm -f "$OUT.manifest.tmp"
echo "      SOURCE-HASH=$HASH"

echo "[3/4] 隔離ディレクトリでビルド"
"$DOTNET" build "$SNAP/PriceOfBread.slnx" -c Release 2>&1 | grep -E "error|エラー|警告|成功" | head -5
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

echo "[4/4] 測定: $*"
# ★忠実度の基線（裁定①の条件2＝前回値との差分を必ず併記）は**作業ツリー直下**に置く。
# 隔離スナップショットは毎回作り直されるので、そこに置くと「前回」が常に存在しない。
export POB_FIDELITY_BASELINE="${POB_FIDELITY_BASELINE:-$SRC_DIR/.pob-fidelity-baseline.tsv}"
# ★2026-09-26（fis-23）：Runner は基線の1行目に走行条件（シード・長さ・係数の上書き・このソースの SOURCE-HASH）を書き、次の走行で突き合わせる
#   ＝長さや上書きの違う腕の値と黙って差を取らない（条件が違えば「前回値なし（条件違い）」）。
export POB_SOURCE_HASH="$HASH"
"$DOTNET" "$SNAP/PriceOfBread.Runner/bin/Release/net10.0/PriceOfBread.Runner.dll" "$@" > "$OUT" 2>&1
RC=$?
[ "$SNAP_OWNED" = "1" ] && rm -rf "$SNAP"   # 既定の隔離先だけ片付ける（SNAP_DIR を渡した腕は呼び手のもの＝残す）
echo "      終了コード=$RC　テスト=$TEST_RC　出力=$OUT　マニフェスト=$OUT.manifest（SOURCE-HASH=$HASH）"
# ★★終了コードは3値（裁定①・2026-08-01）。裁定順序「破綻しない ＞ 正しい挙動 ＞ バランス」を大小へ写す。
#   0 ＝ 硬性ゲート全緑・テスト緑・6次元も到達
#   1 ＝ 硬性ゲートが赤 **または テストが赤** ← 差し戻しの理由になる唯一の値
#   2 ＝ 硬性ゲート緑・テスト緑だが6次元が未達 ← 記録して進む（差し戻しの理由にならない）
# ⚠テストの赤は必ず1へ畳む（6次元の未達より重い＝「壊れていない」の側の判定だから）。
if [ "$TEST_RC" != "0" ] || [ "$RC" = "1" ]; then exit 1; fi
exit "$RC"
