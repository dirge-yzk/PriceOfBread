namespace PriceOfBread.SimCore;

/// <summary>
/// 輸送容量（§5.1拡張）：鉄道（内陸→港）と海運（港→世界市場）の容量を、鉄道財/船財の投資で積む。
/// 投資家が国内市場から鉄道(線路+機関車)/船(帆船+汽船)を買い→容量に変換（在庫消費＝非貨幣）。
/// 容量は毎期減耗し、維持に再投資が要る＝鉄道/船の持続需要を生む（史実：輸送財の需要増）。
/// 金は 投資家pop→生産者所有者pop で移転＝保存（BaseMoney不変）。積んだ容量が交易量を律速する（<see cref="Trade"/>）。
/// </summary>
public static class Transport
{
    public static void Step(World w, Coefficients c)
    {
        if (!c.TransportEnabled) return;   // TradeEnabledに依存しない（多国シムは InternationalMarket 経由・TradeEnabled=false のため）
        w.RailCapacity = System.Math.Max(0, w.RailCapacity * (1 - c.TransportDecay));   // 減耗（鉄道は0スタート）
        w.ShipCapacity = System.Math.Max(0, w.ShipCapacity * (1 - c.TransportDecay));
        double target = w.TotalPopulation * c.TransportTargetPerPop;   // 経済規模に応じた目標容量
        // ★T4（輸送doc §3-1・2026-08-13）：目標の錨を**人口**から「実際に運びたかった量」（LastTradePotential＝律速率の分母）へ寄せる。
        //   ⚠人口だけの目標は、交易しない人口大国に容量を積ませ、交易集約の小国に積ませない（軍とまったく同じ病＝費用も需要も見ない目標追従）。
        //   ★形は軍で確立したもの（扶養限界のブレンド比 ArmySustainWeight）を踏襲する——⚠`Math.Max(式A, 式B)` で競わせない。
        //     2式が拮抗する国で支配側が切り替わるたび値が不連続に飛ぶ（軍の3回目の失敗の原因）。**片方に寄せるのでなくブレンドで統一する。**
        //   ★人口項を残すのは較正の土台であると同時に、容量0からの立ち上がりを支えるため（潜在量は容量が無いと観測されない＝鶏卵）。
        //   既定0＝人口のみ＝**厳密恒等**。
        if (c.TransportPotentialWeight > 0)
        {
            double pot = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) pot += r.LastTradePotential;
            if (!(pot >= 0)) pot = 0;   // NaNは人口項のみへ倒す（否定形で書く）
            target = target * (1.0 - c.TransportPotentialWeight) + pot * c.TransportPotentialHeadroom * c.TransportPotentialWeight;
        }
        // ★安保doc手順3（2026-08-30点火＝既定1.0）：国防の目的関数＝受けている脅威が大きいほど目標容量を積む（戦略鉄道の一般化＝路線を1本も定義しない）。
        //   脅威比＝前期の SecurityThreat/自国質量（無次元）。target×(1+k×脅威比)＝民間項（上）に**乗せるだけ**＝どの路線かは書かない。
        //   ⚠正のフィードバック（鉄道→脅威→鉄道）は国庫の有限性だけが歯止め。0＝この分岐を通らない＝厳密恒等。
        if (c.ThreatRailTargetWeight > 0 && w.SecurityThreatOwnMass > 1e-9)
        {
            double threatRatio = w.SecurityThreat / w.SecurityThreatOwnMass;
            if (threatRatio > 0 && double.IsFinite(threatRatio)) target *= 1.0 + c.ThreatRailTargetWeight * threatRatio;
        }
        // 接3（監査2026-07-11・2026-07-25休眠配線・2026-07-27点火＝既定1.0）：混雑シグナル（輸送律速率）を目標へ接続＝交易集約の小国は容量を積み増し、
        // 交易の乏しい人口大国は使わない容量へ資金を誤配分しない。混雑＝活動州の(1−律速率)平均（港湾の混雑ゲートと同じ状態量）。0＝不読＝厳密恒等。
        if (c.TransportCongestionWeight > 0)
        {
            double congSum = 0; int congN = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) { congSum += 1.0 - r.LastTradeThrottle; congN++; }
            if (congN > 0) target *= 1.0 + c.TransportCongestionWeight * (congSum / congN);
        }
        // 鉄道は技術で解禁（史実1820s〜）：技術がonset未満は内陸輸送が道路/河川（基準値）のみ＝序盤の内陸交易は強く絞られる。船（帆船＝海運）は古来あり据え置き。
        double railGate = System.Math.Clamp((w.TechLevel - c.RailTechOnset) / System.Math.Max(1e-9, c.RailTechWidth), 0, 1);   // 0除算ガード（BilateralMarket.cs の同式と同じ守り）
        BuyToward(w, c, [c.RailGoodId, c.LocoGoodId], target * railGate, rail: true);
        var (shipAdd, steamAdd) = BuyToward(w, c, c.ShipGoodIds, target, rail: false);
        // 運用コスト監査2026-08-16 #3（汽船の運行燃料・休眠）：ShipCapacity は帆船と汽船の合算＝購入後に区別が消え運航費が同一
        // （帆船消滅の史実主因の欠落）。購入時の汽船比率をEMAで1本持ち、燃料需要＝ShipFuelRate×ShipCapacity×汽船比率。
        // ⚠EMA更新も係数ゲート内＝ShipFuelRate=0（既定）では World.ShipSteamShareEma を一切読み書きしない＝厳密恒等。
        if (c.ShipFuelRate > 0)
        {
            if (shipAdd > 1e-9)
            {
                double share = steamAdd / shipAdd;
                if (!(share >= 0 && share <= 1)) share = 0;   // 否定形＝NaNは帆走側へ
                double a = System.Math.Clamp(c.ShipSteamShareSmoothing, 0, 1);
                w.ShipSteamShareEma = (1.0 - a) * w.ShipSteamShareEma + a * share;
                if (!(w.ShipSteamShareEma >= 0 && w.ShipSteamShareEma <= 1)) w.ShipSteamShareEma = 0;
            }
            BuyShipFuel(w, c);
        }
    }

    // #3 汽船の運行燃料（運用コスト監査2026-08-16）：商人pop（船の買い手＝BuyToward:59 と同じ選択）が地元市場から石炭（または重油＝
    // Colonial B6 の二択・供給実在ガード込み）を買って焚く。決済は BuyToward の船購入と同一パターン（需要計上→在庫/財布律速→
    // 生産者所有者へ支払い＝pop→pop 移転で保存・LastSold 計上で財収支を保つ）。呼び手が ShipFuelRate>0 でゲート済み。
    private static void BuyShipFuel(World w, Coefficients c)
    {
        double totalWant = c.ShipFuelRate * w.ShipCapacity * w.ShipSteamShareEma;   // 帆走のみ（EMA=0）なら需要ゼロ
        if (!(totalWant > 1e-9)) return;   // 否定形＝NaNも不走行側へ
        int regCount = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active && (r.MerchantPopIndex >= 0 || r.InvestorPopIndex >= 0)) regCount++;
        if (regCount == 0) return;
        double per = totalWant / regCount;   // BuyToward と同じ州按分（1州あたりの希望量）
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            int buyerIdx = r.MerchantPopIndex >= 0 ? r.MerchantPopIndex : r.InvestorPopIndex;   // 船＝商人が負担（BuyToward:59 と同一の選択）
            if (buyerIdx < 0 || Catalog.CoalId >= r.Markets.Length) continue;
            var buyer = r.Pops[buyerIdx];
            var (fuelG, want) = Colonial.ChooseFuelGood(r, c, per);   // 石炭→重油の二択（海軍・植民地補給線と同式）
            var m = r.Markets[fuelG];
            m.LastDemand += want;               // 在庫が無くても需要を計上＝生産を駆動（BuyToward と同じ鶏卵回避）
            if (m.Stock <= 1e-9 || m.Price <= 1e-9) continue;
            double buy = System.Math.Min(m.Stock, want);
            double pay = buy * m.Price;
            if (pay > buyer.Savings) { pay = System.Math.Max(0, buyer.Savings); buy = pay / m.Price; }
            if (buy <= 1e-9) continue;
            m.Stock -= buy;
            m.LastSold += buy;          // 汽船が焚く＝消費（財収支に計上）
            buyer.Savings -= pay;
            var prod = r.Producers.Find(x => x.GoodId == fuelG);
            if (prod is not null && (uint)prod.OwnerPopIndex < (uint)r.Pops.Count) r.Pops[prod.OwnerPopIndex].Savings += pay;   // 売り手（生産者の所有者）へ＝保存
            else buyer.Savings += pay;                                       // 生産者不在／添字範囲外なら払い戻し＝保存（BuyToward と同じ境界ガード）
        }
    }

    // 目標容量へ向けて鉄道/船を国内市場から購入→容量を積む。**鉄道＝投資家（資本家）・船＝商人**が負担：
    // 商人は貿易と流通で稼ぎ、商船を買って海運を支える（交易↑→商人が富む→商船↑→さらに交易↑の自己強化）。
    // 在庫が無くても希望量を需要に計上＝生産を立ち上げる（鶏卵回避）。買えた分だけ容量に変換（在庫消費＝非貨幣）。金は買い手pop→生産者所有者で保存。
    // 戻り値＝今月積んだ容量の（合計, うち汽船tier2分）。#3 運行燃料の汽船比率EMAが読む（読み手が居なくても集計は局所変数のみ＝挙動不変）。
    private static (double Total, double Steam) BuyToward(World w, Coefficients c, int[] goods, double target, bool rail)
    {
        double addTotal = 0, addSteam = 0;
        double cap = rail ? w.RailCapacity : w.ShipCapacity;
        double gapUnits = (target - cap) * c.TransportBuildRate / c.TransportCapPerUnit;   // 今月詰めたい総単位数
        if (gapUnits <= 1e-9) return (0, 0);
        int regCount = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active && r.InvestorPopIndex >= 0) regCount++;
        if (regCount == 0) return (0, 0);
        double perGood = gapUnits / regCount / goods.Length;   // 1州1財あたりの希望単位
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || r.InvestorPopIndex < 0) continue;
            int buyerIdx = rail ? r.InvestorPopIndex : (r.MerchantPopIndex >= 0 ? r.MerchantPopIndex : r.InvestorPopIndex);
            var investor = r.Pops[buyerIdx];
            foreach (int gi in goods)
            {
                if (gi < 0 || gi >= r.Markets.Length) continue;
                var m = r.Markets[gi];
                // T1（輸送doc §3-3・2026-07-31）：**買えない国の需要が世界市場に載り、買える国の調達費を押し上げる**歪みの是正。
                // 旧＝希望量をそのまま計上（在庫0でも生産を駆動する鶏卵回避が目的で、意図は正しい）。⚠ ただし財布を一切見ていなかった。
                // >0 で「買い手の手持ちで買える単位数 × これ」を希望量の上限にする＝**貯蓄があれば在庫0でも需要は立つ**（鶏卵回避は保つ）が、無い袖の需要は消える。
                // 0（既定）＝上限なし＝分岐に入らず**厳密恒等**。
                double want = perGood;
                if (c.TransportDemandAffordCap > 0 && m.Price > 1e-9)
                    want = System.Math.Min(want, c.TransportDemandAffordCap * System.Math.Max(0, investor.Savings) / m.Price);
                m.LastDemand += want;               // 在庫が無くても需要を計上＝生産を駆動（鉄道/船の需要が上がる＝史実・ブートストラップ）
                if (m.Stock <= 1e-9 || m.Price <= 1e-9) continue;
                double buy = System.Math.Min(m.Stock, want);
                double pay = buy * m.Price;
                if (pay > investor.Savings) { pay = System.Math.Max(0, investor.Savings); buy = pay / m.Price; }
                if (buy <= 1e-9) continue;
                m.Stock -= buy;
                m.LastSold += buy;          // 財収支に計上（輸送購入＝販売）。在庫減を販売として記録し§11.6財収支を保つ
                investor.Savings -= pay;
                var prod = r.Producers.Find(x => x.GoodId == gi);
                if (prod is not null && (uint)prod.OwnerPopIndex < (uint)r.Pops.Count) r.Pops[prod.OwnerPopIndex].Savings += pay;   // 売り手（生産者の所有者）へ＝金は保存（兄弟の pay-owner 経路と同じ境界ガード）
                else investor.Savings += pay;                                       // 生産者不在／所有者添字が範囲外なら払い戻し＝金は保存
                double mult = (!rail && gi == c.ShipTier2GoodId) ? c.ShipTier2CapMult : 1.0;   // 汽船=tier2＝1単位あたり容量が大（蒸気船の革命）
                double add = buy * c.TransportCapPerUnit * mult;
                if (rail) w.RailCapacity += add; else w.ShipCapacity += add;
                addTotal += add;
                if (!rail && gi == c.ShipTier2GoodId) addSteam += add;   // #3 汽船比率（容量加重）の分子
            }
        }
        return (addTotal, addSteam);
    }
}
