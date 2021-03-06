﻿using EDDiscovery;
using EDDiscovery.DB;
using EDDiscovery2.DB;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using EDDiscovery2.Trilateration;
using OpenTK;
using System.Resources;
using EDDiscovery.Properties;
using EDDiscovery.EDSM;

namespace EDDiscovery2._3DMap
{
    public class DatasetBuilder
    {
        private List<IData3DSet> _datasets;
        private static Dictionary<string, TexturedQuadData> _cachedTextures = new Dictionary<string, TexturedQuadData>();
        private static Dictionary<string, Bitmap> _cachedBitmaps = new Dictionary<string, Bitmap>();

        public EDDConfig.MapColoursClass MapColours { get; set; } = EDDConfig.Instance.MapColours;

        public Vector2 MinGridPos { get; set; } = new Vector2(-50000.0f, -20000.0f);
        public Vector2 MaxGridPos { get; set; } = new Vector2(50000.0f, 80000.0f);

        int gridunitSize = 1000;

        public DatasetBuilder()
        {
            _datasets = new List<IData3DSet>();
        }


        #region MAPS

       public List<IData3DSet> AddMapImages(FGEImage [] list)
        {
            if (list.Length > 0 )
            {
                var datasetMapImg = Data3DSetClass<TexturedQuadData>.Create("mapimage", Color.White, 1.0f);

                for( int i = 0; i < list.Length; i++)
                {
                    FGEImage img = list[i];

                    if (_cachedTextures.ContainsKey(img.FileName))
                    {
                        datasetMapImg.Add(_cachedTextures[img.FileName]);
                    }
                    else
                    {
                        Bitmap bmp = (Bitmap)Bitmap.FromFile(img.FilePath);
                                                                                    
                        Vector3d centre = new Vector3d((img.TopLeft.X + img.BottomRight.X) / 2, 0, (img.TopRight.Y + img.BottomLeft.Y) / 2);
                        float width = img.TopRight.X - img.BottomLeft.X;
                        float height = img.TopLeft.Y - img.BottomRight.Y;           // its rectangular.. so does not really matter which left/right/top/bot you use

                        var texture = TexturedQuadData.FromBitmap(bmp, centre, TexturedQuadData.NoRotation, width, height );

                        _cachedTextures[img.FileName] = texture;
                        datasetMapImg.Add(texture);
                    }
                }
                _datasets.Add(datasetMapImg);
            }

            return _datasets;
        }

        #endregion

        #region Bookmarks

        public List<IData3DSet> AddStarBookmarks(Bitmap mapstar, Bitmap mapregion, Bitmap maptarget, float widthly, float heightly, Vector3 rotation )
        {
            var datasetbks = Data3DSetClass<TexturedQuadData>.Create("bkmrs", Color.White, 1f);

            long bookmarktarget = TargetClass.GetTargetBookmark();

            foreach (BookmarkClass bc in BookmarkClass.bookmarks)
            {
                Bitmap touse = (bc.id == bookmarktarget) ? maptarget : ((bc.isRegion) ? mapregion : mapstar);
                TexturedQuadData newtexture = TexturedQuadData.FromBitmap(touse, new PointData(bc.x, bc.y, bc.z), rotation, widthly, heightly,  0, heightly / 2);
                newtexture.Tag = bc;
                newtexture.Tag2 = 0;        // bookmark
                datasetbks.Add(newtexture);
            }

            _datasets.Add(datasetbks);

            return _datasets;
        }

        public List<IData3DSet> AddNotedBookmarks(Bitmap map, Bitmap maptarget, float widthly, float heightly, Vector3 rotation, List<VisitedSystemsClass> VisitedSystems)
        {
            var datasetbks = Data3DSetClass<TexturedQuadData>.Create("bkmrs", Color.White, 1f);

            long bookmarknoted = TargetClass.GetTargetNotedSystem();

            if (VisitedSystems != null)
            {
                foreach (VisitedSystemsClass vs in VisitedSystems)
                {
                    SystemNoteClass notecs = SystemNoteClass.GetSystemNoteClass(vs.Name);

                    if (notecs != null)         // if we have a note..
                    {
                        string note = notecs.Note.Trim();

                        if (note.Length > 0)
                        {
                            PointData pd = new PointData((vs.HasTravelCoordinates) ? vs.X : vs.curSystem.x, (vs.HasTravelCoordinates) ? vs.Y : vs.curSystem.y, (vs.HasTravelCoordinates) ? vs.Z : vs.curSystem.z);

                            Bitmap touse = (notecs.id == bookmarknoted) ? maptarget : map;
                            TexturedQuadData newtexture = TexturedQuadData.FromBitmap(touse, pd, rotation, widthly, heightly, 0, heightly / 2);
                            newtexture.Tag = vs;
                            newtexture.Tag2 = 1;        // note mark
                            datasetbks.Add(newtexture);
                        }
                    }
                }
            }

            _datasets.Add(datasetbks);

            return _datasets;
        }

        public void UpdateBookmarks(ref List<IData3DSet> _datasets, float widthly, float heightly, Vector3 rotation )
        {
            if (_datasets == null)
                return;

            foreach (IData3DSet dataset in _datasets)
            {
                TexturedQuadDataCollection tqdc = dataset as TexturedQuadDataCollection;

                foreach (TexturedQuadData tqd in tqdc.BaseTextures)
                {
                    PointData pd;
                    if ( (int)tqd.Tag2 == 1)
                    {
                        VisitedSystemsClass vs = (VisitedSystemsClass)tqd.Tag;
                        pd = new PointData((vs.HasTravelCoordinates) ? vs.X : vs.curSystem.x, (vs.HasTravelCoordinates) ? vs.Y : vs.curSystem.y, (vs.HasTravelCoordinates) ? vs.Z : vs.curSystem.z);
                    }
                    else
                    {
                        BookmarkClass bc = (BookmarkClass)tqd.Tag;
                        pd = new PointData(bc.x, bc.y, bc.z);
                    }

                    tqd.UpdateVertices(pd, rotation, widthly, heightly, 0, heightly / 2);
                }
            }
        }

        #endregion

        #region GMOs

        static Font gmostarfont = new Font("MS Sans Serif", 20F);       // font size really determines the nicenest of the image, not its size on screen..
        float gmonameoff = -0.75F;      // bitmap is from +0.5 to -0.5.  
        float gmotargetoff = 1F;
        float gmoselonly = 0.75F;
        float gmoseltarget = 1.75F;

        public List<IData3DSet> AddGalMapObjectsToDataset(Bitmap target, float widthly, float heightly, Vector3 rotation , bool namethem)
        {
            var datasetbks = Data3DSetClass<TexturedQuadData>.Create("galobj", Color.White, 1f);

            if (EDDiscoveryForm.galacticMapping != null )
            {
                long gmotarget = TargetClass.GetTargetGMO();

                foreach (GalacticMapObject gmo in EDDiscoveryForm.galacticMapping.galacticMapObjects)
                {
                    PointData pd = (gmo.points.Count > 0) ? gmo.points[0] : null;     // lets be paranoid
                    Bitmap touse = gmo.galMapType.Image;                        // under our control, so must have it

                    if (touse != null && pd != null && gmo.galMapType.Enabled )     // if it has an image (may not) and has a co-ord, may not.. 
                    {
                        Debug.Assert(touse != null);

                        TexturedQuadData newtexture = TexturedQuadData.FromBitmap(touse, pd, rotation, widthly, heightly);
                        newtexture.Tag = gmo;
                        newtexture.Tag2 = 0;
                        datasetbks.Add(newtexture);

                        if (namethem)
                        {
                            Bitmap map = null;

                            if (_cachedBitmaps.ContainsKey(gmo.name))      // cache them, they take a long time to compute..
                            {
                                map = _cachedBitmaps[gmo.name];
                            }
                            else
                            {
                                map = DrawString(gmo.name, Color.Orange, gmostarfont);
                                _cachedBitmaps.Add(gmo.name, map);
                            }

                            TexturedQuadData ntext = TexturedQuadData.FromBitmap(map, pd, rotation, 
                                                    (widthly / 10 * gmo.name.Length),
                                                    (heightly / 3),
                                                    0, heightly * gmonameoff);
                            ntext.Tag = gmo;
                            ntext.Tag2 = 1;
                            datasetbks.Add(ntext);
                        }

                        if ( gmo.id == gmotarget )
                        {
                            TexturedQuadData ntag = TexturedQuadData.FromBitmap(target, pd, rotation, widthly, heightly, 0, heightly * gmotargetoff);
                            ntag.Tag = gmo;
                            ntag.Tag2 = 2;
                            datasetbks.Add(ntag);
                        }
                    }
                }
            }

            _datasets.Add(datasetbks);

            return _datasets;
        }

        public void UpdateGalObjects(ref List<IData3DSet> _datasets, float widthly, float heightly , Vector3 rotation )
        {
            if (_datasets == null)
                return;

            foreach ( IData3DSet dataset in _datasets )
            {
                TexturedQuadDataCollection tqdc = dataset as TexturedQuadDataCollection;

                foreach(TexturedQuadData tqd in tqdc.BaseTextures )
                {
                    GalacticMapObject gmo = tqd.Tag as GalacticMapObject;
                    Debug.Assert(gmo != null);
                    PointData pd = (gmo.points.Count > 0) ? gmo.points[0] : null;     // lets be paranoid
                    Debug.Assert(pd!=null);

                    int id = (int)tqd.Tag2;
                    if ( id == 0 )
                        tqd.UpdateVertices(pd, rotation, widthly,heightly);
                    else if ( id == 1 )
                        tqd.UpdateVertices(pd, rotation, (widthly / 10 * gmo.name.Length), (heightly / 3), 0, heightly * gmonameoff);
                    else
                        tqd.UpdateVertices(pd, rotation, widthly, heightly, 0, heightly * gmotargetoff);
                }
            }
        }

        #endregion

        #region Grids

        public List<IData3DSet> AddGridCoords()
        {
            string fontname = "MS Sans Serif";
            {
                Font fnt = new Font(fontname, 20F);

                int bitmapwidth, bitmapheight;
                Bitmap text_bmp = new Bitmap(300, 30);
                using (Graphics g = Graphics.FromImage(text_bmp))
                {
                    SizeF sz = g.MeasureString("-99999,-99999", fnt);
                    bitmapwidth = (int)sz.Width + 4;
                    bitmapheight = (int)sz.Height + 4;
                }
                var datasetMapImgLOD1 = Data3DSetClass<TexturedQuadData>.Create("text bitmap LOD1", Color.White, 1.0f);
                var datasetMapImgLOD2 = Data3DSetClass<TexturedQuadData>.Create("text bitmap LOD2", Color.FromArgb(128, Color.White), 1.0f);

                int textheightly = 50;
                int textwidthly = textheightly * bitmapwidth / bitmapheight;

                int gridwideLOD1 = (int)Math.Floor((MaxGridPos.X - MinGridPos.X) / gridunitSize + 1);
                int gridhighLOD1 = (int)Math.Floor((MaxGridPos.Y - MinGridPos.Y) / gridunitSize + 1);
                int gridwideLOD2 = (int)Math.Floor((MaxGridPos.X - MinGridPos.X) / (gridunitSize * 10) + 1);
                int gridhighLOD2 = (int)Math.Floor((MaxGridPos.Y - MinGridPos.Y) / (gridunitSize * 10) + 1);
                int texwide = 1024 / bitmapwidth;
                int texhigh = 1024 / bitmapheight;
                int numtexLOD1 = (int)Math.Ceiling((gridwideLOD1 * gridhighLOD1) * 1.0 / (texwide * texhigh));
                int numtexLOD2 = (int)Math.Ceiling((gridwideLOD2 * gridhighLOD2) * 1.0 / (texwide * texhigh));

                List<TexturedQuadData> basetexturesLOD1 = Enumerable.Range(0, numtexLOD1).Select(i => new TexturedQuadData(null, null, new Bitmap(1024, 1024))).ToList();
                List<TexturedQuadData> basetexturesLOD2 = Enumerable.Range(0, numtexLOD2).Select(i => new TexturedQuadData(null, null, new Bitmap(1024, 1024))).ToList();

                for (float x = MinGridPos.X; x < MaxGridPos.X; x += gridunitSize)
                {
                    for (float z = MinGridPos.Y; z <= MaxGridPos.Y; z += gridunitSize)
                    {
                        int num = (int)(Math.Floor((x - MinGridPos.X) / gridunitSize) * gridwideLOD1 + Math.Floor((z - MinGridPos.Y) / gridunitSize));
                        int tex_x = (num % texwide) * bitmapwidth;
                        int tex_y = ((num / texwide) % texhigh) * bitmapheight;
                        int tex_n = num / (texwide * texhigh);

                        //Console.WriteLine("num {0} tex_x {1} tex_y {2} txt_n {3}", num, tex_x, tex_y, tex_n);

                        DrawGridBitmap(basetexturesLOD1[tex_n].Texture, x, z, fnt, tex_x, tex_y);
                        datasetMapImgLOD1.Add(basetexturesLOD1[tex_n].Horz(
                            x, z,
                            x + textwidthly, z + textheightly,
                            tex_x, tex_y, tex_x + bitmapwidth, tex_y + bitmapheight
                          ));
                    }
                }

                for (float x = MinGridPos.X; x < MaxGridPos.X; x += gridunitSize * 10)
                {
                    for (float z = MinGridPos.Y; z <= MaxGridPos.Y; z += gridunitSize * 10)
                    {
                        int num = (int)(Math.Floor((x - MinGridPos.X) / (gridunitSize * 10)) * gridwideLOD2 + Math.Floor((z - MinGridPos.Y) / (gridunitSize * 10)));
                        int tex_x = (num % texwide) * bitmapwidth;
                        int tex_y = ((num / texwide) % texhigh) * bitmapheight;
                        int tex_n = num / (texwide * texhigh);

                        DrawGridBitmap(basetexturesLOD2[tex_n].Texture, x, z, fnt, tex_x, tex_y);
                        datasetMapImgLOD2.Add(basetexturesLOD2[tex_n].Horz(
                            x, z,
                            x + textwidthly*10, z + textheightly*10,
                            tex_x, tex_y, tex_x + bitmapwidth, tex_y + bitmapheight
                        ));
                    }
                }

                _datasets.Add(datasetMapImgLOD1);
                _datasets.Add(datasetMapImgLOD2);
            }

            return _datasets;
        }

        private Bitmap DrawGridBitmap(Bitmap text_bmp, float x, float z, Font fnt, int px, int py)
        {
            using (Graphics g = Graphics.FromImage(text_bmp))
            {
                using (Brush br = new SolidBrush(MapColours.CoarseGridLines))
                    g.DrawString(x.ToString("0") + "," + z.ToString("0"), fnt, br, new Point(px, py));
            }

            return text_bmp;
        }

        public List<IData3DSet> AddFineGridLines()
        {
            int smallUnitSize = gridunitSize / 10;
            var smalldatasetGrid = Data3DSetClass<LineData>.Create("gridLOD0", MapColours.FineGridLines, 0.6f);

            for (float x = MinGridPos.X; x <= MaxGridPos.X; x += smallUnitSize)
            {
                smalldatasetGrid.Add(new LineData(x, 0, MinGridPos.Y, x, 0, MaxGridPos.Y));
            }

            for (float z = MinGridPos.Y; z <= MaxGridPos.Y; z += smallUnitSize)
            {
                smalldatasetGrid.Add(new LineData(MinGridPos.X, 0, z, MaxGridPos.X, 0, z));
            }

            _datasets.Add(smalldatasetGrid);
            return _datasets;
        }

        public List<IData3DSet> AddCoarseGridLines()
        {
            var datasetGridLOD1 = Data3DSetClass<LineData>.Create("gridLOD1", MapColours.CoarseGridLines, 0.6f);

            for (float x = MinGridPos.X; x <= MaxGridPos.X; x += gridunitSize)
            {
                datasetGridLOD1.Add(new LineData(x, 0, MinGridPos.Y, x, 0, MaxGridPos.Y));
            }

            for (float z = MinGridPos.Y; z <= MaxGridPos.Y; z += gridunitSize)
            {
                datasetGridLOD1.Add(new LineData(MinGridPos.X, 0, z, MaxGridPos.X, 0, z));
            }

            _datasets.Add(datasetGridLOD1);

            var datasetGridLOD2 = Data3DSetClass<LineData>.Create("gridLOD2", MapColours.CoarseGridLines, 0.6f);

            for (float x = MinGridPos.X; x <= MaxGridPos.X; x += gridunitSize * 10)
            {
                datasetGridLOD2.Add(new LineData(x, 0, MinGridPos.Y, x, 0, MaxGridPos.Y));
            }

            for (float z = MinGridPos.Y; z <= MaxGridPos.Y; z += gridunitSize * 10)
            {
                datasetGridLOD2.Add(new LineData(MinGridPos.X, 0, z, MaxGridPos.X, 0, z));
            }

            _datasets.Add(datasetGridLOD2);
            return _datasets;
        }

        public void UpdateGridZoom(ref List<IData3DSet> _datasets, double zoom)
        {
            double LOD1fade = Math.Max(Math.Min((zoom / 0.1 - 1.0) / 5.0, 1.0), 0.5);

            var gridLOD1 = _datasets.SingleOrDefault(s => s.Name == "gridLOD1");
            if (gridLOD1 != null)
            {
                var colour = MapColours.CoarseGridLines;
                //Console.WriteLine("LOD1 fade"+ LOD1fade);
                colour = Color.FromArgb((int)(colour.R * LOD1fade), (int)(colour.G * LOD1fade), (int)(colour.B * LOD1fade));
                var newgrid = Data3DSetClass<LineData>.Create("gridLOD1", colour, 0.6f);

                foreach (var line in ((Data3DSetClass<LineData>)gridLOD1).Primatives)
                {
                    newgrid.Add(line);
                }

                int index = _datasets.IndexOf(gridLOD1);
                _datasets.Remove(gridLOD1);
                ((IDisposable)gridLOD1).Dispose();
                _datasets.Insert(index, newgrid);
            }
        }

        public void UpdateGridCoordZoom(ref List<IData3DSet> _datasets, double zoom)
        {
            double LOD2fade = Math.Max(Math.Min((0.2 / zoom - 1.0) / 2.0, 1.0), 0.0);

            var gridLOD2 = _datasets.SingleOrDefault(s => s.Name == "text bitmap LOD2");
            if (gridLOD2 != null)
            {
                //Console.WriteLine("LOD2 fade" + LOD2fade);
                var newgrid = Data3DSetClass<TexturedQuadData>.Create("text bitmap LOD2", Color.FromArgb((int)(255 * LOD2fade), Color.White), 1.0f);
                foreach (var tex in ((Data3DSetClass<TexturedQuadData>)gridLOD2).Primatives)
                {
                    newgrid.Add(tex);
                }

                _datasets.Remove(gridLOD2);
                _datasets.Add(newgrid);
            }
        }

        #endregion

        #region Routes

        public List<IData3DSet> BuildVisitedSystems(bool drawlines, ISystem centersystem, List<VisitedSystemsClass> VisitedSystems,
                                                     List<SystemClass> refsys, List<SystemClass> planned)
        {
            AddVisitedSystemsInformation(drawlines, VisitedSystems);
            AddRoutePlannerInfoToDataset(planned);
            AddTrilaterationInfoToDataset(centersystem, refsys);
            return _datasets;
        }

        private void AddVisitedSystemsInformation( bool DrawLines , List<VisitedSystemsClass> VisitedSystems )
        {
            if (VisitedSystems != null && VisitedSystems.Any() && DrawLines )
            {
                VisitedSystemsClass.SetLastKnownSystemPosition(VisitedSystems);

                IEnumerable<IGrouping<int, VisitedSystemsClass>> colours =
                    from VisitedSystemsClass sysPos in VisitedSystems 
                    group sysPos by sysPos.MapColour;

                if (colours!=null)
                {
                    foreach (IGrouping<int, VisitedSystemsClass> colour in colours)
                    {
                        var datasetl = Data3DSetClass<LineData>.Create("visitedstars" + colour.Key.ToString(), Color.FromArgb(colour.Key), 2.0f);
                        foreach (VisitedSystemsClass sp in colour)
                        {
                            if (sp.curSystem != null && sp.curSystem.HasCoordinate && sp.lastKnownSystem != null && sp.lastKnownSystem.HasCoordinate)
                            {
                                datasetl.Add(new LineData(sp.curSystem.x, sp.curSystem.y, sp.curSystem.z,
                                    sp.lastKnownSystem.x, sp.lastKnownSystem.y, sp.lastKnownSystem.z));

                            }
                        }
                        _datasets.Add(datasetl);

                        var datasetvs = Data3DSetClass<PointData>.Create("visitedstars" + colour.Key.ToString(), Color.Orange, 2.0f);
                        foreach (VisitedSystemsClass sp in colour)
                        {
                            if ( sp.curSystem != null && sp.curSystem.HasCoordinate)
                            {
                                datasetvs.Add(new PointData(sp.curSystem.x, sp.curSystem.y, sp.curSystem.z));
                            }
                        }
                        _datasets.Add(datasetvs);
                    }
                }
            }
        }

        private void AddTrilaterationInfoToDataset(ISystem CenterSystem, List<SystemClass> ReferenceSystems)
        {
            if (ReferenceSystems != null && ReferenceSystems.Any())
            {
                var referenceLines = Data3DSetClass<LineData>.Create("CurrentReference", MapColours.TrilatCurrentReference, 5.0f);
                foreach (var refSystem in ReferenceSystems)
                {
                    referenceLines.Add(new LineData(CenterSystem.x, CenterSystem.y, CenterSystem.z, refSystem.x, refSystem.y, refSystem.z));
                }

                _datasets.Add(referenceLines);

                var lineSet = Data3DSetClass<LineData>.Create("SuggestedReference", MapColours.TrilatSuggestedReference, 5.0f);


                Stopwatch sw = new Stopwatch();
                sw.Start();
                SuggestedReferences references = new SuggestedReferences(CenterSystem.x, CenterSystem.y, CenterSystem.z);

                for (int ii = 0; ii < 16; ii++)
                {
                    var rsys = references.GetCandidate();
                    if (rsys == null) break;
                    var system = rsys.System;
                    references.AddReferenceStar(system);
                    if (ReferenceSystems != null && ReferenceSystems.Any(s => s.name == system.name)) continue;
                    System.Diagnostics.Trace.WriteLine(string.Format("{0} Dist: {1} x:{2} y:{3} z:{4}", system.name, rsys.Distance.ToString("0.00"), system.x, system.y, system.z));
                    lineSet.Add(new LineData(CenterSystem.x, CenterSystem.y, CenterSystem.z, system.x, system.y, system.z));
                }
                sw.Stop();
                System.Diagnostics.Trace.WriteLine("Reference stars time " + sw.Elapsed.TotalSeconds.ToString("0.000s"));
                _datasets.Add(lineSet);
            }
        }

        private void AddRoutePlannerInfoToDataset(List<SystemClass> PlannedRoute)
        {
            if (PlannedRoute != null && PlannedRoute.Any())
            {
                var routeLines = Data3DSetClass<LineData>.Create("PlannedRoute", MapColours.PlannedRoute, 25.0f);
                ISystem prevSystem = PlannedRoute.First();
                foreach (ISystem point in PlannedRoute.Skip(1))
                {
                    routeLines.Add(new LineData(prevSystem.x, prevSystem.y, prevSystem.z, point.x, point.y, point.z));
                    prevSystem = point;
                }
                _datasets.Add(routeLines);
            }
        }

        #endregion

        #region Systems

        public List<IData3DSet> BuildSelected(ISystem centersystem, ISystem selectedsystem, GalacticMapObject selectedgmo, float widthly, float heightly, Vector3 rotation )
        {
            Bitmap selmark  = (Bitmap)EDDiscovery.Properties.Resources.selectedmarker;

            if (centersystem != null)
            {
                var dataset = Data3DSetClass<PointData>.Create("Center", MapColours.CentredSystem, 5.0f);
                dataset.Add(new PointData(centersystem.x, centersystem.y, centersystem.z));
                _datasets.Add(dataset);
            }

            if (selectedsystem != null)
            {
                var datasetbks = Data3DSetClass<TexturedQuadData>.Create("selstar", Color.White, 1f);

                TexturedQuadData newtexture = TexturedQuadData.FromBitmap(selmark, new PointData(selectedsystem.x,selectedsystem.y,selectedsystem.z), rotation, widthly, heightly/2, 0, heightly/4+heightly/16);
                newtexture.Tag = 0;
                datasetbks.Add(newtexture);
                _datasets.Add(datasetbks);
            }

            if ( selectedgmo != null )
            {
                PointData pd = (selectedgmo.points.Count > 0) ? selectedgmo.points[0] : null;     // lets be paranoid

                if (pd != null)
                {
                    var datasetbks = Data3DSetClass<TexturedQuadData>.Create("selgmo", Color.White, 1f);

                    long gmotarget = TargetClass.GetTargetGMO();

                    float hoff = (gmotarget == selectedgmo.id) ? (heightly * gmoseltarget) : (heightly * gmoselonly);
                    TexturedQuadData newtexture = TexturedQuadData.FromBitmap(selmark, pd, rotation, widthly, heightly / 2, 0, hoff);
                    newtexture.Tag = 1;
                    datasetbks.Add(newtexture);
                    _datasets.Add(datasetbks);
                }
            }

            return _datasets;
        }

        public void UpdateSelected(ref List<IData3DSet> _datasets, ISystem selectedsystem, GalacticMapObject selectedgmo, float widthly, float heightly, Vector3 rotation)
        {
            if (_datasets == null)
                return;

            foreach (IData3DSet dataset in _datasets)
            {
                if (dataset is TexturedQuadDataCollection)
                {
                    TexturedQuadDataCollection tqdc = dataset as TexturedQuadDataCollection;

                    foreach (TexturedQuadData tqd in tqdc.BaseTextures)
                    {
                        int id = (int)tqd.Tag;

                        if (id == 0)
                            tqd.UpdateVertices(new PointData(selectedsystem.x, selectedsystem.y, selectedsystem.z), rotation, widthly, heightly / 2, 0, heightly / 4 + heightly / 16);
                        else
                        {
                            long gmotarget = TargetClass.GetTargetGMO();
                            float hoff = (gmotarget == selectedgmo.id) ? (heightly * gmoseltarget) : (heightly * gmoselonly);
                            PointData pd = (selectedgmo.points.Count > 0) ? selectedgmo.points[0] : null;     // lets be paranoid
                            tqd.UpdateVertices(pd, rotation, widthly, heightly / 2, 0, hoff);
                        }
                    }
                }
            }
        }

        #endregion

        #region Helpers

        static public Bitmap DrawString(string str, Color textcolour, Font fnt)
        {
            Bitmap stringstarmeasurebitmap = new Bitmap(1, 1);
            using (Graphics g = Graphics.FromImage(stringstarmeasurebitmap))
            {
                SizeF sz = g.MeasureString(str, fnt);

                Bitmap text_bmp = new Bitmap((int)sz.Width + 4, (int)sz.Height + 4);
                using (Graphics h = Graphics.FromImage(text_bmp))
                {
                    h.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    using (Brush br = new SolidBrush(textcolour))
                        h.DrawString(str, fnt, br, new Point(0, 0));
                    h.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
                }

                return text_bmp;
            }
        }

        #endregion
    }
}
