﻿// ============================================================================
// PlotRegionToPDF.cs
// © Andrey Bushman, 2014
// ============================================================================
// The PLOTREGION command plot a Region object to PDF.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using cad = Autodesk.AutoCAD.ApplicationServices.Application;
using Ap = Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Ed = Autodesk.AutoCAD.EditorInput;
using Rt = Autodesk.AutoCAD.Runtime;
using Gm = Autodesk.AutoCAD.Geometry;
using Wn = Autodesk.AutoCAD.Windows;
using Hs = Autodesk.AutoCAD.DatabaseServices.HostApplicationServices;
using Us = Autodesk.AutoCAD.DatabaseServices.SymbolUtilityServices;
using Br = Autodesk.AutoCAD.BoundaryRepresentation;
using Pt = Autodesk.AutoCAD.PlottingServices;

[assembly: Rt.CommandClass(typeof(HomeDesignCad.Plot.PlotRegionToPDF))]

namespace HomeDesignCad.Plot
{

    public static class PlotRegionToPDF
    {
#if AUTOCAD_NEWER_THAN_2012
    const String acedTransOwner = "accore.dll";
#else
        const String acedTransOwner = "accore.dll";
#endif

#if AUTOCAD_NEWER_THAN_2014
    const String acedTrans_x86_Prefix = "_";
#else
        const String acedTrans_x86_Prefix = "";
#endif

        const String acedTransName = "acedTrans";

        [DllImport(acedTransOwner, CallingConvention = CallingConvention.Cdecl,
                EntryPoint = acedTrans_x86_Prefix + acedTransName)]
        static extern Int32 acedTrans_x86(Double[] point, IntPtr fromRb,
          IntPtr toRb, Int32 disp, Double[] result);

        [DllImport(acedTransOwner, CallingConvention = CallingConvention.Cdecl,
                EntryPoint = acedTransName)]
        static extern Int32 acedTrans_x64(Double[] point, IntPtr fromRb,
          IntPtr toRb, Int32 disp, Double[] result);

        public static Int32 acedTrans(Double[] point, IntPtr fromRb, IntPtr toRb,
          Int32 disp, Double[] result)
        {
            if (IntPtr.Size == 4)
                return acedTrans_x86(point, fromRb, toRb, disp, result);
            else
                return acedTrans_x64(point, fromRb, toRb, disp, result);
        }

        [Rt.CommandMethod("plotRegion", Rt.CommandFlags.Modal)]
        public static void PlotRegion()
        {
            Ap.Document doc = cad.DocumentManager.MdiActiveDocument;
            if (doc == null || doc.IsDisposed)
                return;

            Ed.Editor ed = doc.Editor;
            Db.Database db = doc.Database;

            using (doc.LockDocument())
            {
                Ed.PromptEntityOptions peo = new Ed.PromptEntityOptions(
                 "\nSelect a region"
                 );

                peo.SetRejectMessage("\nIt is not a region."
                  );
                peo.AddAllowedClass(typeof(Db.Region), false);

                Ed.PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != Ed.PromptStatus.OK)
                {
                    ed.WriteMessage("\nCommand canceled.\n");
                    return;
                }

                Db.ObjectId regionId = per.ObjectId;

                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32
                  .SaveFileDialog();
                saveFileDialog.Title =
                  "PDF file name";
                saveFileDialog.Filter = "PDF-files|*.pdf";
                bool? result = saveFileDialog.ShowDialog();

                if (!result.HasValue || !result.Value)
                {
                    ed.WriteMessage("\nCommand canceled.");
                    return;
                }

                String pdfFileName = saveFileDialog.FileName;

                PlotRegion(regionId, "DWG To PDF.pc3",
                  "ISO_A4_(210.00_x_297.00_MM)", pdfFileName);

                ed.WriteMessage("\nThe \"{0}\" file created.\n", pdfFileName);
            }
        }

        // This method helps to get correct "boundary box" for the regions which 
        // created through the splines. Written by Alexander Rivilis.
        public static void GetVisualBoundary(this Db.Region region, double delta,
            ref Gm.Point2d minPoint, ref Gm.Point2d maxPoint)
        {
            using (Gm.BoundBlock3d boundBlk = new Gm.BoundBlock3d())
            {
                using (Br.Brep brep = new Br.Brep(region))
                {
                    foreach (Br.Edge edge in brep.Edges)
                    {
                        using (Gm.Curve3d curve = edge.Curve)
                        {
                            Gm.ExternalCurve3d curve3d = curve as Gm.ExternalCurve3d;

                            if (curve3d != null && curve3d.IsNurbCurve)
                            {
                                using (Gm.NurbCurve3d nurbCurve = curve3d.NativeCurve
                                  as Gm.NurbCurve3d)
                                {
                                    Gm.Interval interval = nurbCurve.GetInterval();
                                    for (double par = interval.LowerBound; par <=
                                      interval.UpperBound; par += (delta * 2.0))
                                    {
                                        Gm.Point3d p = nurbCurve.EvaluatePoint(par);
                                        if (!boundBlk.IsBox)
                                            boundBlk.Set(p, p);
                                        else
                                            boundBlk.Extend(p);
                                    }
                                }
                            }
                            else
                            {
                                if (!boundBlk.IsBox)
                                {
                                    boundBlk.Set(edge.BoundBlock.GetMinimumPoint(),
                                      edge.BoundBlock.GetMaximumPoint());
                                }
                                else
                                {
                                    boundBlk.Extend(edge.BoundBlock.GetMinimumPoint());
                                    boundBlk.Extend(edge.BoundBlock.GetMaximumPoint());
                                }
                            }
                        }
                    }
                }
                boundBlk.Swell(delta);

                minPoint = new Gm.Point2d(boundBlk.GetMinimumPoint().X,
                  boundBlk.GetMinimumPoint().Y);
                maxPoint = new Gm.Point2d(boundBlk.GetMaximumPoint().X,
                  boundBlk.GetMaximumPoint().Y);
            }
        }

        // This code based on Kean Walmsley's article:
        // http://through-the-interface.typepad.com/through_the_interface/2007/10/plotting-a-wind.html
        public static void PlotRegion(Db.ObjectId regionId, String pcsFileName,
      String mediaName, String outputFileName)
        {

            if (regionId.IsNull)
                throw new ArgumentException("regionId.IsNull == true");
            if (!regionId.IsValid)
                throw new ArgumentException("regionId.IsValid == false");

            if (regionId.ObjectClass.Name != "AcDbRegion")
                throw new ArgumentException("regionId.ObjectClass.Name != AcDbRegion");

            if (pcsFileName == null)
                throw new ArgumentNullException("pcsFileName");
            if (pcsFileName.Trim() == String.Empty)
                throw new ArgumentException("pcsFileName.Trim() == String.Empty");

            if (mediaName == null)
                throw new ArgumentNullException("mediaName");
            if (mediaName.Trim() == String.Empty)
                throw new ArgumentException("mediaName.Trim() == String.Empty");

            if (outputFileName == null)
                throw new ArgumentNullException("outputFileName");
            if (outputFileName.Trim() == String.Empty)
                throw new ArgumentException("outputFileName.Trim() == String.Empty");

            Db.Database previewDb = Hs.WorkingDatabase;
            Db.Database db = null;
            Ap.Document doc = cad.DocumentManager.MdiActiveDocument;
            if (doc == null || doc.IsDisposed)
                return;

            Ed.Editor ed = doc.Editor;
            try
            {
                if (regionId.Database != null && !regionId.Database.IsDisposed)
                {
                    Hs.WorkingDatabase = regionId.Database;
                    db = regionId.Database;
                }
                else
                {
                    db = doc.Database;
                }

                using (doc.LockDocument())
                {
                    using (Db.Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        Db.Region region = tr.GetObject(regionId,
                        Db.OpenMode.ForRead) as Db.Region;

                        Db.Extents3d extends = region.GeometricExtents;
                        Db.ObjectId modelId = Us.GetBlockModelSpaceId(db);
                        Db.BlockTableRecord model = tr.GetObject(modelId,
                        Db.OpenMode.ForRead) as Db.BlockTableRecord;

                        Db.Layout layout = tr.GetObject(model.LayoutId,
                        Db.OpenMode.ForRead) as Db.Layout;

                        using (Pt.PlotInfo pi = new Pt.PlotInfo())
                        {
                            pi.Layout = model.LayoutId;

                            using (Db.PlotSettings ps = new Db.PlotSettings(layout.ModelType)
                              )
                            {

                                ps.CopyFrom(layout);

                                Db.PlotSettingsValidator psv = Db.PlotSettingsValidator
                                  .Current;

                                Gm.Point2d bottomLeft = Gm.Point2d.Origin;
                                Gm.Point2d topRight = Gm.Point2d.Origin;

                                region.GetVisualBoundary(0.1, ref bottomLeft,
                                  ref topRight);

                                Gm.Point3d bottomLeft_3d = new Gm.Point3d(bottomLeft.X,
                                  bottomLeft.Y, 0);
                                Gm.Point3d topRight_3d = new Gm.Point3d(topRight.X, topRight.Y,
                                  0);

                                Db.ResultBuffer rbFrom = new Db.ResultBuffer(new Db.TypedValue(
                                  5003, 1));
                                Db.ResultBuffer rbTo = new Db.ResultBuffer(new Db.TypedValue(
                                  5003, 2));

                                double[] firres = new double[] { 0, 0, 0 };
                                double[] secres = new double[] { 0, 0, 0 };

                                acedTrans(bottomLeft_3d.ToArray(), rbFrom.UnmanagedObject,
                                  rbTo.UnmanagedObject, 0, firres);
                                acedTrans(topRight_3d.ToArray(), rbFrom.UnmanagedObject,
                                  rbTo.UnmanagedObject, 0, secres);

                                Db.Extents2d extents = new Db.Extents2d(
                                    firres[0],
                                    firres[1],
                                    secres[0],
                                    secres[1]
                                  );

                                psv.SetZoomToPaperOnUpdate(ps, true);

                                psv.SetPlotWindowArea(ps, extents);
                                psv.SetPlotType(ps, Db.PlotType.Window);
                                psv.SetUseStandardScale(ps, true);
                                psv.SetStdScaleType(ps, Db.StdScaleType.ScaleToFit);
                                psv.SetPlotCentered(ps, true);
                                psv.SetPlotRotation(ps, Db.PlotRotation.Degrees000);

                                // We'll use the standard DWF PC3, as
                                // for today we're just plotting to file
                                psv.SetPlotConfigurationName(ps, pcsFileName, mediaName);

                                // We need to link the PlotInfo to the
                                // PlotSettings and then validate it
                                pi.OverrideSettings = ps;
                                Pt.PlotInfoValidator piv = new Pt.PlotInfoValidator();
                                piv.MediaMatchingPolicy = Pt.MatchingPolicy.MatchEnabled;
                                piv.Validate(pi);

                                // A PlotEngine does the actual plotting
                                // (can also create one for Preview)
                                if (Pt.PlotFactory.ProcessPlotState == Pt.ProcessPlotState
                                  .NotPlotting)
                                {
                                    using (Pt.PlotEngine pe = Pt.PlotFactory.CreatePublishEngine()
                                      )
                                    {
                                        // Create a Progress Dialog to provide info
                                        // and allow thej user to cancel

                                        using (Pt.PlotProgressDialog ppd =
                                          new Pt.PlotProgressDialog(false, 1, true))
                                        {
                                            ppd.set_PlotMsgString(
                                            Pt.PlotMessageIndex.DialogTitle, "Custom Plot Progress");

                                            ppd.set_PlotMsgString(
                                              Pt.PlotMessageIndex.CancelJobButtonMessage,
                                              "Cancel Job");

                                            ppd.set_PlotMsgString(
                                            Pt.PlotMessageIndex.CancelSheetButtonMessage,
                                            "Cancel Sheet");

                                            ppd.set_PlotMsgString(
                                            Pt.PlotMessageIndex.SheetSetProgressCaption,
                                            "Sheet Set Progress");

                                            ppd.set_PlotMsgString(
                                              Pt.PlotMessageIndex.SheetProgressCaption,
                                             "Sheet Progress");

                                            ppd.LowerPlotProgressRange = 0;
                                            ppd.UpperPlotProgressRange = 100;
                                            ppd.PlotProgressPos = 0;

                                            // Let's start the plot, at last
                                            ppd.OnBeginPlot();
                                            ppd.IsVisible = true;
                                            pe.BeginPlot(ppd, null);

                                            // We'll be plotting a single document
                                            pe.BeginDocument(pi, doc.Name, null, 1, true,
                                                // Let's plot to file
                                             outputFileName);
                                            // Which contains a single sheet
                                            ppd.OnBeginSheet();
                                            ppd.LowerSheetProgressRange = 0;
                                            ppd.UpperSheetProgressRange = 100;
                                            ppd.SheetProgressPos = 0;
                                            Pt.PlotPageInfo ppi = new Pt.PlotPageInfo();
                                            pe.BeginPage(ppi, pi, true, null);
                                            pe.BeginGenerateGraphics(null);
                                            pe.EndGenerateGraphics(null);

                                            // Finish the sheet
                                            pe.EndPage(null);
                                            ppd.SheetProgressPos = 100;
                                            ppd.OnEndSheet();

                                            // Finish the document
                                            pe.EndDocument(null);

                                            // And finish the plot
                                            ppd.PlotProgressPos = 100;
                                            ppd.OnEndPlot();
                                            pe.EndPlot(null);
                                        }
                                    }
                                }
                                else
                                {
                                    ed.WriteMessage("\nAnother plot is in progress.");
                                }
                            }
                        }
                        tr.Commit();
                    }
                }
            }
            finally
            {
                Hs.WorkingDatabase = previewDb;
            }
        }
    }
}
 
