import re,sys,os
UN = """LastEarned LastEarnedPerMonth
IndustrialRent IndustrialRentPerMonth
LastInterest LastInterestPerMonth
LastAdminClaim LastAdminClaimPerMonth
LastSoldierPay LastSoldierPayPerMonth
LastMilitaryClaim LastMilitaryClaimPerMonth
LastWelfareCost LastWelfareCostPerMonth
LastPensionCost LastPensionCostPerMonth
LastColonialUpkeep LastColonialUpkeepPerMonth
FiscalCapacity FiscalCapacityPerMonth
LastTariffRevenue LastTariffRevenuePerClearing
LastTributeOut LastTributeOutPerQuarter
LastTributeIn LastTributeInPerQuarter
GovBondRate GovBondRatePerYear
GovAvgCoupon GovAvgCouponPerYear
ExpectedInflation ExpectedInflationPerYear
SoldierSize SoldierSizeK
DefaultCooldownQuarters DefaultCooldownMonths
BondRepayFrac BondRepayFracPerMonth
BondRolloverFrac BondRolloverFracPerMonth
BondRateMax BondRateMaxPerYear
BondBaseRate BondBaseRatePerYear
LoanSpread LoanSpreadPerYear
OpulenceSpendRate OpulenceSpendRatePerYear
PensionCostRate PensionCostRatePerMonth
WelfareCostRate WelfareCostRatePerMonth
SoldierMigrationRate SoldierMigrationRatePerMonth
MilitaryWearRates MilitaryWearRatesPerMonth
SubsidyDependenceRate SubsidyDependenceRatePerMonth
PrizeMoneyRate PrizeMoneyRatePerMonth
MintFloorBuyRate MintFloorBuyRatePerMonth
MintReserveSellRate MintReserveSellRatePerMonth
ForceUnlockTreasuryCost ForceUnlockTreasuryCostPerMonth
BorrowCooldown BorrowCooldownMonths"""
pairs=[l.split() for l in UN.splitlines()]
for p in sys.argv[1:]:
    b=open(p,'rb').read(); bom=b[:3]==b'\xef\xbb\xbf'; s=(b[3:] if bom else b).decode('utf-8'); tot=0
    for a,c in pairs:
        pat=r'(?<=\.)'+a+r'(?![A-Za-z0-9_])'
        n=len(re.findall(pat,s))
        if n: s=re.sub(pat,c,s); tot+=n; print(os.path.relpath(p),a,n)
    if tot: open(p,'wb').write((b'\xef\xbb\xbf' if bom else b'')+s.encode('utf-8'))
