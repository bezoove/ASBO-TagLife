using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The materials the new parts of town are built from (Resources/Town, made by the editor from tools/art/town), on
    /// top of the estate's (CouncilEstate.M). A missing one falls back to an estate material so a half-set-up project
    /// still builds.
    /// </summary>
    public static class TownMats
    {
        public static Material signal, signalHead, pole, yellow, signs, beacon, steel, glass, stone, gravel, water, reeds, mud,
            brownBrick, redBrick, pebble, panel, panelDark, towerWin, deckBrick0, deckBrick1, deckBrick2, deckBrick3, render, roofTile,
            graffiti, shopfronts2, metalSheet, rollerDoor, redPaint, hiVis, blueLamp, nhsBlue, tarmacRed, paviors, grassVerge, hedge,
            lobbyFloor, lobbyWall, liftDoor, notice, chainlink, courtGreen, playSoft, bench, rust, tyre, signsServices, glassDark, lino, curtain, bed;

        static Material L(string n) => Resources.Load<Material>("Town/" + n);
        static Material Or(Material a, Material b) => a != null ? a : b;

        public static bool Load()
        {
            if (!CouncilEstate.LoadMats()) return false;
            var E = CouncilEstate.M;
            signal = L("TownSignal"); signalHead = Or(L("TownSignalHead"), E.black); pole = Or(L("TownPole"), E.lampCol);
            yellow = Or(L("TownYellow"), E.line); signs = Or(L("TownSigns"), E.sign); beacon = Or(L("TownBeacon"), E.glow);
            steel = Or(L("TownSteel"), E.lampCol); glass = Or(L("TownGlass"), E.windows); stone = Or(L("TownStone"), E.concrete);
            gravel = Or(L("TownGravel"), E.drive); water = L("TownWater"); reeds = L("TownReeds"); mud = Or(L("TownMud"), E.drive);
            brownBrick = Or(L("TownBrownBrick"), E.brick); redBrick = Or(L("TownRedBrick"), E.brick); pebble = Or(L("TownPebble"), E.cream);
            panel = Or(L("TownPanel"), E.concrete); panelDark = Or(L("TownPanelDark"), E.concrete); towerWin = Or(L("TownTowerWindows"), E.windows);
            deckBrick0 = Or(L("TownDeckBrick0"), E.brick); deckBrick1 = Or(L("TownDeckBrick1"), E.brick); deckBrick2 = Or(L("TownDeckBrick2"), E.brick); deckBrick3 = Or(L("TownDeckBrick3"), E.brick);
            render = Or(L("TownRender"), E.cream); roofTile = Or(L("TownRoofTile"), E.roof); graffiti = L("TownGraffiti");
            shopfronts2 = L("TownShopfronts"); metalSheet = Or(L("TownMetalSheet"), E.concrete); rollerDoor = Or(L("TownRollerDoor"), E.garage[2]);
            redPaint = Or(L("TownRedPaint"), E.playRed); hiVis = Or(L("TownHiVis"), E.playYellow); blueLamp = Or(L("TownBlueLamp"), E.glow); nhsBlue = Or(L("TownNhsBlue"), E.playBlue);
            tarmacRed = Or(L("TownTarmacRed"), E.asphalt); paviors = Or(L("TownPaviors"), E.pave); grassVerge = L("TownGrass"); hedge = L("TownHedge");
            lobbyFloor = Or(L("TownLobbyFloor"), E.slabs); lobbyWall = Or(L("TownLobbyWall"), E.cream); liftDoor = Or(L("TownLiftDoor"), E.lampCol); notice = Or(L("TownNotice"), E.sign);
            chainlink = Or(L("TownChainlink"), E.railing); courtGreen = Or(L("TownCourtGreen"), E.rubber); playSoft = Or(L("TownPlaySoft"), E.rubber); bench = Or(L("TownBench"), E.black);
            rust = Or(L("TownRust"), E.post); tyre = Or(L("TownTyre"), E.black); signsServices = Or(L("TownServiceSigns"), E.sign); glassDark = Or(L("TownGlassDark"), E.windows);
            lino = Or(L("TownLino"), E.slabs); curtain = Or(L("TownCurtain"), E.cream); bed = Or(L("TownBed"), E.white);
            void T(Material m, float metres) { if (m != null && !MeshKit.Tile.ContainsKey(m)) MeshKit.Tile[m] = metres; }
            T(brownBrick, 2.7f); T(redBrick, 2.7f); T(pebble, 1.6f); T(panel, 2.4f); T(panelDark, 2.4f); T(render, 2f); T(roofTile, 2.5f);
            T(deckBrick0, 2.7f); T(deckBrick1, 2.7f); T(deckBrick2, 2.7f); T(deckBrick3, 2.7f); T(stone, 2f); T(gravel, 2f); T(mud, 2f);
            T(tarmacRed, 3f); T(paviors, 1.2f); T(metalSheet, 1.5f); T(lobbyFloor, 1.2f); T(lobbyWall, 2f); T(chainlink, 2f); T(courtGreen, 4f); T(playSoft, 2f);
            T(lino, 2f); T(grassVerge, 3f); T(hedge, 2f); T(rust, 1.5f);
            return true;
        }
    }
}
