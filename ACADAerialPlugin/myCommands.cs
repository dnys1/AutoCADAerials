// (C) Copyright 2020 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using InsertAerial;
using Exception = Autodesk.AutoCAD.Runtime.Exception;
using System.IO;
using Common;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(ACADAerialPlugin.AutoCADAerials))]

namespace ACADAerialPlugin
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class AutoCADAerials
    {
        [CommandMethod("InsertAerial")]
        public void InsertAerial()
        {
            // Show the user the Insert Aerial dialog and pass in
            // InsertDownloadedImageToDocument as the callback function
            // for when it is successfully downloaded.
            string currentDirectory = Path.GetDirectoryName(Application.DocumentManager.MdiActiveDocument.Name);
            MainWindow insertAerialWindow = new MainWindow(InsertDownloadedImageToDocument, currentDirectory);
            insertAerialWindow.Show();
        }

        [CommandMethod("UpdateAerial")]
        public void UpdateAerial()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            Editor ed = doc.Editor;

            // Select an aerial entity to update
            PromptEntityResult entityResult = ed.GetEntity("Pick an aerial to update : ");
            
            if (entityResult.Status == PromptStatus.OK)
            {
                // Create a new transaction to contain the update
                Transaction tx = doc.TransactionManager.StartTransaction();

                try
                {
                    // Get the aerial entity object and try to read its extension dictionary
                    RasterImage aerialEnt = tx.GetObject(entityResult.ObjectId, OpenMode.ForRead) as RasterImage;

                    if (aerialEnt.ExtensionDictionary.IsNull)
                    {
                        throw new Exception(ErrorStatus.InvalidInput, "Unrecognized aerial image.");
                    }

                    // Get the extension dictionary object
                    DBDictionary extDict = tx.GetObject(aerialEnt.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;

                    if (!extDict.Contains("ImageData"))
                    {
                        throw new Exception(ErrorStatus.InvalidInput, "Unrecognized aerial image.");
                    }

                    // Get the associated image data
                    ObjectId boundsEntryId = extDict.GetAt("ImageData");
                    Xrecord boundsRecord = tx.GetObject(boundsEntryId, OpenMode.ForRead) as Xrecord;

                    TypedValue[] resBuff = boundsRecord.Data.AsArray();

                    GeoPoint center = new GeoPoint((resBuff[0].Value as double?).Value, (resBuff[1].Value as double?).Value);
                    GeoPoint neCorner = new GeoPoint((resBuff[2].Value as double?).Value, (resBuff[3].Value as double?).Value);
                    GeoPoint swCorner = new GeoPoint((resBuff[4].Value as double?).Value, (resBuff[5].Value as double?).Value);

                    int zoom = (resBuff[6].Value as int?).Value;
                    int mapTypeInt = (resBuff[7].Value as int?).Value;
                    int mapSourceInt = (resBuff[8].Value as int?).Value;

                    MapType mapType = (MapType)mapTypeInt;
                    AerialRepository repository;

                    if (mapSourceInt == 0)
                    {
                        repository = new BingAerialRepository();
                    } else
                    {
                        repository = new GoogleMapsAerialRepository();
                    }

                    // Get the current filename using the aerial's RasterImageDef
                    ObjectId aerialImgDefId = aerialEnt.ImageDefId;
                    RasterImageDef aerialImgDef = tx.GetObject(aerialImgDefId, OpenMode.ForRead) as RasterImageDef;

                    string filename = aerialImgDef.SourceFileName;

                    // Get the image width and height
                    
                    int width = (int)aerialImgDef.Size.X;
                    int height = (int)aerialImgDef.Size.Y;

                    // Get the request details for the image downlaod
                    AerialImageData imageData = new AerialImageData(center, neCorner, 
                        swCorner, width, height, zoom, mapType, filename);
                    string requestUrl = repository.BuildImageRequestUrl(imageData, update: true);

                    // Delete the current entity
                    aerialImgDef.UpgradeOpen();
                    aerialImgDef.Unload(true);

                    // Download the new aerial photo to replace the old
                    AerialRepository.DownloadImage(requestUrl, filename);

                    // Reload the image entity
                    aerialImgDef.Load();

                    // Commit the transaction
                    tx.Commit();
                } catch (Exception ex)
                {
                    ed.WriteMessage("Error updating aerial: " + ex.Message + '\n');
                    ed.WriteMessage("Please try again.\n");
                } finally
                {
                    tx.Dispose();
                }
            }
        }

        private double MetersToFeet(double meters) => meters * 3.28084;

        private double DegreesToFeet(double deg) => deg * (24901 / 360) * 5280;

        private void InsertDownloadedImageToDocument(MapSource mapSource, AerialImageData imageData)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("Image downloaded to: " + imageData.FileName + '\n');

            Transaction tx = ed.Document.Database.TransactionManager.StartTransaction();

            try
            {
                // Lock the document
                Application.DocumentManager.MdiActiveDocument.LockDocument();

                // Get the document database
                Database dwg = Application.DocumentManager.MdiActiveDocument.Database;

                // Get the raster image dictionary id. 
                // Create it if it's null.
                ObjectId imageDictId = RasterImageDef.GetImageDictionary(dwg);

                if (imageDictId.IsNull)
                {
                    imageDictId = RasterImageDef.CreateImageDictionary(dwg);
                }

                // Open the image dictionary
                DBDictionary imageDict = tx.GetObject(imageDictId, OpenMode.ForWrite) as DBDictionary;

                // Get the image's name, i.e. Aerial for C:\Aerial.jpg
                string imageName = Path.GetFileName(imageData.FileName).Split('.')[0];

                // Add the image to the dictionary, if it's not already present.
                RasterImageDef rasterImageDef;
                ObjectId imageDefId;
                bool rasterDefCreated = false;
                if (imageDict.Contains(imageName))
                {
                    // Get the image reference from the image dictionary.
                    imageDefId = imageDict.GetAt(imageName);
                    rasterImageDef = tx.GetObject(imageDefId, OpenMode.ForWrite) as RasterImageDef;
                } else
                {
                    // Create the raster image definition
                    RasterImageDef newImageDef = new RasterImageDef
                    {
                        SourceFileName = imageData.FileName
                    };

                    // Load the image into the definition
                    newImageDef.Load();

                    // Write the image to the image dictionary
                    imageDict.UpgradeOpen();
                    imageDefId = imageDict.SetAt(imageName, newImageDef);

                    // Notify the transaction of the change.
                    tx.AddNewlyCreatedDBObject(newImageDef, true);

                    rasterImageDef = newImageDef;
                    rasterDefCreated = true;
                }

                // Open the block table to get the model space ID
                BlockTable bt = tx.GetObject(dwg.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the block table record for the model space.
                BlockTableRecord btr = tx.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Create the new image and assign it the image definition
                using (RasterImage image = new RasterImage())
                {
                    image.ImageDefId = imageDefId;

                    // Get the scale for the image
                    double width = DegreesToFeet(Math.Abs(imageData.SWCorner.lng - imageData.NECorner.lng));
                    double height = rasterImageDef.Size.Y * (width / rasterImageDef.Size.X);
                    
                    ed.WriteMessage($"Using {width} for width, and {height} for height.\n");

                    PromptKeywordOptions promptInsPtOpt = new PromptKeywordOptions("Select insert type [Origin/Real/Choose] <O> : ", "Origin Real Choose");
                    PromptResult promptInsPtOptRes = ed.GetKeywords(promptInsPtOpt);

                    // Insert the image at the reference point
                    Point3d? insertPt = null;

                    if (promptInsPtOptRes.Status == PromptStatus.OK)
                    {
                        switch (promptInsPtOptRes.StringResult)
                        {
                            case "Real":
                                insertPt = new Point3d(DegreesToFeet(imageData.SWCorner.lat),
                                    DegreesToFeet(imageData.SWCorner.lng), 0);
                                break;

                            case "Choose":
                                PromptPointOptions insertPtOptions = new PromptPointOptions("Select insert point : ");
                                PromptPointResult insertPtRes = ed.GetPoint(insertPtOptions);

                                if (insertPtRes.Status == PromptStatus.OK)
                                {
                                    insertPt = insertPtRes.Value;
                                }
                                break;
                        }
                    }

                    if (insertPt == null)
                    {
                        insertPt = new Point3d(0, 0, 0);
                    }

                    // Create the coordinate system to define the width and height.
                    // Otherwise, they will default to the image size in pixels.
                    image.Orientation = new CoordinateSystem3d(insertPt.Value,
                        new Vector3d(width, 0, 0), new Vector3d(0, height, 0));

                    // Set the rotation angle for the image.
                    image.Rotation = 0;

                    // Add the new object to the block table (must come before adding extension dict!)
                    btr.AppendEntity(image);

                    // Add the extension dictionary with associated image data.
                    if (image.ExtensionDictionary.IsNull)
                    {
                        image.CreateExtensionDictionary();
                    }

                    DBDictionary extDict = tx.GetObject(image.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;

                    // Create the Xrecord and result buffer with TypedValue array
                    Xrecord record = new Xrecord();
                    record.Data = new ResultBuffer(new TypedValue[]
                    {
                        new TypedValue((int)DxfCode.Real, imageData.Center.lat),
                        new TypedValue((int)DxfCode.Real, imageData.Center.lng),
                        new TypedValue((int)DxfCode.Real, imageData.NECorner.lat),
                        new TypedValue((int)DxfCode.Real, imageData.NECorner.lng),
                        new TypedValue((int)DxfCode.Real, imageData.SWCorner.lat),
                        new TypedValue((int)DxfCode.Real, imageData.SWCorner.lng),
                        new TypedValue((int)DxfCode.Int32, imageData.Zoom),
                        new TypedValue((int)DxfCode.Int32, (int)imageData.MapType),
                        new TypedValue((int)DxfCode.Int32, (int)mapSource),
                    });

                    extDict.SetAt("ImageData", record);

                    // Add the new objects to the transaction
                    tx.AddNewlyCreatedDBObject(image, true);
                    tx.AddNewlyCreatedDBObject(record, true);

                    // Connect the raster definition and image together so the definition
                    // does not appear as "unreferenced" in the External References palette
                    RasterImage.EnableReactors(true);
                    image.AssociateRasterDef(rasterImageDef);

                    if (rasterDefCreated)
                    {
                        rasterImageDef.Dispose();
                    }
                }

                tx.Commit();

                // TODO: Zoom to aerial
            }
            catch (Exception ex)
            {
                ed.WriteMessage("An error occurred inserting the image: " + ex.Message + '\n');
                ed.WriteMessage("Please try again.\n");
            } finally
            {
                tx.Dispose();
            }

        }
    }

}
