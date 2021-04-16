﻿using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Resources;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Compiler.DMM;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamServer.Dream {
    class DreamMap {
        public struct MapCell {
            public DreamObject Area;
            public UInt32 Turf;
        }

        public Dictionary<DreamPath, DreamObject> Areas = new();
        public MapCell[,] Cells { get; private set; }
        public int Width { get => Cells.GetLength(0); }
        public int Height { get => Cells.GetLength(1); }

        public void LoadMap(DreamResource mapResource) {
            string dmmSource = mapResource.ReadAsString();
            DMMParser dmmParser = new DMMParser(new DMLexer(dmmSource));
            DMMParser.Map map = dmmParser.ParseMap();

            Cells = new MapCell[map.MaxX - 1, map.MaxY - 1];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Cells[x, y] = new MapCell();
                }
            }

            foreach (DMMParser.MapBlock mapBlock in map.Blocks) {
                foreach (KeyValuePair<(int X, int Y), string> cell in mapBlock.Cells) {
                    DMMParser.CellDefinition cellDefinition = map.CellDefinitions[cell.Value];
                    DreamObject turf = CreateMapObject(cellDefinition.Turf);
                    DreamPath areaPath = cellDefinition.Area?.Type ?? DreamPath.Area;
                    int x = mapBlock.X + cell.Key.X - 1;
                    int y = mapBlock.Y + cell.Key.Y - 1;

                    if (turf == null) turf = Program.DreamObjectTree.CreateObject(DreamPath.Turf);

                    SetTurf(x, y, turf);
                    SetArea(x, y, cellDefinition.Area.Type);
                    foreach (DMMParser.MapObject mapObject in cellDefinition.Objects) {
                        CreateMapObject(mapObject, turf);
                    }
                }
            }
        }

        public void SetTurf(int x, int y, DreamObject turf) {
            if (!turf.IsSubtypeOf(DreamPath.Turf)) {
                throw new Exception("Turf was not a sub-type of " + DreamPath.Turf);
            }

            turf.SetVariable("x", new DreamValue(x));
            turf.SetVariable("y", new DreamValue(y));
            turf.SetVariable("z", new DreamValue(1));

            Cells[x - 1, y - 1].Turf = DreamMetaObjectAtom.AtomIDs[turf];
            Program.DreamStateManager.AddTurfDelta(x - 1, y - 1, turf);
        }

        public void SetArea(int x, int y, DreamPath areaPath) {
            if (!areaPath.IsDescendantOf(DreamPath.Area) || !Program.DreamObjectTree.HasTreeEntry(areaPath)) {
                throw new Exception("Invalid area " + areaPath);
            }

            DreamObject area;
            if (!Areas.TryGetValue(areaPath, out area)) {
                area = Program.DreamObjectTree.CreateObject(areaPath);

                Areas.Add(areaPath, area);
            }

            if (area.GetVariable("x").GetValueAsInteger() > x) area.SetVariable("x", new DreamValue(x));
            if (area.GetVariable("y").GetValueAsInteger() > y) area.SetVariable("y", new DreamValue(y));

            Cells[x - 1, y - 1].Area = area;
        }

        public Point GetTurfLocation(DreamObject turf) {
            if (!turf.IsSubtypeOf(DreamPath.Turf)) {
                throw new Exception("Turf is not a sub-type of " + DreamPath.Turf);
            }

            UInt32 turfAtomID = DreamMetaObjectAtom.AtomIDs[turf];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    if (Cells[x, y].Turf == turfAtomID) return new Point(x + 1, y + 1);
                }
            }

            return new Point(0, 0); //Not on the map
        }

        public DreamObject GetTurfAt(int x, int y) {
            return DreamMetaObjectAtom.AtomIDToAtom[Cells[x - 1, y - 1].Turf];
        }

        public DreamObject GetAreaAt(int x, int y, int z) {
            return Cells[x - 1, y - 1].Area;
        }

        private DreamObject CreateMapObject(DMMParser.MapObject mapObject, DreamObject loc = null) {
            if (!Program.DreamObjectTree.HasTreeEntry(mapObject.Type)) {
                Console.WriteLine("MAP LOAD: Skipping " + mapObject.Type);

                return null;
            }

            DreamObjectDefinition definition = Program.DreamObjectTree.GetObjectDefinitionFromPath(mapObject.Type);
            if (mapObject.VarOverrides.Count > 0) {
                definition = new DreamObjectDefinition(definition);

                foreach (KeyValuePair<string, DreamValue> varOverride in mapObject.VarOverrides) {
                    if (definition.HasVariable(varOverride.Key)) {
                        definition.Variables[varOverride.Key] = varOverride.Value;
                    }
                }
            }

            return new DreamObject(definition, new DreamProcArguments(new() { new DreamValue(loc) }));
        }
    }
}
